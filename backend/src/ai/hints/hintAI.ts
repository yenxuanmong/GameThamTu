// ============================================
// Hint AI — manages hint allowance per player per match
// ============================================

import type { DifficultyLevel } from '../../types/case.types';
import type { FullCase } from '../../murder_engine/models/Case';
import type { InvestigationProgress } from '../../types/player.types';
import { generateHint, type Hint } from './hintGenerator';
import { getRedisClient, REDIS_KEYS } from '../../configs/redis';
import { MATCH } from '../../utils/constants';
import logger from '../../utils/logger';

interface HintState {
  playerId: string;
  matchId: string;
  hintsUsed: number;
  lastHintAt: number;    // timestamp ms
  hintHistory: Hint[];
}

// ============================================
// Request a hint
// ============================================

export async function requestHint(
  playerId: string,
  matchId: string,
  fullCase: FullCase,
  progress: InvestigationProgress,
  difficulty: DifficultyLevel
): Promise<{ success: boolean; hint?: Hint; error?: string; cooldownSeconds?: number }> {
  const state = await getHintState(playerId, matchId);

  // Enforce max hints
  if (state.hintsUsed >= MATCH.MAX_HINTS_PER_PLAYER) {
    return { success: false, error: `No hints remaining (used ${state.hintsUsed}/${MATCH.MAX_HINTS_PER_PLAYER})` };
  }

  // Enforce cooldown
  const now = Date.now();
  const elapsed = (now - state.lastHintAt) / 1000;
  if (state.lastHintAt > 0 && elapsed < MATCH.HINT_COOLDOWN_SECONDS) {
    const remaining = Math.ceil(MATCH.HINT_COOLDOWN_SECONDS - elapsed);
    return { success: false, error: 'Hint on cooldown', cooldownSeconds: remaining };
  }

  try {
    const hint = await generateHint(fullCase, progress, difficulty);

    const newState: HintState = {
      ...state,
      hintsUsed: state.hintsUsed + 1,
      lastHintAt: now,
      hintHistory: [...state.hintHistory, hint],
    };

    await saveHintState(newState);
    logger.debug('[HintAI] Hint provided', { playerId, matchId, hintsUsed: newState.hintsUsed });

    return { success: true, hint };
  } catch (err) {
    logger.error('[HintAI] Hint generation failed', { err });
    return { success: false, error: 'Failed to generate hint — try again' };
  }
}

export async function getHintsRemaining(playerId: string, matchId: string): Promise<number> {
  const state = await getHintState(playerId, matchId);
  return Math.max(0, MATCH.MAX_HINTS_PER_PLAYER - state.hintsUsed);
}

// ============================================
// Internal
// ============================================

async function getHintState(playerId: string, matchId: string): Promise<HintState> {
  const redis = getRedisClient();
  const key = `hint:${matchId}:${playerId}`;
  const raw = await redis.get(key);
  if (!raw) {
    return { playerId, matchId, hintsUsed: 0, lastHintAt: 0, hintHistory: [] };
  }
  return JSON.parse(raw) as HintState;
}

async function saveHintState(state: HintState): Promise<void> {
  const redis = getRedisClient();
  const key = `hint:${state.matchId}:${state.playerId}`;
  // TTL matches match state TTL
  const matchKey = REDIS_KEYS.matchState(state.matchId);
  void matchKey; // just for reference
  await redis.setex(key, 7200, JSON.stringify(state));
}
