// ============================================
// Match Service — match lifecycle management
// ============================================

import type { DifficultyLevel } from '../types/case.types';
import type { Match, GamePhase } from '../types/match.types';
import type { PlayerConclusion } from '../types/case.types';
import { prisma } from '../configs/database';
import { REDIS_KEYS, redisSet, redisGet } from '../configs/redis';
import { MurderEngine } from '../murder_engine/engine/MurderEngine';
import { setRoomInMatch, setRoomMatchEnded } from '../server/rooms/roomManager';
import { CACHE_TTL } from '../utils/constants';
import { DIFFICULTY_CONFIGS } from '../types/case.types';
import { v4 as uuidv4 } from 'uuid';
import logger from '../utils/logger';
import type { FullCase } from '../murder_engine/models/Case';
import { updatePlayerStats } from '../analytics/playerAnalytics';
import { applyRpChange } from './rankingService';
import { checkMatchAchievements } from './achievementService';
import { RP_CHANGES } from '../utils/constants';

const murderEngine = new MurderEngine();

// ============================================
// Create Match
// ============================================

export async function createMatch(
  roomId: string,
  playerIds: string[],
  difficulty: DifficultyLevel
): Promise<Match> {
  const config = DIFFICULTY_CONFIGS[difficulty];
  const duration = config.matchDurationSeconds;

  // Generate case
  const fullCase = await murderEngine.generateCase(difficulty);

  // Persist case to DB
  await persistCaseToDB(fullCase);

  // Create match in DB
  const dbMatch = await prisma.match.create({
    data: {
      id: uuidv4(),
      roomId,
      caseId: fullCase.id,
      difficulty: mapDifficulty(difficulty),
      status: 'waiting',
      phase: 'investigation',
      maxPlayers: playerIds.length,
      durationSeconds: duration,
      players: {
        create: playerIds.map((playerId, index) => ({
          playerId,
          isHost: index === 0,
        })),
      },
    },
  });

  const match: Match = {
    id: dbMatch.id,
    roomId,
    caseId: fullCase.id,
    difficulty,
    status: 'waiting',
    phase: 'investigation',
    playerIds,
    maxPlayers: playerIds.length,
    startedAt: undefined,
    endedAt: undefined,
    durationSeconds: duration,
    timeRemainingSeconds: duration,
    conclusions: [],
    scores: [],
    createdAt: dbMatch.createdAt,
  };

  // Cache match state + full case in Redis
  await redisSet(REDIS_KEYS.matchState(match.id), match, CACHE_TTL.MATCH_STATE);
  await redisSet(REDIS_KEYS.caseCache(fullCase.id), fullCase, CACHE_TTL.CASE_CACHE);

  await setRoomInMatch(roomId, match.id);

  logger.info('[MatchService] Match created', {
    matchId: match.id,
    caseId: fullCase.id,
    difficulty,
    players: playerIds.length,
  });

  return match;
}

// ============================================
// Start Match
// ============================================

export async function startMatch(matchId: string): Promise<Match | null> {
  const match = await getMatchState(matchId);
  if (!match) return null;

  const started: Match = {
    ...match,
    status: 'active',
    startedAt: new Date(),
  };

  await prisma.match.update({
    where: { id: matchId },
    data: { status: 'active', startedAt: started.startedAt },
  });

  await redisSet(REDIS_KEYS.matchState(matchId), started, CACHE_TTL.MATCH_STATE);

  logger.info('[MatchService] Match started', { matchId });
  return started;
}

// ============================================
// Update timer (called every second by game loop)
// ============================================

export async function tickMatchTimer(matchId: string): Promise<{
  match: Match;
  phase: GamePhase;
  shouldEnd: boolean;
}> {
  const match = await getMatchState(matchId);
  if (!match || match.status !== 'active') {
    return { match: match!, phase: 'investigation', shouldEnd: true };
  }

  const newTimeRemaining = Math.max(0, match.timeRemainingSeconds - 1);
  let phase: GamePhase = match.phase;

  // Phase transitions
  if (newTimeRemaining <= 300 && match.phase === 'investigation') {
    phase = 'final_minutes';
  }
  if (newTimeRemaining <= 0) {
    phase = 'submission';
  }

  const updated: Match = {
    ...match,
    timeRemainingSeconds: newTimeRemaining,
    phase,
  };

  await redisSet(REDIS_KEYS.matchState(matchId), updated, CACHE_TTL.MATCH_STATE);

  return {
    match: updated,
    phase,
    shouldEnd: newTimeRemaining <= 0,
  };
}

// ============================================
// Submit Conclusion
// ============================================

export async function submitConclusion(
  matchId: string,
  conclusion: PlayerConclusion
): Promise<{ success: boolean; error?: string }> {
  const match = await getMatchState(matchId);
  if (!match) return { success: false, error: 'Match not found' };
  if (match.status !== 'active') return { success: false, error: 'Match is not active' };

  const alreadySubmitted = match.conclusions.some((c) => c.playerId === conclusion.playerId);
  if (alreadySubmitted) return { success: false, error: 'Already submitted' };

  const updated: Match = {
    ...match,
    conclusions: [...match.conclusions, conclusion],
  };

  // If all players submitted, move to conclusion phase
  if (updated.conclusions.length >= match.playerIds.length) {
    updated.status = 'conclusion';
    updated.phase = 'reveal';
  }

  await redisSet(REDIS_KEYS.matchState(matchId), updated, CACHE_TTL.MATCH_STATE);

  // Persist conclusion to DB
  await prisma.playerConclusion.create({
    data: {
      matchId,
      playerId: conclusion.playerId,
      killerId: conclusion.killerId,
      motive: conclusion.motive,
      weapon: conclusion.weapon,
      location: conclusion.location,
      timeline: conclusion.timeline,
      narrative: conclusion.narrative,
    },
  });

  await prisma.matchPlayer.update({
    where: { matchId_playerId: { matchId, playerId: conclusion.playerId } },
    data: { hasSubmitted: true },
  });

  logger.info('[MatchService] Conclusion submitted', { matchId, playerId: conclusion.playerId });
  return { success: true };
}

// ============================================
// Finalise Match (score + persist)
// ============================================

export async function finaliseMatch(matchId: string): Promise<Match | null> {
  const match = await getMatchState(matchId);
  if (!match) return null;

  const fullCase = await getCaseFomMatch(matchId);
  if (!fullCase) return null;

  // Build time map
  const playerTimeMap = new Map<string, number>();
  for (const c of match.conclusions) {
    if (match.startedAt) {
      const elapsed = (c.submittedAt.getTime() - match.startedAt.getTime()) / 1000;
      playerTimeMap.set(c.playerId, elapsed);
    }
  }

  // Score all conclusions
  const scores = murderEngine.scoreAllConclusions(
    match.conclusions,
    fullCase,
    match.durationSeconds,
    playerTimeMap
  );

  const winnerId = scores[0]?.playerId;

  const finishedMatch: Match = {
    ...match,
    status: 'finished',
    phase: 'reveal',
    endedAt: new Date(),
    scores,
    winnerId,
  };

  // Persist scores to DB
  for (const score of scores) {
    await prisma.matchScore.create({
      data: {
        matchId,
        playerId: score.playerId,
        conclusionId: await getConclusionId(matchId, score.playerId),
        totalScore: score.totalScore,
        killerScore: score.breakdown.killer,
        motiveScore: score.breakdown.motive,
        weaponScore: score.breakdown.weapon,
        locationScore: score.breakdown.location,
        timelineScore: score.breakdown.timeline,
        narrativeScore: score.breakdown.narrative,
        timeBonus: score.timeBonus,
        isCorrect: score.isCorrect,
        finalRank: score.rank,
        rpChange: score.totalScore > 500 ? 20 : -10,
        result: score.rank === 1 ? 'win' : 'loss',
      },
    });
  }

  await prisma.match.update({
    where: { id: matchId },
    data: {
      status: 'finished',
      endedAt: finishedMatch.endedAt,
      winnerId,
    },
  });

  await setRoomMatchEnded(match.roomId);
  await redisSet(REDIS_KEYS.matchState(matchId), finishedMatch, CACHE_TTL.MATCH_STATE);

  // ---- Post-match: update player stats + ranking ----
  for (const score of scores) {
    const isWin = score.rank === 1;
    const timeTaken = playerTimeMap.get(score.playerId) ?? match.durationSeconds;

    // Compute RP change based on rank
    let rpChange: number;
    if (score.rank === 1) rpChange = RP_CHANGES.WIN_1ST;
    else if (score.rank === 2) rpChange = RP_CHANGES.WIN_2ND;
    else if (score.rank === 3) rpChange = RP_CHANGES.WIN_3RD;
    else rpChange = RP_CHANGES.LOSS;

    if (score.isCorrect && score.rank === 1) rpChange += RP_CHANGES.PERFECT_SOLVE_BONUS;

    await updatePlayerStats(score.playerId, score.totalScore, isWin, score.isCorrect, timeTaken);
    await applyRpChange(score.playerId, rpChange, isWin).catch((err) =>
      logger.warn('[MatchService] Failed to apply RP change', { playerId: score.playerId, err })
    );

    // Check achievements
    await checkMatchAchievements(score.playerId, matchId).then((unlocked) => {
      if (unlocked.length > 0) {
        const io = (global as any).io;
        if (io) {
          for (const ach of unlocked) {
            io.to(`player:${score.playerId}`).emit('achievement:unlocked', ach);
          }
        }
      }
    }).catch((err) =>
      logger.warn('[MatchService] Achievement check failed', { playerId: score.playerId, err })
    );
  }

  logger.info('[MatchService] Match finalised', { matchId, winnerId });
  return finishedMatch;
}

// ============================================
// State accessors
// ============================================

export async function getMatchState(matchId: string): Promise<Match | null> {
  const cached = await redisGet<Match>(REDIS_KEYS.matchState(matchId));
  if (cached) {
    // Rehydrate dates
    if (cached.startedAt) cached.startedAt = new Date(cached.startedAt);
    if (cached.endedAt) cached.endedAt = new Date(cached.endedAt);
    if (cached.createdAt) cached.createdAt = new Date(cached.createdAt);
    return cached;
  }

  // Fallback to DB
  const dbMatch = await prisma.match.findUnique({
    where: { id: matchId },
    include: { players: true, conclusions: true, scores: true },
  });

  if (!dbMatch) return null;

  const match: Match = {
    id: dbMatch.id,
    roomId: dbMatch.roomId,
    caseId: dbMatch.caseId,
    difficulty: dbMatch.difficulty as DifficultyLevel,
    status: dbMatch.status as Match['status'],
    phase: dbMatch.phase as GamePhase,
    playerIds: dbMatch.players.map((p) => p.playerId),
    maxPlayers: dbMatch.maxPlayers,
    startedAt: dbMatch.startedAt ?? undefined,
    endedAt: dbMatch.endedAt ?? undefined,
    durationSeconds: dbMatch.durationSeconds,
    timeRemainingSeconds: 0,
    conclusions: dbMatch.conclusions.map((c) => ({
      playerId: c.playerId,
      matchId: c.matchId,
      caseId: dbMatch.caseId,
      submittedAt: c.submittedAt,
      killerId: c.killerId,
      motive: c.motive as PlayerConclusion['motive'],
      weapon: c.weapon as PlayerConclusion['weapon'],
      location: c.location as PlayerConclusion['location'],
      timeline: c.timeline,
      narrative: c.narrative,
    })),
    scores: [],
    winnerId: dbMatch.winnerId ?? undefined,
    createdAt: dbMatch.createdAt,
  };

  return match;
}

export async function getCaseFomMatch(matchId: string): Promise<FullCase | null> {
  const match = await getMatchState(matchId);
  if (!match) return null;

  const cached = await redisGet<FullCase>(REDIS_KEYS.caseCache(match.caseId));
  if (cached) return cached;

  logger.warn('[MatchService] Case not found in cache', { caseId: match.caseId });
  return null;
}

// ============================================
// Helpers
// ============================================

async function persistCaseToDB(fullCase: FullCase): Promise<void> {
  await prisma.case.create({
    data: {
      id: fullCase.id,
      title: fullCase.title,
      description: fullCase.description,
      difficulty: mapDifficulty(fullCase.difficulty),
      status: 'active',
      seed: fullCase.seed,
      tags: fullCase.tags,
      victimName: fullCase.victim.name,
      victimAge: fullCase.victim.age,
      victimOccupation: fullCase.victim.occupation,
      victimBackstory: fullCase.victim.backstory,
      causeOfDeath: fullCase.victim.causeOfDeath,
      timeOfDeath: fullCase.victim.timeOfDeath,
      locationFound: fullCase.victim.locationFound,
      killerId: fullCase.solution.killerId,
      motive: fullCase.solution.motive,
      weapon: fullCase.solution.weapon,
      murderLocation: fullCase.solution.location,
      solutionTimeline: fullCase.solution.timeline,
      solutionMethod: fullCase.solution.method,
      solutionNarrative: fullCase.solution.narrative,
    },
  });

  // Persist suspects
  for (const suspect of fullCase.suspects) {
    await prisma.suspect.create({
      data: {
        id: suspect.id,
        caseId: fullCase.id,
        name: suspect.name,
        age: suspect.age,
        occupation: suspect.occupation,
        backstory: suspect.backstory,
        personality: suspect.personality,
        isKiller: suspect.isKiller,
        alibi: suspect.alibi.description,
        alibiIsTrue: suspect.alibi.isTrue,
        motive: suspect.motive ?? null,
        relationships: suspect.relationships as object,
      },
    });
  }

  // Persist witnesses
  for (const witness of fullCase.witnesses) {
    await prisma.witness.create({
      data: {
        id: witness.id,
        caseId: fullCase.id,
        name: witness.name,
        age: witness.age,
        occupation: witness.occupation,
        backstory: witness.backstory,
        personality: witness.personality,
        honestyLevel: witness.honestyLevel,
        isKiller: witness.isKiller,
        isSuspect: witness.isSuspect,
        alibi: witness.alibi.alibi,
        alibiIsTrue: witness.alibi.isTrue,
        knownFacts: witness.knownFacts,
        hiddenFacts: witness.hiddenFacts,
        relationships: witness.relationships as object,
        memories: witness.memories as object,
      },
    });
  }

  // Persist evidence
  for (const evidence of fullCase.evidencePool) {
    await prisma.evidence.create({
      data: {
        id: evidence.id,
        caseId: fullCase.id,
        type: evidence.type,
        name: evidence.name,
        description: evidence.description,
        location: evidence.location,
        isReal: evidence.isReal,
        isFakeEvidence: evidence.isFakeEvidence,
        reliability: evidence.reliability,
        pointsTo: evidence.pointsTo ?? null,
        imageUrl: evidence.imageUrl ?? null,
        metadata: evidence.metadata as object,
      },
    });
  }

  // Persist timeline
  for (const event of fullCase.timeline) {
    await prisma.timelineEvent.create({
      data: {
        id: event.id,
        caseId: fullCase.id,
        timestamp: event.timestamp,
        description: event.description,
        involvedIds: event.involvedIds,
        location: event.location,
        isKeyEvent: event.isKeyEvent,
        isPublicInfo: event.isPublicInfo,
        evidenceId: event.evidenceId ?? null,
        order: event.order,
      },
    });
  }
}

async function getConclusionId(matchId: string, playerId: string): Promise<string> {
  const c = await prisma.playerConclusion.findFirst({
    where: { matchId, playerId },
    select: { id: true },
  });
  return c?.id ?? '';
}

function mapDifficulty(d: DifficultyLevel) {
  const map = {
    easy: 'easy' as const,
    medium: 'medium' as const,
    hard: 'hard' as const,
    expert: 'expert' as const,
    nightmare: 'nightmare' as const,
  };
  return map[d];
}

// Re-export for convenience
export { murderEngine };
