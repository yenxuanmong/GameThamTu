// ============================================
// Witness Memory System
// — tracks what each NPC knows and has revealed
// ============================================

import type { Witness, WitnessMemory, StressLevel } from '../../types/witness.types';
import type { SeededRandom } from '../../utils/random';

export interface MemoryRevealResult {
  revealed: boolean;
  memory: WitnessMemory | null;
  reason: string;
}

const STRESS_ORDER: StressLevel[] = ['calm', 'mild', 'moderate', 'high', 'extreme'];

function stressToNumber(s: StressLevel): number {
  return STRESS_ORDER.indexOf(s);
}

// ============================================
// Check if a memory can be revealed right now
// ============================================

export function canRevealMemory(
  memory: WitnessMemory,
  currentStress: StressLevel,
  trustScore: number,
  revealedFacts: string[]
): { canReveal: boolean; reason: string } {
  // Already revealed
  if (revealedFacts.includes(memory.description)) {
    return { canReveal: false, reason: 'already_revealed' };
  }

  // Not willing to share regardless
  if (!memory.isWillingToShare && trustScore < 0.7) {
    return { canReveal: false, reason: 'not_willing' };
  }

  // Stress is too high
  const stressNum = stressToNumber(currentStress);
  const thresholdNum = stressToNumber(memory.requiresStressBelow);
  if (stressNum > thresholdNum) {
    return { canReveal: false, reason: 'stress_too_high' };
  }

  return { canReveal: true, reason: 'ok' };
}

// ============================================
// Try to trigger a memory based on player message
// ============================================

export function tryTriggerMemory(
  witness: Witness,
  playerMessage: string,
  currentStress: StressLevel,
  trustScore: number,
  revealedFacts: string[],
  rng: SeededRandom
): MemoryRevealResult {
  const messageLower = playerMessage.toLowerCase();

  for (const memory of witness.memories) {
    // Check if this message satisfies a reveal condition
    if (memory.revealCondition) {
      const conditionLower = memory.revealCondition.toLowerCase();
      // Simple keyword check — extract key term from condition
      const keyTerm = conditionLower.replace('player mentions ', '').replace('player asks about ', '');
      if (!messageLower.includes(keyTerm)) continue;
    }

    const { canReveal, reason } = canRevealMemory(memory, currentStress, trustScore, revealedFacts);

    if (!canReveal) continue;

    // Accuracy-based probabilistic reveal
    if (!rng.nextBool(memory.accuracy)) continue;

    return {
      revealed: true,
      memory,
      reason: 'triggered_by_keyword',
    };
  }

  return { revealed: false, memory: null, reason: 'no_match' };
}

// ============================================
// Get all facts the NPC is currently willing to share
// ============================================

export function getShareableFacts(
  witness: Witness,
  currentStress: StressLevel,
  trustScore: number,
  revealedFacts: string[]
): string[] {
  return witness.knownFacts.filter((fact) => {
    // Check via memories if there's a constraint
    const memory = witness.memories.find((m) => m.description === fact);
    if (!memory) return true; // no constraint, always shareable

    const { canReveal } = canRevealMemory(memory, currentStress, trustScore, revealedFacts);
    return canReveal;
  });
}

// ============================================
// Detect contradictions in NPC responses
// ============================================

export interface ContradictionCheckResult {
  hasContradiction: boolean;
  details?: string;
}

export function detectContradiction(
  newStatement: string,
  previousStatements: string[]
): ContradictionCheckResult {
  const newLower = newStatement.toLowerCase();

  // Simple contradiction patterns
  const contradictionPairs = [
    ['was there', 'was not there'],
    ['did not see', 'saw'],
    ['was home', 'was not home'],
    ['knew them', 'did not know them'],
    ['heard nothing', 'heard something'],
    ['alone', 'with someone'],
    ['before dinner', 'after dinner'],
    ['never met', 'had met'],
  ];

  for (const prev of previousStatements) {
    const prevLower = prev.toLowerCase();
    for (const [a, b] of contradictionPairs) {
      if (!a || !b) continue;
      if (
        (newLower.includes(a) && prevLower.includes(b)) ||
        (newLower.includes(b) && prevLower.includes(a))
      ) {
        return {
          hasContradiction: true,
          details: `Statement contradicts earlier claim about "${a}/${b}"`,
        };
      }
    }
  }

  return { hasContradiction: false };
}
