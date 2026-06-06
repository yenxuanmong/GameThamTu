// ============================================
// NPC Service — exposes NPC/witness interactions for HTTP + Socket
// ============================================

import { prisma } from '../configs/database';
import { sendMessage, startNPCSession, getSession, clearSession } from '../ai/npc/llmNpc';
import type { FullCase } from '../murder_engine/models/Case';
import logger from '../utils/logger';

export interface InterrogateInput {
  matchId: string;
  playerId: string;
  witnessId: string;
  message: string;
  fullCase: FullCase;
}

export interface InterrogateResult {
  success: boolean;
  npcMessage?: string;
  stressLevel?: string;
  trustScore?: number;
  messageCount?: number;
  memoryTriggered?: string;
  contradictionDetected?: string;
  error?: string;
}

// ============================================
// Interrogate a witness
// ============================================

export async function interrogateWitness(input: InterrogateInput): Promise<InterrogateResult> {
  const { matchId, playerId, witnessId, message, fullCase } = input;

  const witness = fullCase.witnesses.find((w) => w.id === witnessId);
  if (!witness) return { success: false, error: 'Witness not found in this case' };

  const killer = fullCase.suspects.find((s) => s.isKiller);
  if (!killer) return { success: false, error: 'Case data is corrupted' };

  try {
    await startNPCSession({
      matchId,
      playerId,
      witness,
      killer,
      suspects: fullCase.suspects,
      victim: fullCase.victim,
    });

    const output = await sendMessage({
      matchId,
      playerId,
      witnessId,
      message,
      witness,
      killer,
      suspects: fullCase.suspects,
      victim: fullCase.victim,
    });

    // Persist session state to DB
    await prisma.nPCDialogueSession.upsert({
      where: { witnessId_matchId_playerId: { witnessId, matchId, playerId } },
      update: {
        stressLevel: output.updatedSession.stressLevel,
        trustScore: output.updatedSession.trustScore,
        revealedFacts: output.updatedSession.revealedFacts,
        contradictionCount: output.updatedSession.contradictionCount,
        messageCount: output.updatedSession.messageCount,
        conversationHistory: output.updatedSession.conversationHistory as object[],
        updatedAt: new Date(),
      },
      create: {
        witnessId,
        matchId,
        playerId,
        stressLevel: output.updatedSession.stressLevel,
        trustScore: output.updatedSession.trustScore,
        revealedFacts: output.updatedSession.revealedFacts,
        contradictionCount: output.updatedSession.contradictionCount,
        messageCount: output.updatedSession.messageCount,
        conversationHistory: output.updatedSession.conversationHistory as object[],
      },
    });

    return {
      success: true,
      npcMessage: output.npcMessage,
      stressLevel: output.updatedSession.stressLevel,
      trustScore: output.updatedSession.trustScore,
      messageCount: output.updatedSession.messageCount,
      memoryTriggered: output.memoryTriggered,
      contradictionDetected: output.contradictionDetected,
    };
  } catch (err) {
    logger.error('[NPCService] Interrogation failed', { err, witnessId, playerId });
    return { success: false, error: 'Witness is not responding right now' };
  }
}

// ============================================
// Get conversation history
// ============================================

export async function getConversationHistory(
  matchId: string,
  playerId: string,
  witnessId: string
) {
  const session = await getSession(witnessId, matchId, playerId);
  if (!session) {
    // Fallback to DB
    const dbSession = await prisma.nPCDialogueSession.findUnique({
      where: { witnessId_matchId_playerId: { witnessId, matchId, playerId } },
      select: {
        conversationHistory: true,
        stressLevel: true,
        trustScore: true,
        messageCount: true,
        revealedFacts: true,
      },
    });
    return dbSession;
  }

  return {
    conversationHistory: session.conversationHistory,
    stressLevel: session.stressLevel,
    trustScore: session.trustScore,
    messageCount: session.messageCount,
    revealedFacts: session.revealedFacts,
  };
}

// ============================================
// Get all sessions for a player in a match
// ============================================

export async function getPlayerSessions(matchId: string, playerId: string) {
  const sessions = await prisma.nPCDialogueSession.findMany({
    where: { matchId, playerId },
    select: {
      witnessId: true,
      stressLevel: true,
      trustScore: true,
      messageCount: true,
      revealedFacts: true,
      updatedAt: true,
    },
  });

  return sessions;
}

// ============================================
// Reset/clear session (admin/debug)
// ============================================

export async function resetWitnessSession(
  matchId: string,
  playerId: string,
  witnessId: string
): Promise<void> {
  await clearSession(witnessId, matchId, playerId);
  await prisma.nPCDialogueSession.deleteMany({
    where: { witnessId, matchId, playerId },
  });
}
