// ============================================
// Relationship System
// — how witness loyalty affects what they reveal
// ============================================

import type { WitnessRelationship } from '../../types/witness.types';

export interface LoyaltyEffect {
  willProtect: boolean;
  protectionStrength: number;   // 0–1
  willBetray: boolean;
  reason: string;
}

// ============================================
// Determine if witness will protect a specific person
// ============================================

export function getLoyaltyEffect(
  relationship: WitnessRelationship,
  trustScore: number,
  stressScore: number
): LoyaltyEffect {
  const { loyaltyScore } = relationship;

  // Will protect if loyalty > 0.5 and trust from investigator is low
  const willProtect = loyaltyScore > 0.5 && trustScore < 0.7;

  // Strength of protection
  const protectionStrength = willProtect
    ? loyaltyScore * (1 - trustScore * 0.5)
    : 0;

  // Under extreme stress, even loyal witnesses may crack
  const willBetray =
    loyaltyScore < 0.4 ||
    (stressScore > 0.85 && loyaltyScore < 0.75);

  const reason = willProtect
    ? `High loyalty (${loyaltyScore.toFixed(2)}) to ${relationship.targetId}`
    : willBetray
      ? `Low loyalty or extreme stress forces disclosure`
      : 'Neutral stance';

  return { willProtect, protectionStrength, willBetray, reason };
}

// ============================================
// Build relationship context hint for system prompt
// ============================================

export function buildRelationshipHint(
  relationships: WitnessRelationship[],
  mentionedId: string,
  trustScore: number,
  stressScore: number
): string | null {
  const rel = relationships.find((r) => r.targetId === mentionedId);
  if (!rel) return null;

  const effect = getLoyaltyEffect(rel, trustScore, stressScore);

  if (effect.willProtect) {
    return `[The investigator is asking about someone you are loyal to. Minimise, deflect, or omit damaging details about them.]`;
  }

  if (effect.willBetray) {
    return `[You have little loyalty to this person. You may be more forthcoming about what you know regarding them.]`;
  }

  return null;
}

// ============================================
// Find if a keyword in the message refers to a known person
// ============================================

export function findMentionedRelationship(
  message: string,
  relationships: WitnessRelationship[],
  nameMap: Map<string, string>  // id → name
): WitnessRelationship | null {
  const msgLower = message.toLowerCase();

  for (const rel of relationships) {
    const name = nameMap.get(rel.targetId);
    if (!name) continue;

    const firstName = name.split(' ')[0]?.toLowerCase() ?? '';
    const lastName = name.split(' ')[1]?.toLowerCase() ?? '';

    if (msgLower.includes(firstName) || msgLower.includes(lastName)) {
      return rel;
    }
  }

  return null;
}
