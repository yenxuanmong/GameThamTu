// ============================================
// Honesty System
// — determines whether an NPC tells truth, lies, or deflects
// ============================================

import type { HonestyLevel, StressLevel } from '../../types/witness.types';
import type { SeededRandom } from '../../utils/random';
import { stressToScore } from './stressSystem';

export type ResponseDisposition = 'truthful' | 'deflect' | 'lie' | 'partial';

export interface HonestyDecision {
  disposition: ResponseDisposition;
  shouldRevealHiddenFact: boolean;
  trustBonus: number;           // applied to trust score after this message
  systemHint: string;           // injected into next system prompt
}

// Base truth probability per honesty level
const BASE_TRUTH_PROBABILITY: Record<HonestyLevel, number> = {
  always_honest:  0.97,
  mostly_honest:  0.80,
  mixed:          0.55,
  mostly_lying:   0.25,
  always_lying:   0.05,
};

// ============================================
// Decide how the NPC will respond to this message
// ============================================

export function decideHonestyDisposition(
  honestyLevel: HonestyLevel,
  currentStress: StressLevel,
  trustScore: number,
  isKiller: boolean,
  messageIsAccusation: boolean,
  rng: SeededRandom
): HonestyDecision {
  let truthProb = BASE_TRUTH_PROBABILITY[honestyLevel];

  // High stress nudges toward lying (self-protection) or accidental truth
  const stressScore = stressToScore(currentStress);
  if (stressScore > 0.6) {
    if (isKiller) {
      truthProb -= 0.15; // killer clamps down under pressure
    } else {
      // Non-killers under extreme stress may accidentally reveal more
      truthProb += stressScore > 0.85 ? 0.1 : 0;
    }
  }

  // High trust increases honesty
  truthProb += trustScore * 0.15;

  // Direct accusations make liars lie harder
  if (messageIsAccusation && truthProb < 0.5) {
    truthProb -= 0.1;
  }

  truthProb = Math.max(0.02, Math.min(0.98, truthProb));

  const roll = rng.next();
  let disposition: ResponseDisposition;

  if (roll < truthProb) {
    disposition = 'truthful';
  } else if (roll < truthProb + 0.15) {
    disposition = 'partial';
  } else if (roll < truthProb + 0.30) {
    disposition = 'deflect';
  } else {
    disposition = 'lie';
  }

  // Should reveal a hidden fact? (only if truthful and high trust)
  const shouldRevealHiddenFact =
    disposition === 'truthful' &&
    trustScore > 0.65 &&
    stressScore < 0.5 &&
    rng.nextBool(0.35);

  // Trust adjustments
  const trustBonus =
    disposition === 'truthful'
      ? 0.03
      : disposition === 'lie'
        ? -0.05
        : 0;

  const systemHint = buildSystemHint(disposition, shouldRevealHiddenFact, stressScore);

  return {
    disposition,
    shouldRevealHiddenFact,
    trustBonus,
    systemHint,
  };
}

function buildSystemHint(
  disposition: ResponseDisposition,
  shouldReveal: boolean,
  stressScore: number
): string {
  const parts: string[] = [];

  switch (disposition) {
    case 'truthful':
      parts.push('[Respond honestly and directly to the question.]');
      break;
    case 'partial':
      parts.push('[Answer the surface question truthfully but omit the most significant detail you know.]');
      break;
    case 'deflect':
      parts.push('[Avoid directly answering. Redirect the conversation or claim uncertainty.]');
      break;
    case 'lie':
      parts.push('[Give a plausible but false answer. Stay consistent with your earlier statements.]');
      break;
  }

  if (shouldReveal) {
    parts.push('[You feel compelled to share one thing you have been keeping to yourself — choose the least damaging hidden fact.]');
  }

  if (stressScore > 0.75) {
    parts.push('[Your stress is very high — your answer may be fragmented, rushed, or slip slightly.]');
  }

  return parts.join(' ');
}
