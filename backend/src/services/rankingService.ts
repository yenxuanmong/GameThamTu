// ============================================
// Ranking Service
// ============================================

import { prisma } from '../configs/database';
import { getLeaderboard, getPlayerRank, updateLeaderboardScore } from '../server/ranking/leaderboard';
import { getActiveSeason, createSeason } from '../server/ranking/seasonSystem';
import type { RankTier } from '../types/player.types';
import logger from '../utils/logger';

export interface PlayerRankInfo {
  playerId: string;
  username: string;
  avatarUrl?: string;
  tier: RankTier;
  points: number;
  peakPoints: number;
  wins: number;
  losses: number;
  winRate: number;
  streak: number;
  globalRank?: number;
  season: number;
}

// ============================================
// Get player rank info
// ============================================

export async function getPlayerRankInfo(playerId: string): Promise<PlayerRankInfo | null> {
  const [player, rankData] = await prisma.$transaction([
    prisma.player.findUnique({
      where: { id: playerId },
      select: { id: true, username: true, avatarUrl: true },
    }),
    prisma.playerRank.findUnique({
      where: { playerId },
    }),
  ]);

  if (!player || !rankData) return null;

  const season = await getActiveSeason();
  const globalRankData = season
    ? await getPlayerRank(playerId, season.number)
    : null;

  return {
    playerId: player.id,
    username: player.username,
    avatarUrl: player.avatarUrl ?? undefined,
    tier: rankData.tier as RankTier,
    points: rankData.points,
    peakPoints: rankData.peakPoints,
    wins: rankData.wins,
    losses: rankData.losses,
    winRate: rankData.wins + rankData.losses > 0
      ? rankData.wins / (rankData.wins + rankData.losses)
      : 0,
    streak: rankData.streak,
    globalRank: globalRankData?.rank,
    season: season?.number ?? 1,
  };
}

// ============================================
// Get leaderboard
// ============================================

export async function getLeaderboardPage(
  page = 1,
  pageSize = 50,
  season?: number
) {
  const activeSeason = await getActiveSeason();
  const targetSeason = season ?? activeSeason?.number ?? 1;
  return getLeaderboard(targetSeason, page, pageSize);
}

// ============================================
// Get season info
// ============================================

export async function getSeasonInfo() {
  const season = await getActiveSeason();
  if (!season) return null;

  const playerCount = await prisma.seasonLeaderboard.count({
    where: { seasonId: season.id },
  });

  return {
    ...season,
    playerCount,
  };
}

// ============================================
// Get all seasons
// ============================================

export async function getAllSeasons() {
  return prisma.season.findMany({
    orderBy: { number: 'desc' },
    select: {
      id: true,
      number: true,
      name: true,
      startDate: true,
      endDate: true,
      isActive: true,
    },
  });
}

// ============================================
// Get match history for a player
// ============================================

export async function getPlayerMatchHistory(
  playerId: string,
  page = 1,
  pageSize = 20
) {
  const [matchPlayers, total] = await prisma.$transaction([
    prisma.matchPlayer.findMany({
      where: { playerId },
      orderBy: { joinedAt: 'desc' },
      skip: (page - 1) * pageSize,
      take: pageSize,
      select: {
        joinedAt: true,
        matchId: true,
        match: {
          select: {
            id: true,
            difficulty: true,
            status: true,
            startedAt: true,
            endedAt: true,
            caseId: true,
          },
        },
      },
    }),
    prisma.matchPlayer.count({ where: { playerId } }),
  ]);

  // Fetch scores separately
  const matchIds = matchPlayers.map((mp) => mp.matchId);
  const scores = await prisma.matchScore.findMany({
    where: { matchId: { in: matchIds }, playerId },
    select: {
      matchId: true,
      totalScore: true,
      isCorrect: true,
      finalRank: true,
      rpChange: true,
      result: true,
    },
  });
  const scoreMap = new Map(scores.map((s) => [s.matchId, s]));

  return {
    history: matchPlayers.map((mp) => {
      const score = scoreMap.get(mp.matchId);
      return {
        matchId: mp.match.id,
        difficulty: mp.match.difficulty,
        status: mp.match.status,
        startedAt: mp.match.startedAt,
        endedAt: mp.match.endedAt,
        caseId: mp.match.caseId,
        score: score?.totalScore ?? null,
        isCorrect: score?.isCorrect ?? null,
        rank: score?.finalRank ?? null,
        rpChange: score?.rpChange ?? null,
        result: score?.result ?? null,
      };
    }),
    total,
    page,
    pageSize,
  };
}

// ============================================
// Apply RP change after match
// ============================================

export async function applyRpChange(
  playerId: string,
  rpChange: number,
  isWin: boolean
): Promise<void> {
  const season = await getActiveSeason();
  if (!season) {
    logger.warn('[RankingService] No active season for RP update', { playerId });
    return;
  }
  await updateLeaderboardScore(playerId, season.number, rpChange, isWin);
}

// ============================================
// Get top players around a given player
// ============================================

export async function getNearbyRankings(
  playerId: string,
  range = 5
): Promise<{ above: any[]; player: any; below: any[] }> {
  const season = await getActiveSeason();
  if (!season) return { above: [], player: null, below: [] };

  const playerRankData = await getPlayerRank(playerId, season.number);
  if (!playerRankData) return { above: [], player: null, below: [] };

  const rank = playerRankData.rank;
  const page = Math.max(1, Math.floor((rank - range) / 50) + 1);
  const entries = await getLeaderboard(season.number, page, 50 + range * 2);

  const playerIndex = entries.findIndex((e) => e.playerId === playerId);

  return {
    above: entries.slice(Math.max(0, playerIndex - range), playerIndex),
    player: entries[playerIndex] ?? null,
    below: entries.slice(playerIndex + 1, playerIndex + range + 1),
  };
}
