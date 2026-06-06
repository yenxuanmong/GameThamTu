// ============================================
// Hint Generator
// — generates contextual hints based on player progress
// ============================================

import type { FullCase } from '../../murder_engine/models/Case';
import type { InvestigationProgress } from '../../types/player.types';
import type { DifficultyLevel } from '../../types/case.types';
import { chatCompletion } from '../../configs/openai';
import logger from '../../utils/logger';

export type HintType = 'evidence' | 'suspect' | 'timeline' | 'motive' | 'general';

export interface Hint {
  type: HintType;
  content: string;
  relatedId?: string;   // evidenceId, suspectId, etc.
  cost: number;         // score penalty for using hint
}

const HINT_COST_BY_DIFFICULTY: Record<DifficultyLevel, number> = {
  easy:      10,
  medium:    25,
  hard:      40,
  expert:    60,
  nightmare: 80,
};

// ============================================
// Generate a contextual hint for a player
// ============================================

export async function generateHint(
  fullCase: FullCase,
  progress: InvestigationProgress,
  difficulty: DifficultyLevel
): Promise<Hint> {
  const cost = HINT_COST_BY_DIFFICULTY[difficulty];

  // Determine what the player is missing
  const hintType = determineHintType(fullCase, progress);

  // Try LLM-generated hint first, fall back to template
  try {
    const content = await generateLLMHint(fullCase, progress, hintType);
    return { type: hintType, content, cost };
  } catch (err) {
    logger.warn('[HintGenerator] LLM hint failed, using template', { err });
    const content = generateTemplateHint(fullCase, progress, hintType);
    return { type: hintType, content, cost };
  }
}

// ============================================
// Determine what kind of hint is most useful
// ============================================

function determineHintType(fullCase: FullCase, progress: InvestigationProgress): HintType {
  const totalEvidence = fullCase.evidencePool.length;
  const discoveredCount = progress.discoveredEvidenceIds.length;
  const discoveredRatio = discoveredCount / totalEvidence;

  const interrogatedCount = progress.interrogatedWitnessIds.length;
  const totalWitnesses = fullCase.witnesses.length;
  const interrogatedRatio = interrogatedCount / totalWitnesses;

  // If player has discovered very little evidence, nudge toward evidence
  if (discoveredRatio < 0.3) return 'evidence';

  // If player hasn't talked to many witnesses, nudge toward suspects
  if (interrogatedRatio < 0.4) return 'suspect';

  // If player is close to the end, nudge toward motive or timeline
  if (discoveredRatio > 0.6) {
    return Math.random() > 0.5 ? 'motive' : 'timeline';
  }

  return 'general';
}

// ============================================
// LLM-generated hint
// ============================================

async function generateLLMHint(
  fullCase: FullCase,
  progress: InvestigationProgress,
  hintType: HintType
): Promise<string> {
  const killer = fullCase.suspects.find((s) => s.isKiller)!;
  const undiscoveredEvidence = fullCase.evidencePool
    .filter((e) => !progress.discoveredEvidenceIds.includes(e.id) && e.isReal)
    .slice(0, 3);

  const systemPrompt = `You are a cryptic narrative hint-giver in a murder mystery game. 
Give ONE short, atmospheric hint (max 2 sentences) that nudges the player in the right direction 
WITHOUT directly naming the killer or outright stating the solution.
The hint should feel like a clue, not a spoiler.`;

  const contextMap: Record<HintType, string> = {
    evidence:
      `Hint type: evidence. Undiscovered key evidence includes: ${undiscoveredEvidence.map((e) => e.name).join(', ')}. ` +
      `Nudge the player toward searching more carefully.`,
    suspect:
      `Hint type: suspect. The killer's occupation is "${killer.occupation}" and they have a relationship with the victim as "${killer.relationships[0]?.type ?? 'unknown'}". ` +
      `Give a subtle character hint without naming them.`,
    motive:
      `Hint type: motive. The motive is "${fullCase.solution.motive.replace('_', ' ')}". ` +
      `Hint about what drove someone to act without naming who.`,
    timeline:
      `Hint type: timeline. The murder occurred at ${fullCase.victim.timeOfDeath.toLocaleTimeString()} in the ${fullCase.solution.location.replace('_', ' ')}. ` +
      `Hint about timing without being explicit.`,
    general:
      `Hint type: general. Case difficulty: ${fullCase.difficulty}. ` +
      `The player has examined ${progress.discoveredEvidenceIds.length} pieces of evidence. ` +
      `Give an atmospheric nudge to look more carefully.`,
  };

  const messages = [
    { role: 'system' as const, content: systemPrompt },
    { role: 'user' as const, content: contextMap[hintType] },
  ];

  return chatCompletion(messages, { maxTokens: 100, temperature: 0.9 });
}

// ============================================
// Template hints (fallback)
// ============================================

const TEMPLATE_HINTS: Record<HintType, string[]> = {
  evidence: [
    'Not all that glitters is evidence — but some things left in the shadows are more revealing than they appear.',
    'The scene holds more secrets. Look again at what was left behind.',
    'Physical traces rarely vanish entirely. Search more carefully.',
  ],
  suspect: [
    'Everyone in this house has something to hide. Some more than others.',
    'A relationship left unacknowledged is often the most significant one.',
    'Pay closer attention to who avoids certain questions — and why.',
  ],
  motive: [
    'Ask yourself who stood to gain the most from this death.',
    'Hatred and greed are old companions. Which one walked through that door?',
    'The motive was present long before the act itself.',
  ],
  timeline: [
    'Reconstruct the evening minute by minute. The gap, when you find it, will be telling.',
    'Timing is everything. When did the noise stop — and when did it resume?',
    'One person\'s account of the evening does not quite align with the rest. Find the inconsistency.',
  ],
  general: [
    'The truth is in the room. You simply haven\'t looked at the right corner yet.',
    'Trust less. Question more. The answers are closer than you think.',
    'Every liar makes at least one mistake. You just have to be paying attention.',
  ],
};

function generateTemplateHint(
  _fullCase: FullCase,
  _progress: InvestigationProgress,
  hintType: HintType
): string {
  const hints = TEMPLATE_HINTS[hintType];
  return hints[Math.floor(Math.random() * hints.length)] ?? hints[0]!;
}
