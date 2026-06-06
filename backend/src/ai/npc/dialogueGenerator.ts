// ============================================
// Dialogue Generator
// — orchestrates one NPC dialogue turn
// ============================================

import type { Witness, NPCDialogueState, DialogueTurn } from '../../types/witness.types';
import type { Suspect } from '../../murder_engine/models/Suspect';
import type { CaseVictim } from '../../types/case.types';
import { chatCompletion } from '../../configs/openai';
import { buildSystemPrompt, buildContextualPrompt } from './promptBuilder';
import { decideHonestyDisposition } from '../witness/honestySystem';
import { calculateStressChange, stressToScore, getStressEffects } from '../witness/stressSystem';
import { tryTriggerMemory, detectContradiction } from '../witness/witnessMemory';
import { findMentionedRelationship, buildRelationshipHint } from '../witness/relationshipSystem';
import { SeededRandom } from '../../utils/random';
import { NPC } from '../../utils/constants';
import logger from '../../utils/logger';

export interface DialogueInput {
  playerMessage: string;
  witness: Witness;
  killer: Suspect;
  suspects: Suspect[];
  victim: CaseVictim;
  session: NPCDialogueState;
}

export interface DialogueOutput {
  npcMessage: string;
  updatedSession: NPCDialogueState;
  memoryTriggered?: string;       // fact that was revealed
  contradictionDetected?: string;
  stressChange: number;           // delta
  trustChange: number;            // delta
}

// ============================================
// Generate one NPC response
// ============================================

export async function generateNPCResponse(input: DialogueInput): Promise<DialogueOutput> {
  const { playerMessage, witness, killer, suspects, victim, session } = input;

  // Gate: max message limit
  if (session.messageCount >= NPC.MAX_MESSAGES_PER_SESSION) {
    return buildRefusalResponse(session, 'I have nothing more to say.');
  }

  const rng = new SeededRandom(`${witness.id}-${session.messageCount}-${Date.now()}`);

  // ---- 1. Stress update ----
  const currentStressScore = stressToScore(session.stressLevel as any);
  const stressUpdate = calculateStressChange(
    playerMessage,
    currentStressScore,
    witness.personality,
    witness.isKiller
  );

  // ---- 2. Honesty disposition ----
  const isAccusation = isAccusationMessage(playerMessage);
  const honestyDecision = decideHonestyDisposition(
    witness.honestyLevel,
    stressUpdate.newLevel,
    session.trustScore,
    witness.isKiller,
    isAccusation,
    rng
  );

  // ---- 3. Memory trigger ----
  const memoryResult = tryTriggerMemory(
    witness,
    playerMessage,
    stressUpdate.newLevel,
    session.trustScore,
    session.revealedFacts,
    rng
  );

  // ---- 4. Relationship loyalty ----
  const nameMap = new Map<string, string>(suspects.map((s) => [s.id, s.name]));
  nameMap.set(victim.id, victim.name);

  const mentionedRel = findMentionedRelationship(playerMessage, witness.relationships, nameMap);
  const relationshipHint = mentionedRel
    ? buildRelationshipHint(
        witness.relationships,
        mentionedRel.targetId,
        session.trustScore,
        currentStressScore
      )
    : null;

  // ---- 5. Stress effects ----
  const stressEffects = getStressEffects(stressToScore(stressUpdate.newLevel), witness.personality);

  if (stressEffects.refusesToAnswer) {
    return buildRefusalResponse(
      session,
      'I — I can\'t. I need to stop. Please, not now.'
    );
  }

  // ---- 6. Build prompts ----
  const systemPrompt = buildSystemPrompt({
    witness,
    killer,
    suspects,
    victim,
    currentStress: stressToScore(stressUpdate.newLevel),
    trustScore: session.trustScore,
    revealedFacts: session.revealedFacts,
    contradictionCount: session.contradictionCount,
    messageCount: session.messageCount,
  });

  // Add hints from honesty / relationship systems
  const hints: string[] = [honestyDecision.systemHint];
  if (relationshipHint) hints.push(relationshipHint);
  if (memoryResult.revealed && memoryResult.memory) {
    hints.push(
      `[You may naturally weave in the following detail if it feels appropriate: "${memoryResult.memory.description}"]`
    );
  }
  if (stressEffects.willContradictSelf) {
    hints.push('[Subtly contradict something minor you said earlier without drawing attention to it.]');
  }

  const augmentedSystemPrompt = systemPrompt + '\n\n## Current Response Guidance\n' + hints.join('\n');

  // Build message history for context
  const contextualHistory = buildContextualPrompt(
    {
      witness,
      killer,
      suspects,
      victim,
      currentStress: stressToScore(stressUpdate.newLevel),
      trustScore: session.trustScore,
      revealedFacts: session.revealedFacts,
      contradictionCount: session.contradictionCount,
      messageCount: session.messageCount,
    },
    session.conversationHistory
  );

  // ---- 7. Call LLM ----
  let npcMessage: string;
  try {
    const messages: { role: 'system' | 'user' | 'assistant'; content: string }[] = [
      { role: 'system', content: augmentedSystemPrompt },
    ];

    if (contextualHistory.trim().length > 0) {
      messages.push({ role: 'user', content: contextualHistory });
    }

    messages.push({ role: 'user', content: `Investigator: ${playerMessage}` });

    npcMessage = await chatCompletion(messages, {
      maxTokens: 200,
      temperature: 0.85,
    });
  } catch (err) {
    logger.error('[DialogueGenerator] LLM call failed', { err });
    npcMessage = generateFallbackResponse(witness.personality);
  }

  // ---- 8. Contradiction check ----
  const previousStatements = session.conversationHistory
    .filter((t) => t.role === 'npc')
    .map((t) => t.content);

  const contradictionCheck = detectContradiction(npcMessage, previousStatements);
  const contradictionCount = session.contradictionCount + (contradictionCheck.hasContradiction ? 1 : 0);

  // ---- 9. Update session ----
  const newTurn: DialogueTurn = {
    role: 'npc',
    content: npcMessage,
    timestamp: new Date(),
    revealedMemoryId: memoryResult.memory?.eventId,
    stressChange: stressUpdate.delta,
  };

  const playerTurn: DialogueTurn = {
    role: 'player',
    content: playerMessage,
    timestamp: new Date(),
  };

  const newRevealedFacts = memoryResult.revealed && memoryResult.memory
    ? [...session.revealedFacts, memoryResult.memory.description]
    : session.revealedFacts;

  const trustChange = honestyDecision.trustBonus + (isAccusation ? -NPC.TRUST_DECREASE_PER_HOSTILE : 0);
  const newTrust = Math.max(0, Math.min(1, session.trustScore + trustChange));

  const updatedSession: NPCDialogueState = {
    ...session,
    stressLevel: stressUpdate.newLevel,
    trustScore: newTrust,
    revealedFacts: newRevealedFacts,
    contradictionCount,
    messageCount: session.messageCount + 1,
    conversationHistory: [
      ...session.conversationHistory,
      playerTurn,
      newTurn,
    ],
  };

  return {
    npcMessage,
    updatedSession,
    memoryTriggered: memoryResult.memory?.description,
    contradictionDetected: contradictionCheck.hasContradiction
      ? contradictionCheck.details
      : undefined,
    stressChange: stressUpdate.delta,
    trustChange,
  };
}

// ============================================
// Helpers
// ============================================

const ACCUSATION_KEYWORDS_CHECK = [
  'you did it', 'you killed', 'you\'re the killer', 'you murdered',
  'you\'re lying', 'admit it', 'we know it was you',
];

function isAccusationMessage(message: string): boolean {
  const lower = message.toLowerCase();
  return ACCUSATION_KEYWORDS_CHECK.some((k) => lower.includes(k));
}

function buildRefusalResponse(
  session: NPCDialogueState,
  message: string
): DialogueOutput {
  const refusalTurn: DialogueTurn = {
    role: 'npc',
    content: message,
    timestamp: new Date(),
  };

  return {
    npcMessage: message,
    updatedSession: {
      ...session,
      messageCount: session.messageCount + 1,
      conversationHistory: [...session.conversationHistory, refusalTurn],
    },
    stressChange: 0,
    trustChange: 0,
  };
}

const FALLBACK_RESPONSES: Record<string, string[]> = {
  cooperative: [
    'I\'m trying to remember. Give me a moment.',
    'I want to help — I just need to collect my thoughts.',
  ],
  nervous: [
    'I — sorry, what did you ask? I keep losing my train of thought.',
    'Please, I\'m doing my best to remember.',
  ],
  hostile: [
    'I\'ve told you everything I know.',
    'I don\'t see why I have to keep answering these questions.',
  ],
  deceptive: [
    'I\'m not sure I understand what you\'re implying.',
    'I\'ve been perfectly clear, haven\'t I?',
  ],
  confused: [
    'It was all rather a blur, to be honest.',
    'I keep going over it in my head but I can\'t seem to pin it down.',
  ],
  protective: [
    'I really don\'t think that\'s relevant.',
    'I\'d rather not speak for others — that\'s not my place.',
  ],
  opportunistic: [
    'Well, there is something I could tell you, if you think it would help.',
    'I\'ve been wondering whether I should mention this...',
  ],
};

function generateFallbackResponse(personality: string): string {
  const responses = FALLBACK_RESPONSES[personality] ?? FALLBACK_RESPONSES['cooperative']!;
  return responses[Math.floor(Math.random() * responses.length)] ?? 'I don\'t know.';
}
