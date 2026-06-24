// ============================================
// Accuracy Analytics
// — breakdown of how accurately players solve cases
// ============================================

import { prisma } from '../configs/database';

export interface AccuracyBreakdown {
  playerId: string;
  totalSubmissions: number;
  killerAccuracy: number;       // 0–1 (correct killer / total)
  motiveAccuracy: number;
  weaponAccuracy: number;
  locationAccuracy: number;
  fullCorrectRate: number;      // isCorrect / total
  avgScore: number;
  avgTimeBonus: number;
}

// ============================================
// Per-player accuracy breakdown across all matches
// ============================================

export async function getPlayerAccuracyBreakdown(playerId: string): Promise<AccuracyBreakdown | null> {
  const [scores, conclusions] = await prisma.$transaction([
    prisma.matchScore.findMany({
      where: { playerId },
      select: {
        totalScore: true,
        killerScore: true,
        motiveScore: true,
        weaponScore: true,
        locationScore: true,
        timeBonus: true,
        isCorrect: true,
      },
    }),
    prisma.playerConclusion.findMany({
      where: { playerId },
      select: {
        killerId: true,
        motive: true,
        weapon: true,
        location: true,
        match: {
          select: {
            case: {
              select: {
                killerId: true,
                motive: true,
                weapon: true,
                murderLocation: true,
              },
            },
          },
        },
      },
    }),
  ]);

  if (scores.length === 0) return null;

  const total = scores.length;

  // Score-based accuracy rates
  // killer: 300 max, motive: 200, weapon: 150, location: 100
  const killerAccuracy = parseFloat(
    (scores.reduce((sum, s) => sum + s.killerScore, 0) / (total * 300)).toFixed(3)
  );
  const motiveAccuracy = parseFloat(
    (scores.reduce((sum, s) => sum + s.motiveScore, 0) / (total * 200)).toFixed(3)
  );
  const weaponAccuracy = parseFloat(
    (scores.reduce((sum, s) => sum + s.weaponScore, 0) / (total * 150)).toFixed(3)
  );
  const locationAccuracy = parseFloat(
    (scores.reduce((sum, s) => sum + s.locationScore, 0) / (total * 100)).toFixed(3)
  );

  const fullCorrectRate = parseFloat(
    (scores.filter((s) => s.isCorrect).length / total).toFixed(3)
  );

  const avgScore = Math.round(
    scores.reduce((sum, s) => sum + s.totalScore, 0) / total
  );

  const avgTimeBonus = Math.round(
    scores.reduce((sum, s) => sum + s.timeBonus, 0) / total
  );

  return {
    playerId,
    totalSubmissions: total,
    killerAccuracy,
    motiveAccuracy,
    weaponAccuracy,
    locationAccuracy,
    fullCorrectRate,
    avgScore,
    avgTimeBonus,
  };
}

// ============================================
// Aggregated accuracy stats for a case
// ============================================

export interface CaseAccuracyStats {
  caseId: string;
  totalAttempts: number;
  killerCorrectRate: number;
  motiveCorrectRate: number;
  weaponCorrectRate: number;
  locationCorrectRate: number;
  fullSolveRate: number;
  avgScore: number;
  hardestField: string;  // field with lowest correct rate
}

export async function getCaseAccuracyStats(caseId: string): Promise<CaseAccuracyStats | null> {
  const dbCase = await prisma.case.findUnique({
    where: { id: caseId },
    select: {
      killerId: true,
      motive: true,
      weapon: true,
      murderLocation: true,
    },
  });

  if (!dbCase) return null;

  const conclusions = await prisma.playerConclusion.findMany({
    where: { match: { caseId } },
    select: {
      killerId: true,
      motive: true,
      weapon: true,
      location: true,
      score: { select: { isCorrect: true, totalScore: true } },
    },
  });

  const total = conclusions.length;
  if (total === 0) return null;

  const killerCorrect = conclusions.filter((c) => c.killerId === dbCase.killerId).length;
  const motiveCorrect = conclusions.filter((c) => c.motive === dbCase.motive).length;
  const weaponCorrect = conclusions.filter((c) => c.weapon === dbCase.weapon).length;
  const locationCorrect = conclusions.filter((c) => c.location === dbCase.murderLocation).length;
  const fullSolves = conclusions.filter((c) => c.score?.isCorrect).length;

  const avgScore = Math.round(
    conclusions.reduce((sum, c) => sum + (c.score?.totalScore ?? 0), 0) / total
  );

  const rates = {
    killer: killerCorrect / total,
    motive: motiveCorrect / total,
    weapon: weaponCorrect / total,
    location: locationCorrect / total,
  };

  const hardestField = (Object.entries(rates) as [string, number][])
    .sort((a, b) => a[1] - b[1])[0]?.[0] ?? 'killer';

  return {
    caseId,
    totalAttempts: total,
    killerCorrectRate: parseFloat((killerCorrect / total).toFixed(3)),
    motiveCorrectRate: parseFloat((motiveCorrect / total).toFixed(3)),
    weaponCorrectRate: parseFloat((weaponCorrect / total).toFixed(3)),
    locationCorrectRate: parseFloat((locationCorrect / total).toFixed(3)),
    fullSolveRate: parseFloat((fullSolves / total).toFixed(3)),
    avgScore,
    hardestField,
  };
}

// ============================================
// Accuracy trend over time (last N matches)
// ============================================

export async function getAccuracyTrend(
  playerId: string,
  limit = 20
): Promise<Array<{ matchId: string; score: number; isCorrect: boolean; endedAt: Date | null }>> {
  const scores = await prisma.matchScore.findMany({
    where: { playerId },
    orderBy: { match: { endedAt: 'desc' } },
    take: limit,
    select: {
      matchId: true,
      totalScore: true,
      isCorrect: true,
      match: { select: { endedAt: true } },
    },
  });

  return scores.map((s) => ({
    matchId: s.matchId,
    score: s.totalScore,
    isCorrect: s.isCorrect,
    endedAt: s.match.endedAt,
  })).reverse(); // oldest first for trend display
}
