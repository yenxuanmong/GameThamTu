// ============================================
// Adaptive Difficulty
// — adjusts game parameters based on player performance history
// ============================================

import type { DifficultyLevel } from '../../types/case.types';
import type { PlayerStats, PlayerRank } from '../../types/player.types';
import logger from '../../utils/logger';

export interface DifficultyRecommendation {
  recommended: DifficultyLevel;
  reason: string;
  confidence: number;   // 0–1
}

export interface AdaptiveModifiers {
  npcHonestyBoost: number;     // positive = more honest NPCs (easier)
  extraHints: number;          // additional hints beyond base allowance
  timeExtension: number;       // additional seconds
  fakeEvidenceReduction: number; // reduce fake evidence count
}

const DIFFICULTY_LEVELS: DifficultyLevel[] = ['easy', 'medium', 'hard', 'expert', 'nightmare'];

// ============================================
// Recommend difficulty based on player profile
// ============================================

export function recommendDifficulty(
  stats: PlayerStats,
  rank: PlayerRank
): DifficultyRecommendation {
  // New player
  if (stats.totalMatches < 5) {
    return {
      recommended: 'easy',
      reason: 'New player — starting with accessible difficulty',
      confidence: 0.9,
    };
  }

  const winRate = stats.totalWins / stats.totalMatches;
  const avgAccuracy = stats.avgAccuracy; // 0–1000
  const rankPoints = rank.points;

  // Determine current level index
  const currentLevel = rankBasedLevel(rankPoints);
  const currentIndex = DIFFICULTY_LEVELS.indexOf(currentLevel);

  // Performance scoring
  let performanceScore = 0;
  if (winRate > 0.65) performanceScore += 2;
  else if (winRate > 0.45) performanceScore += 1;
  else performanceScore -= 1;

  if (avgAccuracy > 750) performanceScore += 2;
  else if (avgAccuracy > 500) performanceScore += 1;
  else if (avgAccuracy < 300) performanceScore -= 1;

  if (stats.perfectSolves > stats.totalMatches * 0.2) performanceScore += 1;
  if (rank.streak > 3) performanceScore += 1;
  if (rank.streak < -3) performanceScore -= 1;

  // Adjust index
  let recommendedIndex = currentIndex;
  if (performanceScore >= 4) {
    recommendedIndex = Math.min(currentIndex + 1, DIFFICULTY_LEVELS.length - 1);
  } else if (performanceScore <= -2) {
    recommendedIndex = Math.max(currentIndex - 1, 0);
  }

  const recommended = DIFFICULTY_LEVELS[recommendedIndex] ?? 'medium';

  const reason =
    performanceScore >= 4
      ? `Strong performance (WR ${(winRate * 100).toFixed(0)}%, Avg ${avgAccuracy.toFixed(0)}) — stepping up`
      : performanceScore <= -2
        ? `Struggling recently — easing difficulty for recovery`
        : `Performance is consistent with current tier`;

  return {
    recommended,
    reason,
    confidence: Math.min(0.95, 0.5 + stats.totalMatches / 50),
  };
}

// ============================================
// Compute in-match adaptive modifiers
// ============================================

export function computeAdaptiveModifiers(
  stats: PlayerStats,
  currentDifficulty: DifficultyLevel
): AdaptiveModifiers {
  const winRate = stats.totalMatches > 0 ? stats.totalWins / stats.totalMatches : 0;
  const avgAccuracy = stats.avgAccuracy;

  // Struggling player gets subtle assistance
  const isStruggling = winRate < 0.25 || avgAccuracy < 250;
  // Dominating player gets reduced assistance
  const isDominating = winRate > 0.7 && avgAccuracy > 700;

  if (isStruggling) {
    return {
      npcHonestyBoost: 0.1,
      extraHints: 1,
      timeExtension: 120,
      fakeEvidenceReduction: 1,
    };
  }

  if (isDominating) {
    return {
      npcHonestyBoost: -0.05,
      extraHints: 0,
      timeExtension: 0,
      fakeEvidenceReduction: 0,
    };
  }

  return {
    npcHonestyBoost: 0,
    extraHints: 0,
    timeExtension: 0,
    fakeEvidenceReduction: 0,
  };
}

// ============================================
// Post-match difficulty feedback
// ============================================

export interface PostMatchFeedback {
  message: string;
  suggestedDifficulty: DifficultyLevel;
  shouldPromptChange: boolean;
}

export function generatePostMatchFeedback(
  result: 'win' | 'loss',
  score: number,
  currentDifficulty: DifficultyLevel,
  recentResults: ('win' | 'loss')[]
): PostMatchFeedback {
  const recentWins = recentResults.filter((r) => r === 'win').length;
  const winStreak = recentWins === recentResults.length && recentResults.length >= 3;
  const lossStreak = recentWins === 0 && recentResults.length >= 3;

  const currentIndex = DIFFICULTY_LEVELS.indexOf(currentDifficulty);

  if (winStreak && score > 800) {
    const next = DIFFICULTY_LEVELS[Math.min(currentIndex + 1, DIFFICULTY_LEVELS.length - 1)] ?? currentDifficulty;
    return {
      message: `Impressive — ${recentResults.length} wins in a row with high accuracy. You may be ready for a greater challenge.`,
      suggestedDifficulty: next,
      shouldPromptChange: next !== currentDifficulty,
    };
  }

  if (lossStreak) {
    const prev = DIFFICULTY_LEVELS[Math.max(currentIndex - 1, 0)] ?? currentDifficulty;
    return {
      message: `A difficult run. Don't be discouraged — a slight step back in difficulty can help rebuild momentum.`,
      suggestedDifficulty: prev,
      shouldPromptChange: prev !== currentDifficulty,
    };
  }

  return {
    message: result === 'win' ? 'Well played.' : 'Better luck next time.',
    suggestedDifficulty: currentDifficulty,
    shouldPromptChange: false,
  };
}

// ============================================
// Helpers
// ============================================

function rankBasedLevel(points: number): DifficultyLevel {
  if (points < 100) return 'easy';
  if (points < 300) return 'medium';
  if (points < 600) return 'hard';
  if (points < 1000) return 'expert';
  return 'nightmare';
}

// Suppress unused import warning
void logger;
