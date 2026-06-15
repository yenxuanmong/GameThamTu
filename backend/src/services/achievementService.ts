// ============================================
// Achievement Service
// Checks conditions and unlocks achievements after each match.
// ============================================

import { prisma } from '../configs/database';
import logger from '../utils/logger';

// ============================================
// Types
// ============================================

export interface AchievementResult {
  id: string;
  title: string;
  description: string;
  category: string;
  iconKey: string;
  unlocked: boolean;
  unlockedAt: string | null;
  progress: number;
  maxProgress: number;
  isHidden: boolean;
}

export interface UnlockEvent {
  achievementId: string;
  title: string;
  iconKey: string;
}

// ============================================
// Get all achievements for a player
// ============================================

export async function getPlayerAchievements(
  playerId: string
): Promise<AchievementResult[]> {
  const [definitions, playerRows] = await Promise.all([
    prisma.achievementDefinition.findMany({
      where: { isActive: true },
      orderBy: { sortOrder: 'asc' },
    }),
    prisma.playerAchievement.findMany({
      where: { playerId },
    }),
  ]);

  const map = new Map(playerRows.map((r) => [r.achievementId, r]));

  return definitions.map((def) => {
    const row = map.get(def.id);
    return {
      id:          def.id,
      title:       def.title,
      description: def.description,
      category:    def.category,
      iconKey:     def.iconKey,
      maxProgress: def.maxProgress,
      isHidden:    def.isHidden && !(row?.unlocked ?? false), // hide until unlocked
      unlocked:    row?.unlocked ?? false,
      unlockedAt:  row?.unlockedAt?.toISOString() ?? null,
      progress:    row?.progress ?? 0,
    };
  });
}

// ============================================
// Increment progress & unlock if threshold met
// Returns list of newly unlocked achievements
// ============================================

export async function incrementAchievement(
  playerId: string,
  achievementId: string,
  amount = 1
): Promise<UnlockEvent | null> {
  const def = await prisma.achievementDefinition.findUnique({
    where: { id: achievementId },
  });
  if (!def || !def.isActive) return null;

  const existing = await prisma.playerAchievement.findUnique({
    where: { playerId_achievementId: { playerId, achievementId } },
  });

  if (existing?.unlocked) return null; // already unlocked

  const newProgress = Math.min(
    (existing?.progress ?? 0) + amount,
    def.maxProgress
  );
  const willUnlock = newProgress >= def.maxProgress;

  await prisma.playerAchievement.upsert({
    where: { playerId_achievementId: { playerId, achievementId } },
    update: {
      progress:   newProgress,
      unlocked:   willUnlock,
      unlockedAt: willUnlock ? new Date() : undefined,
    },
    create: {
      playerId,
      achievementId,
      progress:   newProgress,
      unlocked:   willUnlock,
      unlockedAt: willUnlock ? new Date() : undefined,
    },
  });

  if (willUnlock) {
    logger.info('[Achievement] Unlocked', { playerId, achievementId });
    return { achievementId: def.id, title: def.title, iconKey: def.iconKey };
  }

  return null;
}

// ============================================
// Check achievements after a match finishes
// Returns all newly unlocked achievements for this player
// ============================================

export async function checkMatchAchievements(
  playerId: string,
  matchId: string
): Promise<UnlockEvent[]> {
  const unlocked: UnlockEvent[] = [];

  const score = await prisma.matchScore.findUnique({
    where: { matchId_playerId: { matchId, playerId } },
    include: { match: true },
  });

  if (!score) return unlocked;

  const stats = await prisma.playerStats.findUnique({ where: { playerId } });
  if (!stats) return unlocked;

  const push = (e: UnlockEvent | null) => { if (e) unlocked.push(e); };

  // ---- First match ----
  if (stats.totalMatches === 1)
    push(await incrementAchievement(playerId, 'first_match'));

  // ---- First correct solve ----
  if (score.isCorrect) {
    push(await incrementAchievement(playerId, 'first_solve'));

    // ---- Perfect score (1000 pts) ----
    if (score.totalScore >= 1000)
      push(await incrementAchievement(playerId, 'perfect_score'));

    // ---- Speed solver (< 10 min) ----
    const mp = await prisma.matchPlayer.findUnique({
      where: { matchId_playerId: { matchId, playerId } },
    });
    if (mp && mp.timeSpent < 600)
      push(await incrementAchievement(playerId, 'speed_solver'));
  }

  // ---- Veteran milestones (progress achievements) ----
  push(await incrementAchievement(playerId, 'play_10'));
  push(await incrementAchievement(playerId, 'play_50'));
  push(await incrementAchievement(playerId, 'play_100'));

  // ---- Win streak ----
  const rank = await prisma.playerRank.findUnique({ where: { playerId } });
  if (rank && rank.streak >= 5)
    push(await incrementAchievement(playerId, 'win_streak_5'));

  // ---- Evidence collector ----
  const evidenceCount = await prisma.evidenceDiscovery.count({
    where: { matchId, playerId },
  });
  if (evidenceCount >= 10)
    push(await incrementAchievement(playerId, 'evidence_collector'));

  // ---- NPC whisperer (interrogated ≥ 3 witnesses) ----
  const npcCount = await prisma.nPCDialogueSession.count({
    where: { matchId, playerId },
  });
  if (npcCount >= 3)
    push(await incrementAchievement(playerId, 'npc_whisperer'));

  return unlocked;
}

// ============================================
// Seed default achievements (run once on startup)
// ============================================

export async function seedAchievements(): Promise<void> {
  const defaults = [
    // Detective
    { id: 'first_match',       title: 'First Case',          description: 'Play your first match.',                    category: 'detective', iconKey: 'badge_star',       maxProgress: 1,  sortOrder: 0  },
    { id: 'first_solve',       title: 'The Detective',       description: 'Correctly solve your first case.',          category: 'detective', iconKey: 'badge_magnify',    maxProgress: 1,  sortOrder: 1  },
    { id: 'perfect_score',     title: 'Perfect Deduction',   description: 'Score 1000 points in a single match.',      category: 'master',    iconKey: 'badge_perfect',    maxProgress: 1,  sortOrder: 2  },
    { id: 'speed_solver',      title: 'Speed Demon',         description: 'Solve a case in under 10 minutes.',         category: 'detective', iconKey: 'badge_lightning',  maxProgress: 1,  sortOrder: 3  },
    { id: 'win_streak_5',      title: 'On a Roll',           description: 'Win 5 matches in a row.',                   category: 'detective', iconKey: 'badge_streak',     maxProgress: 1,  sortOrder: 4  },
    // Veteran
    { id: 'play_10',           title: 'Investigator',        description: 'Play 10 matches.',                          category: 'veteran',   iconKey: 'badge_10',         maxProgress: 10, sortOrder: 10 },
    { id: 'play_50',           title: 'Senior Detective',    description: 'Play 50 matches.',                          category: 'veteran',   iconKey: 'badge_50',         maxProgress: 50, sortOrder: 11 },
    { id: 'play_100',          title: 'Commissioner',        description: 'Play 100 matches.',                         category: 'veteran',   iconKey: 'badge_100',        maxProgress: 100, sortOrder: 12 },
    // Collector
    { id: 'evidence_collector',title: 'Crime Scene Expert',  description: 'Collect 10 pieces of evidence in one match.',category: 'collector', iconKey: 'badge_evidence',   maxProgress: 1,  sortOrder: 20 },
    { id: 'npc_whisperer',     title: 'Witness Whisperer',   description: 'Interrogate 3 witnesses in one match.',     category: 'social',    iconKey: 'badge_npc',        maxProgress: 1,  sortOrder: 21 },
    // Special (hidden)
    { id: 'no_hints',          title: 'No Hints Needed',     description: 'Solve a case without using any hints.',     category: 'special',   iconKey: 'badge_nohints',    maxProgress: 1,  sortOrder: 30, isHidden: true },
    { id: 'nightmare_solve',   title: 'Nightmare Solver',    description: 'Correctly solve a Nightmare difficulty case.', category: 'special', iconKey: 'badge_nightmare', maxProgress: 1,  sortOrder: 31, isHidden: true },
  ] as const;

  for (const a of defaults) {
    await prisma.achievementDefinition.upsert({
      where: { id: a.id },
      update: {},
      create: {
        id:          a.id,
        title:       a.title,
        description: a.description,
        category:    a.category as any,
        iconKey:     a.iconKey,
        maxProgress: a.maxProgress,
        isHidden:    (a as any).isHidden ?? false,
        sortOrder:   a.sortOrder,
      },
    });
  }

  logger.info('[Achievement] Seed complete');
}
