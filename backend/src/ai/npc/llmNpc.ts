// ============================================
// LLM NPC — public API for NPC interactions
// ============================================

import type { Witness, NPCDialogueState } from '../../types/witness.types';
import type { Suspect } from '../../murder_engine/models/Suspect';
import type { CaseVictim } from '../../types/case.types';
import { generateNPCResponse, type DialogueOutput } from './dialogueGenerator';
import { getRedisClient, REDIS_KEYS } from '../../configs/redis';
import { NPC, CACHE_TTL } from '../../utils/constants';
import logger from '../../utils/logger';

export interface StartSessionInput {
  matchId: string;
  playerId: string;
  witness: Witness;
  killer: Suspect;
  suspects: Suspect[];
  victim: CaseVictim;
}

export interface SendMessageInput {
  matchId: string;
  playerId: string;
  witnessId: string;
  message: string;
  witness: Witness;
  killer: Suspect;
  suspects: Suspect[];
  victim: CaseVictim;
}

// ============================================
// Session Management
// ============================================

export async function startNPCSession(input: StartSessionInput): Promise<NPCDialogueState> {
  const { matchId, playerId, witness } = input;

  const existingSession = await loadSession(witness.id, matchId, playerId);
  if (existingSession) {
    logger.debug('[LLM NPC] Resuming existing session', { witnessId: witness.id, playerId });
    return existingSession;
  }

  const session: NPCDialogueState = {
    witnessId: witness.id,
    matchId,
    playerId,
    sessionStart: new Date(),
    messageCount: 0,
    stressLevel: witness.currentStress,
    revealedFacts: [],
    contradictionCount: 0,
    trustScore: getInitialTrust(witness),
    conversationHistory: [],
  };

  await saveSession(session);
  logger.debug('[LLM NPC] New session started', { witnessId: witness.id, playerId });

  return session;
}

export async function sendMessage(input: SendMessageInput): Promise<DialogueOutput> {
  const { matchId, playerId, witnessId, message, witness, killer, suspects, victim } = input;

  // Load current session
  let session = await loadSession(witnessId, matchId, playerId);
  if (!session) {
    session = await startNPCSession({ matchId, playerId, witness, killer, suspects, victim });
  }

  // Enforce global interrogation limit
  if (witness.interrogationCount >= NPC.MAX_INTERROGATIONS_BEFORE_HOSTILE) {
    logger.warn('[LLM NPC] Witness at interrogation limit', { witnessId });
  }

  const output = await generateNPCResponse({
    playerMessage: message,
    witness,
    killer,
    suspects,
    victim,
    session,
  });

  // Persist updated session
  await saveSession(output.updatedSession);

  return output;
}

export async function getSession(
  witnessId: string,
  matchId: string,
  playerId: string
): Promise<NPCDialogueState | null> {
  return loadSession(witnessId, matchId, playerId);
}

export async function clearSession(
  witnessId: string,
  matchId: string,
  playerId: string
): Promise<void> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.npcSession(witnessId, matchId, playerId);
  await redis.del(key);
}

// ============================================
// Conversation Summary (for hint system)
// ============================================

export function summariseSession(session: NPCDialogueState): string {
  if (session.revealedFacts.length === 0) {
    return 'No significant information has been revealed in this conversation.';
  }

  return (
    `${session.revealedFacts.length} fact(s) revealed:\n` +
    session.revealedFacts.map((f, i) => `${i + 1}. ${f}`).join('\n')
  );
}

// ============================================
// Internal helpers
// ============================================

async function loadSession(
  witnessId: string,
  matchId: string,
  playerId: string
): Promise<NPCDialogueState | null> {
  try {
    const redis = getRedisClient();
    const key = REDIS_KEYS.npcSession(witnessId, matchId, playerId);
    const raw = await redis.get(key);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as NPCDialogueState;
    // Rehydrate Date
    parsed.sessionStart = new Date(parsed.sessionStart);
    parsed.conversationHistory = parsed.conversationHistory.map((t) => ({
      ...t,
      timestamp: new Date(t.timestamp),
    }));
    return parsed;
  } catch (err) {
    logger.error('[LLM NPC] Failed to load session from Redis', { err });
    return null;
  }
}

async function saveSession(session: NPCDialogueState): Promise<void> {
  try {
    const redis = getRedisClient();
    const key = REDIS_KEYS.npcSession(session.witnessId, session.matchId, session.playerId);
    await redis.setex(key, CACHE_TTL.MATCH_STATE, JSON.stringify(session));
  } catch (err) {
    logger.error('[LLM NPC] Failed to save session to Redis', { err });
  }
}

function getInitialTrust(witness: Witness): number {
  // Initial trust based on personality
  const trustMap: Record<string, number> = {
    cooperative:    0.6,
    nervous:        0.4,
    hostile:        0.2,
    deceptive:      0.4,
    confused:       0.5,
    protective:     0.35,
    opportunistic:  0.45,
  };
  return trustMap[witness.personality] ?? 0.4;
}
