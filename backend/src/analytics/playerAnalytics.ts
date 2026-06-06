// ============================================
// Player Analytics
// — per-player performance tracking and aggregation
// ============================================

import { prisma } from '../configs/database';
import type { DifficultyLevel } from '../types/case.types';

export interface PlayerPerformanceReport {
  playerId: string;
  username: string;
  totalMatches: number;
  totalWins: number;
  winRate: number;
  avgScore: number;
  highScore: number;
  perfectSolves: number;
  avgTimeToSolve: number;       // seconds
  killerIdentifiedRate: number; // 0–1
  difficultyBreakdown: Record<DifficultyLevel, { played: number; won: number; avgScore: number }>;
  recentTrend: 'improving' | 'declining' | 'stable';
  favoriteWeapon: string | null;
  favoriteSuspectOccupation: string | null;
}

// ============================================
// Build full performance report for a player
// ============================================

export async function buildPlayerReport(playerId: string): Promise<PlayerPerformanceReport | null> {
  const player = await prisma.player.findUnique({
    where: { id: playerId },
    select: { id: true, username: true, stats: true },
  });

  if (!player) return null;

  const [scores, recentScores] = await prisma.$transaction([
    prisma.matchScore.findMany({
      where: { playerId },
      select: {
        totalScore: true,
        isCorrect: true,
        result: true,
        match: { select: { difficulty: true, endedAt: true, durationSeconds: true } },
        conclusion: { select: { weapon: true, killerId: true } },
      },
    }),
    prisma.matchScore.findMany({
      where: { playerId },
      orderBy: { match: { endedAt: 'desc' } },
      take: 10,
      select: { totalScore: true, result: true },
    }),
  ]);

  const totalMatches = scores.length;
  const totalWins = scores.filter((s) => s.result === 'win').length;
  const winRate = totalMatches > 0 ? totalWins / totalMatches : 0;

  const totalScore = scores.reduce((sum, s) => sum + s.totalScore, 0);
  const avgScore = totalMatches > 0 ? Math.round(totalScore / totalMatches) : 0;
  const highScore = scores.reduce((max, s) => Math.max(max, s.totalScore), 0);

  const perfectSolves = scores.filter((s) => s.isCorrect).length;
  const killerIdentifiedRate = totalMatches > 0 ? perfectSolves / totalMatches : 0;

  // Avg time to solve (from match duration vs how long remaining — approximate)
  const avgTimeToSolve =
    scores.length > 0
      ? Math.round(
          scores.reduce((sum, s) => sum + s.match.durationSeconds, 0) / scores.length
        )
      : 0;

  // Difficulty breakdown
  const diffBreakdown: Record<string, { played: number; won: number; scoreSum: number }> = {};
  for (const s of scores) {
    const diff = s.match.difficulty;
    if (!diffBreakdown[diff]) diffBreakdown[diff] = { played: 0, won: 0, scoreSum: 0 };
    diffBreakdown[diff]!.played++;
    if (s.result === 'win') diffBreakdown[diff]!.won++;
    diffBreakdown[diff]!.scoreSum += s.totalScore;
  }

  const difficultyBreakdown = {} as Record<DifficultyLevel, { played: number; won: number; avgScore: number }>;
  for (const [diff, data] of Object.entries(diffBreakdown)) {
    difficultyBreakdown[diff as DifficultyLevel] = {
      played: data.played,
      won: data.won,
      avgScore: data.played > 0 ? Math.round(data.scoreSum / data.played) : 0,
    };
  }

  // Recent trend (last 5 vs previous 5)
  const last5 = recentScores.slice(0, 5);
  const prev5 = recentScores.slice(5, 10);
  const last5Avg = last5.length > 0 ? last5.reduce((s, r) => s + r.totalScore, 0) / last5.length : 0;
  const prev5Avg = prev5.length > 0 ? prev5.reduce((s, r) => s + r.totalScore, 0) / prev5.length : 0;

  let recentTrend: 'improving' | 'declining' | 'stable' = 'stable';
  if (prev5Avg > 0) {
    const delta = (last5Avg - prev5Avg) / prev5Avg;
    if (delta > 0.1) recentTrend = 'improving';
    else if (delta < -0.1) recentTrend = 'declining';
  }

  // Favorite weapon (most accused)
  const weaponCounts: Record<string, number> = {};
  for (const s of scores) {
    const w = s.conclusion.weapon;
    weaponCounts[w] = (weaponCounts[w] ?? 0) + 1;
  }
  const favoriteWeapon =
    Object.entries(weaponCounts).sort((a, b) => b[1] - a[1])[0]?.[0] ?? null;

  return {
    playerId: player.id,
    username: player.username,
    totalMatches,
    totalWins,
    winRate: parseFloat(winRate.toFixed(3)),
    avgScore,
    highScore,
    perfectSolves,
    avgTimeToSolve,
    killerIdentifiedRate: parseFloat(killerIdentifiedRate.toFixed(3)),
    difficultyBreakdown,
    recentTrend,
    favoriteWeapon,
    favoriteSuspectOccupation: null, // would need additional join
  };
}

// ============================================
// Update player stats after a match (called by matchService)
// ============================================

export async function updatePlayerStats(
  playerId: string,
  score: number,
  isWin: boolean,
  isCorrect: boolean,
  timeTaken: number
): Promise<void> {
  const existing = await prisma.playerStats.findUnique({
    where: { playerId },
    select: {
      totalMatches: true,
      totalWins: true,
      totalAccuracyScore: true,
      avgTimeToSolve: true,
      perfectSolves: true,
      killerIdentifiedCount: true,
    },
  });

  if (!existing) return;

  const newTotal = existing.totalMatches + 1;
  const newWins = existing.totalWins + (isWin ? 1 : 0);
  const newAccuracy = existing.totalAccuracyScore + score;
  const newAvgAccuracy = Math.round(newAccuracy / newTotal);
  const newAvgTime =
    Math.round((existing.avgTimeToSolve * existing.totalMatches + timeTaken) / newTotal);
  const newPerfect = existing.perfectSolves + (isCorrect ? 1 : 0);
  const newKillerCount = existing.killerIdentifiedCount + (isCorrect ? 1 : 0);

  await prisma.playerStats.update({
    where: { playerId },
    data: {
      totalMatches: newTotal,
      totalWins: newWins,
      totalAccuracyScore: newAccuracy,
      avgAccuracy: newAvgAccuracy,
      avgTimeToSolve: newAvgTime,
      perfectSolves: newPerfect,
      killerIdentifiedCount: newKillerCount,
    },
  });
}

// ============================================
// Get leaderboard-style player comparison
// ============================================

export async function comparePlayerStats(playerIds: string[]) {
  const stats = await prisma.playerStats.findMany({
    where: { playerId: { in: playerIds } },
    include: {
      player: { select: { username: true, avatarUrl: true } },
    },
  });

  return stats.map((s) => ({
    playerId: s.playerId,
    username: s.player.username,
    avatarUrl: s.player.avatarUrl,
    totalMatches: s.totalMatches,
    totalWins: s.totalWins,
    avgAccuracy: Math.round(s.avgAccuracy),
    perfectSolves: s.perfectSolves,
    winRate: s.totalMatches > 0 ? parseFloat((s.totalWins / s.totalMatches).toFixed(3)) : 0,
  }));
}
