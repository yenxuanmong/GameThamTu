// ============================================
// Difficulty AI — static difficulty configuration
// ============================================

import type { DifficultyLevel, DifficultyConfig } from '../../types/case.types';
import { DIFFICULTY_CONFIGS } from '../../types/case.types';

// ============================================
// Get config for a difficulty level
// ============================================

export function getDifficultyConfig(level: DifficultyLevel): DifficultyConfig {
  return DIFFICULTY_CONFIGS[level];
}

// ============================================
// Calculate effective match duration with modifiers
// ============================================

export function getEffectiveMatchDuration(
  difficulty: DifficultyLevel,
  playerCount: number
): number {
  const base = DIFFICULTY_CONFIGS[difficulty].matchDurationSeconds;
  // With more players, slightly extend to compensate for coordination overhead
  const playerBonus = (playerCount - 2) * 30; // +30s per extra player above 2
  return base + playerBonus;
}

// ============================================
// Score difficulty modifier
// ============================================

export function getDifficultyScoreMultiplier(difficulty: DifficultyLevel): number {
  const multipliers: Record<DifficultyLevel, number> = {
    easy:      0.8,
    medium:    1.0,
    hard:      1.2,
    expert:    1.5,
    nightmare: 2.0,
  };
  return multipliers[difficulty];
}

// ============================================
// NPC honesty rate for difficulty
// ============================================

export function getNPCHonestyRate(difficulty: DifficultyLevel): number {
  return DIFFICULTY_CONFIGS[difficulty].npcHonestyRate;
}
