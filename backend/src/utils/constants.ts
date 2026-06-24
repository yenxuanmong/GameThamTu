// ============================================
// Constants — Detective Royale
// ============================================

// Scoring
export const SCORE_WEIGHTS = {
  KILLER: 300,
  MOTIVE: 200,
  WEAPON: 150,
  LOCATION: 100,
  TIMELINE: 150,
  NARRATIVE: 100,
  TOTAL: 1000,
} as const;

export const TIME_BONUS = {
  MAX: 100,
  THRESHOLD_PERCENT: 0.5, // submit in first 50% of time to get bonus
} as const;

// Ranking Points
export const RP_CHANGES = {
  WIN_1ST: 30,
  WIN_2ND: 15,
  WIN_3RD: 5,
  LOSS: -10,
  PERFECT_SOLVE_BONUS: 20,
  ABANDON_PENALTY: -20,
} as const;

// Match
export const MATCH = {
  MAX_PLAYERS: 4,
  MIN_PLAYERS: 2,
  DEFAULT_DURATION: 1800,
  COUNTDOWN_SECONDS: 10,
  FINAL_MINUTES_THRESHOLD: 300, // 5 minutes
  MAX_HINTS_PER_PLAYER: 3,
  HINT_COOLDOWN_SECONDS: 120,
} as const;

// NPC Dialogue
export const NPC = {
  MAX_MESSAGES_PER_SESSION: 20,
  STRESS_INCREASE_PER_ACCUSATION: 0.2,
  TRUST_INCREASE_PER_CORRECT_GUESS: 0.15,
  TRUST_DECREASE_PER_HOSTILE: 0.1,
  MAX_INTERROGATIONS_BEFORE_HOSTILE: 5,
} as const;

// Queue
export const QUEUE = {
  TIMEOUT_SECONDS: 60,
  MAX_RANK_DIFF_FOR_MATCH: 200, // RP difference
  EXPAND_RANGE_EVERY_SECONDS: 15,
  EXPAND_RANGE_BY: 100,
} as const;

// Cache TTL (seconds)
export const CACHE_TTL = {
  MATCH_STATE: 7200,      // 2 hours
  ROOM_STATE: 3600,       // 1 hour
  PLAYER_SESSION: 86400,  // 24 hours
  CASE_CACHE: 86400 * 7,  // 7 days
  LEADERBOARD: 300,       // 5 minutes
} as const;
