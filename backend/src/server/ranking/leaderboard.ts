// ============================================
// Leaderboard — season-based ranking with Redis sorted set
// ============================================

import { $Enums } from '@prisma/client';

import { getRedisClient, REDIS_KEYS } from '../../configs/redis';
import { prisma } from '../../configs/database';
import { CACHE_TTL } from '../../utils/constants';
import logger from '../../utils/logger';

export interface LeaderboardEntry {
  rank: number;
  playerId: string;
  username: string;
  points: number;
  wins: number;
  tier: string;
  avatarUrl?: string;
}

// ============================================
// Update player score (called after match ends)
// ============================================

export async function updateLeaderboardScore(
  playerId: string,
  season: number,
  rpChange: number,
  isWin: boolean
): Promise<void> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.leaderboard(season);

  // Atomically update sorted set
  await redis.zincrby(key, rpChange, playerId);
  await redis.expire(key, CACHE_TTL.CASE_CACHE); // keep for season duration

  // Update DB
  const playerRank = await prisma.playerRank.findUnique({
    where: { playerId },
    select: { points: true, wins: true, tier: true },
  });

  if (playerRank) {
    const newPoints = Math.max(0, playerRank.points + rpChange);
    const newTier = calculateTier(newPoints) as import('@prisma/client').RankTier;

    await prisma.playerRank.update({
      where: { playerId },
      data: {
        points: newPoints,
        peakPoints: { set: Math.max(playerRank.points, newPoints) },
        wins: isWin ? { increment: 1 } : playerRank.wins,
        losses: !isWin ? { increment: 1 } : undefined,
        tier: newTier,
        streak: isWin ? { increment: 1 } : { set: 0 },
      },
    });

    // Upsert season leaderboard entry
    await prisma.seasonLeaderboard.upsert({
      where: { seasonId_playerId: { seasonId: await getActiveSeasonId(), playerId } },
      update: {
        points: newPoints,
        wins: isWin ? { increment: 1 } : undefined,
        tier: newTier,
        rank: 0, // will be recalculated on read
        updatedAt: new Date(),
      },
      create: {
        seasonId: await getActiveSeasonId(),
        playerId,
        rank: 0,
        points: newPoints,
        wins: isWin ? 1 : 0,
        tier: newTier,
        updatedAt: new Date(),
      },
    });
  }

  logger.debug('[Leaderboard] Score updated', { playerId, rpChange, season });
}

// ============================================
// Get leaderboard page
// ============================================

export async function getLeaderboard(
  season: number,
  page = 1,
  pageSize = 50
): Promise<LeaderboardEntry[]> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.leaderboard(season);

  // Get top players by score (descending)
  const offset = (page - 1) * pageSize;
  const entries = await redis.zrevrangebyscore(
    key,
    '+inf',
    '-inf',
    'WITHSCORES',
    'LIMIT',
    offset,
    pageSize
  );

  if (entries.length === 0) {
    return fetchLeaderboardFromDB(season, page, pageSize);
  }

  // Parse WITHSCORES result: [playerId, score, playerId, score, ...]
  const result: LeaderboardEntry[] = [];
  for (let i = 0; i < entries.length; i += 2) {
    const playerId = entries[i];
    const points = parseInt(entries[i + 1] ?? '0', 10);
    if (!playerId) continue;

    const player = await prisma.player.findUnique({
      where: { id: playerId },
      select: { username: true, avatarUrl: true },
    });

    const rankData = await prisma.playerRank.findUnique({
      where: { playerId },
      select: { wins: true, tier: true },
    });

    result.push({
      rank: offset + result.length + 1,
      playerId,
      username: player?.username ?? 'Unknown',
      points,
      wins: rankData?.wins ?? 0,
      tier: rankData?.tier ?? 'rookie',
      avatarUrl: player?.avatarUrl ?? undefined,
    });
  }

  return result;
}

// ============================================
// Get a specific player's rank
// ============================================

export async function getPlayerRank(
  playerId: string,
  season: number
): Promise<{ rank: number; points: number } | null> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.leaderboard(season);

  const rank = await redis.zrevrank(key, playerId);
  const score = await redis.zscore(key, playerId);

  if (rank === null) return null;

  return { rank: rank + 1, points: parseInt(score ?? '0', 10) };
}

// ============================================
// Helpers
// ============================================

async function fetchLeaderboardFromDB(
  season: number,
  page: number,
  pageSize: number
): Promise<LeaderboardEntry[]> {
  const seasonRecord = await prisma.season.findUnique({ where: { number: season } });
  if (!seasonRecord) return [];

  const entries = await prisma.seasonLeaderboard.findMany({
    where: { seasonId: seasonRecord.id },
    orderBy: { points: 'desc' },
    skip: (page - 1) * pageSize,
    take: pageSize,
  });

  // Fetch player info separately
  const playerIds = entries.map((e) => e.playerId);
  const players = await prisma.player.findMany({
    where: { id: { in: playerIds } },
    select: { id: true, username: true, avatarUrl: true },
  });
  const playerMap = new Map(players.map((p) => [p.id, p]));

  return entries.map((e, i) => {
    const player = playerMap.get(e.playerId);
    return {
      rank: (page - 1) * pageSize + i + 1,
      playerId: e.playerId,
      username: player?.username ?? 'Unknown',
      points: e.points,
      wins: e.wins,
      tier: e.tier,
      avatarUrl: player?.avatarUrl ?? undefined,
    };
  });
}

function calculateTier(points: number): string {
  if (points < 100) return 'rookie';
  if (points < 300) return 'detective';
  if (points < 600) return 'inspector';
  if (points < 1000) return 'senior_inspector';
  if (points < 1500) return 'chief_inspector';
  if (points < 2500) return 'superintendent';
  if (points < 4000) return 'commissioner';
  return 'legendary';
}

let cachedSeasonId: string | null = null;
async function getActiveSeasonId(): Promise<string> {
  if (cachedSeasonId) return cachedSeasonId;
  const season = await prisma.season.findFirst({ where: { isActive: true } });
  cachedSeasonId = season?.id ?? '';
  return cachedSeasonId;
}
