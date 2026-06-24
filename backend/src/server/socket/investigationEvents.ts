// ============================================
// Investigation Socket Events
// — evidence examination, NPC interrogation, notebook
// ============================================

import type { Server as SocketIOServer } from 'socket.io';
import type { AuthenticatedSocket } from './socketServer';
import type { ServerToClientEvents } from '../../types/match.types';
import { getMatchState, getCaseFomMatch } from '../../services/matchService';
import { sendMessage, startNPCSession } from '../../ai/npc/llmNpc';
import { prisma } from '../../configs/database';
import { stressToScore } from '../../ai/witness/stressSystem';
import logger from '../../utils/logger';

export function registerInvestigationEvents(
  io: SocketIOServer<any, ServerToClientEvents>,
  socket: AuthenticatedSocket
): void {
  const { playerId } = socket;

  // ============================================
  // Examine evidence
  // ============================================
  socket.on('investigation:examine_evidence', (data) => {
    void (async () => {
      try {
        const { matchId, evidenceId } = data;

        const fullCase = await getCaseFomMatch(matchId);
        if (!fullCase) {
          socket.emit('error', { code: 'CASE_NOT_FOUND', message: 'Case not available' });
          return;
        }

        // Verify evidence exists in this case
        const evidence = fullCase.evidencePool.find((e) => e.id === evidenceId);
        if (!evidence) {
          socket.emit('error', { code: 'EVIDENCE_NOT_FOUND', message: 'Evidence not found' });
          return;
        }

        // Record discovery
        await prisma.evidenceDiscovery.upsert({
          where: { evidenceId_playerId_matchId: { evidenceId, playerId, matchId } },
          update: {},
          create: {
            evidenceId,
            playerId,
            matchId,
            sharedWith: [],
            notes: '',
          },
        });

        // Notify the discovering player
        socket.emit('investigation:evidence_found', { evidenceId, playerId });

        // Broadcast to room that someone found evidence (without revealing which)
        socket.to(`room:${await getRoomId(matchId)}`).emit('investigation:evidence_found', {
          evidenceId: '[hidden]',
          playerId,
        });

        logger.debug('[InvestigationEvents] Evidence examined', { playerId, evidenceId, matchId });
      } catch (err) {
        logger.error('[InvestigationEvents] Examine evidence failed', { err, playerId });
        socket.emit('error', { code: 'EXAMINE_FAILED', message: 'Failed to examine evidence' });
      }
    })();
  });

  // ============================================
  // Interrogate witness (NPC dialogue)
  // ============================================
  socket.on('investigation:interrogate_witness', (data) => {
    void (async () => {
      try {
        const { matchId, witnessId, message } = data;

        if (!message.trim()) {
          socket.emit('error', { code: 'EMPTY_MESSAGE', message: 'Message cannot be empty' });
          return;
        }

        if (message.length > 500) {
          socket.emit('error', { code: 'MESSAGE_TOO_LONG', message: 'Message too long (max 500 chars)' });
          return;
        }

        const fullCase = await getCaseFomMatch(matchId);
        if (!fullCase) {
          socket.emit('error', { code: 'CASE_NOT_FOUND', message: 'Case not available' });
          return;
        }

        const witness = fullCase.witnesses.find((w) => w.id === witnessId);
        if (!witness) {
          socket.emit('error', { code: 'WITNESS_NOT_FOUND', message: 'Witness not found' });
          return;
        }

        const killer = fullCase.suspects.find((s) => s.isKiller);
        if (!killer) {
          socket.emit('error', { code: 'CASE_INVALID', message: 'Case data is corrupted' });
          return;
        }

        // Ensure session exists
        await startNPCSession({
          matchId,
          playerId,
          witness,
          killer,
          suspects: fullCase.suspects,
          victim: fullCase.victim,
        });

        // Generate NPC response
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

        // Emit NPC response to this player
        socket.emit('npc:response', {
          witnessId,
          message: output.npcMessage,
          stressLevel: output.updatedSession.stressLevel,
        });

        // Track interrogation in DB
        await prisma.matchPlayer.update({
          where: { matchId_playerId: { matchId, playerId } },
          data: { timeSpent: { increment: 5 } }, // approximate time per message
        }).catch(() => void 0);

        // Update NPC dialogue session in DB
        await prisma.nPCDialogueSession.upsert({
          where: {
            witnessId_matchId_playerId: { witnessId, matchId, playerId },
          },
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

        logger.debug('[InvestigationEvents] NPC response generated', {
          playerId,
          witnessId,
          matchId,
          stress: output.updatedSession.stressLevel,
        });
      } catch (err) {
        logger.error('[InvestigationEvents] Interrogation failed', { err, playerId });
        socket.emit('error', { code: 'NPC_FAILED', message: 'Witness is not responding right now' });
      }
    })();
  });

  // ============================================
  // Add notebook entry
  // ============================================
  socket.on('investigation:add_note', (data) => {
    void (async () => {
      try {
        const { matchId, note, relatedId } = data;

        if (!note.trim()) return;

        const matchPlayer = await prisma.matchPlayer.findUnique({
          where: { matchId_playerId: { matchId, playerId } },
          select: { id: true },
        });

        if (!matchPlayer) {
          socket.emit('error', { code: 'NOT_IN_MATCH', message: 'You are not in this match' });
          return;
        }

        await prisma.notebookEntry.create({
          data: {
            matchPlayerId: matchPlayer.id,
            playerId,
            content: note,
            relatedEvidenceId: relatedId ?? null,
          },
        });

        logger.debug('[InvestigationEvents] Note added', { playerId, matchId });
      } catch (err) {
        logger.error('[InvestigationEvents] Add note failed', { err, playerId });
        socket.emit('error', { code: 'NOTE_FAILED', message: 'Failed to save note' });
      }
    })();
  });
}

// ============================================
// Helper
// ============================================

async function getRoomId(matchId: string): Promise<string> {
  const match = await getMatchState(matchId);
  return match?.roomId ?? '';
}

// Suppress unused import
void stressToScore;
