// ============================================
// Match Analytics
// — aggregated statistics per match
// ============================================

import { prisma } from '../configs/database';

export interface MatchAnalyticsSummary {
  matchId: string;
  caseId: string;
  difficulty: string;
  durationSeconds: number;
  playerCount: number;
  correctSolves: number;
  avgScore: number;
  highScore: number;
  winnerId: string | null;
  evidenceDiscoveryRate: number;  // avg across players 0–1
  avgNpcMessages: number;
  startedAt: Date | null;
  endedAt: Date | null;
}

// ============================================
// Build analytics summary for a finished match
// ============================================

export async function buildMatchSummary(matchId: string): Promise<MatchAnalyticsSummary | null> {
  const match = await prisma.match.findUnique({
    where: { id: matchId },
    include: {
      players: { select: { playerId: true, timeSpent: true } },
      scores: {
        select: {
          playerId: true,
          totalScore: true,
          isCorrect: true,
        },
      },
      discoveries: { select: { playerId: true } },
      dialogueSessions: { select: { playerId: true, messageCount: true } },
      case: { select: { id: true, _count: { select: { evidences: true } } } },
    },
  });

  if (!match) return null;

  const playerCount = match.players.length;
  const correctSolves = match.scores.filter((s) => s.isCorrect).length;
  const totalScore = match.scores.reduce((sum, s) => sum + s.totalScore, 0);
  const avgScore = playerCount > 0 ? Math.round(totalScore / playerCount) : 0;
  const highScore = match.scores.reduce((max, s) => Math.max(max, s.totalScore), 0);

  // Evidence discovery rate
  const totalEvidence = match.case._count.evidences;
  const discoveriesPerPlayer: Record<string, Set<string>> = {};
  for (const d of match.discoveries) {
    if (!discoveriesPerPlayer[d.playerId]) {
      discoveriesPerPlayer[d.playerId] = new Set();
    }
    discoveriesPerPlayer[d.playerId]!.add(d.playerId);
  }
  const discoveryRates = Object.values(discoveriesPerPlayer).map(
    (set) => (totalEvidence > 0 ? set.size / totalEvidence : 0)
  );
  const evidenceDiscoveryRate =
    discoveryRates.length > 0
      ? discoveryRates.reduce((s, r) => s + r, 0) / discoveryRates.length
      : 0;

  // Average NPC messages per player
  const npcMessagesByPlayer: Record<string, number> = {};
  for (const session of match.dialogueSessions) {
    npcMessagesByPlayer[session.playerId] =
      (npcMessagesByPlayer[session.playerId] ?? 0) + session.messageCount;
  }
  const npcMsgValues = Object.values(npcMessagesByPlayer);
  const avgNpcMessages =
    npcMsgValues.length > 0
      ? Math.round(npcMsgValues.reduce((s, v) => s + v, 0) / npcMsgValues.length)
      : 0;

  const durationSeconds =
    match.startedAt && match.endedAt
      ? Math.round((match.endedAt.getTime() - match.startedAt.getTime()) / 1000)
      : match.durationSeconds;

  return {
    matchId,
    caseId: match.caseId,
    difficulty: match.difficulty,
    durationSeconds,
    playerCount,
    correctSolves,
    avgScore,
    highScore,
    winnerId: match.winnerId,
    evidenceDiscoveryRate: parseFloat(evidenceDiscoveryRate.toFixed(3)),
    avgNpcMessages,
    startedAt: match.startedAt,
    endedAt: match.endedAt,
  };
}

// ============================================
// Global match stats (admin / leaderboard use)
// ============================================

export interface GlobalMatchStats {
  totalMatches: number;
  finishedMatches: number;
  activeMatches: number;
  avgCorrectSolveRate: number;
  avgMatchDuration: number;
  matchesByDifficulty: Record<string, number>;
}

export async function getGlobalMatchStats(): Promise<GlobalMatchStats> {
  const [total, finished, active, scores, durationAgg] = await prisma.$transaction([
    prisma.match.count(),
    prisma.match.count({ where: { status: 'finished' } }),
    prisma.match.count({ where: { status: 'active' } }),
    prisma.matchScore.aggregate({
      _count: { isCorrect: true },
      where: { isCorrect: true },
    }),
    prisma.match.aggregate({
      _avg: { durationSeconds: true },
      where: { status: 'finished' },
    }),
  ]);

  const totalScores = await prisma.matchScore.count();

  const diffCounts = await prisma.match.groupBy({
    by: ['difficulty'],
    _count: { id: true },
  });

  const matchesByDifficulty: Record<string, number> = {};
  for (const row of diffCounts) {
    matchesByDifficulty[row.difficulty] = row._count.id;
  }

  return {
    totalMatches: total,
    finishedMatches: finished,
    activeMatches: active,
    avgCorrectSolveRate:
      totalScores > 0 ? parseFloat(((scores._count.isCorrect ?? 0) / totalScores).toFixed(3)) : 0,
    avgMatchDuration: Math.round(durationAgg._avg.durationSeconds ?? 0),
    matchesByDifficulty,
  };
}
