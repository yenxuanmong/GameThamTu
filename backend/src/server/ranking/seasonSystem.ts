// ============================================
// Season System — manage competitive seasons
// ============================================

import { prisma } from '../../configs/database';
import logger from '../../utils/logger';

export interface SeasonInfo {
  id: string;
  number: number;
  name: string;
  startDate: Date;
  endDate: Date;
  isActive: boolean;
  daysRemaining: number;
}

// ============================================
// Get active season
// ============================================

export async function getActiveSeason(): Promise<SeasonInfo | null> {
  const season = await prisma.season.findFirst({
    where: { isActive: true },
  });

  if (!season) return null;

  const daysRemaining = Math.max(
    0,
    Math.ceil((season.endDate.getTime() - Date.now()) / (1000 * 60 * 60 * 24))
  );

  return { ...season, daysRemaining };
}

// ============================================
// Create new season
// ============================================

export async function createSeason(
  name: string,
  durationDays = 90
): Promise<SeasonInfo> {
  // Deactivate current season
  await prisma.season.updateMany({
    where: { isActive: true },
    data: { isActive: false },
  });

  const currentCount = await prisma.season.count();
  const startDate = new Date();
  const endDate = new Date(startDate.getTime() + durationDays * 24 * 60 * 60 * 1000);

  const season = await prisma.season.create({
    data: {
      number: currentCount + 1,
      name,
      startDate,
      endDate,
      isActive: true,
    },
  });

  logger.info('[SeasonSystem] New season created', {
    season: season.number,
    name,
    endDate,
  });

  return { ...season, daysRemaining: durationDays };
}

// ============================================
// End season — snapshot leaderboard, reset ranks
// ============================================

export async function endSeason(seasonId: string): Promise<void> {
  const season = await prisma.season.findUnique({ where: { id: seasonId } });
  if (!season) return;

  // Get final leaderboard
  const entries = await prisma.seasonLeaderboard.findMany({
    where: { seasonId },
    orderBy: { points: 'desc' },
  });

  // Assign final ranks
  for (let i = 0; i < entries.length; i++) {
    const entry = entries[i];
    if (!entry) continue;
    await prisma.seasonLeaderboard.update({
      where: { id: entry.id },
      data: { rank: i + 1 },
    });
  }

  // Soft-reset all player ranks (keep some points as carry-over)
  await prisma.playerRank.updateMany({
    data: { points: 0, streak: 0 },
  });

  // Deactivate season
  await prisma.season.update({
    where: { id: seasonId },
    data: { isActive: false },
  });

  logger.info('[SeasonSystem] Season ended', { seasonId, season: season.number });
}

// ============================================
// Check and auto-rotate season
// ============================================

export async function checkSeasonRotation(): Promise<boolean> {
  const season = await prisma.season.findFirst({ where: { isActive: true } });
  if (!season) return false;

  if (season.endDate < new Date()) {
    await endSeason(season.id);
    const seasonNumber = season.number;
    await createSeason(`Season ${seasonNumber + 1}`);
    logger.info('[SeasonSystem] Season auto-rotated', { old: seasonNumber, new: seasonNumber + 1 });
    return true;
  }

  return false;
}
