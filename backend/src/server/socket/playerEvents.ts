// ============================================
// Player Socket Events — queue, match submission
// ============================================

import type { Server as SocketIOServer } from 'socket.io';
import type { AuthenticatedSocket } from './socketServer';
import type { ServerToClientEvents } from '../../types/match.types';
import { enqueue, dequeueAllDifficulties, estimateWaitTime } from '../matchmaking/queueManager';
import { buildQueueEntry } from '../matchmaking/matchmaking';
import {
  getMatchState,
  submitConclusion,
  finaliseMatch,
  getCaseFomMatch,
} from '../../services/matchService';
import { requestHint } from '../../ai/hints/hintAI';
import { prisma } from '../../configs/database';
import logger from '../../utils/logger';
import type { PlayerConclusion } from '../../types/case.types';

export function registerPlayerEvents(
  io: SocketIOServer<any, ServerToClientEvents>,
  socket: AuthenticatedSocket
): void {
  const { playerId } = socket;

  // ============================================
  // Join matchmaking queue
  // ============================================
  socket.on('queue:join', (data) => {
    void (async () => {
      try {
        // Get player rank
        const rank = await prisma.playerRank.findUnique({
          where: { playerId },
          select: { points: true },
        });

        const rankPoints = rank?.points ?? 0;
        const waitTime = await estimateWaitTime(data.difficulty, rankPoints);

        const entry = buildQueueEntry(playerId, data.difficulty, rankPoints, data.region);
        await enqueue(entry);

        socket.emit('notification', {
          type: 'info',
          message: `Joined ${data.difficulty} queue. Estimated wait: ${waitTime}s`,
        });

        logger.debug('[PlayerEvents] Player joined queue', { playerId, difficulty: data.difficulty });
      } catch (err) {
        logger.error('[PlayerEvents] Queue join failed', { err, playerId });
        socket.emit('error', { code: 'QUEUE_JOIN_FAILED', message: 'Failed to join queue' });
      }
    })();
  });

  // ============================================
  // Leave matchmaking queue
  // ============================================
  socket.on('queue:leave', () => {
    void (async () => {
      try {
        await dequeueAllDifficulties(playerId);
        socket.emit('notification', { type: 'info', message: 'Left matchmaking queue' });
      } catch (err) {
        logger.error('[PlayerEvents] Queue leave failed', { err, playerId });
      }
    })();
  });

  // ============================================
  // Submit conclusion
  // ============================================
  socket.on('match:submit_conclusion', (data) => {
    void (async () => {
      try {
        const conclusion: PlayerConclusion = {
          ...data.conclusion,
          submittedAt: new Date(),
        };

        const result = await submitConclusion(data.matchId, conclusion);

        if (!result.success) {
          socket.emit('error', { code: 'SUBMIT_FAILED', message: result.error ?? 'Submission failed' });
          return;
        }

        // Notify all players that this player submitted
        const match = await getMatchState(data.matchId);
        if (!match) return;

        io.to(`room:${match.roomId}`).emit('match:player_submitted', {
          playerId,
          submittedAt: conclusion.submittedAt,
        });

        // If all players submitted → finalise match
        if (match.status === 'conclusion') {
          const finalisedMatch = await finaliseMatch(data.matchId);
          if (finalisedMatch) {
            io.to(`room:${match.roomId}`).emit('match:ended', {
              scores: finalisedMatch.scores,
              winnerId: finalisedMatch.winnerId ?? '',
            });
          }
        }

        logger.debug('[PlayerEvents] Conclusion submitted', { playerId, matchId: data.matchId });
      } catch (err) {
        logger.error('[PlayerEvents] Submit conclusion failed', { err, playerId });
        socket.emit('error', { code: 'SUBMIT_FAILED', message: 'Failed to submit conclusion' });
      }
    })();
  });

  // ============================================
  // Request hint
  // ============================================
  socket.on('match:request_hint', (data) => {
    void (async () => {
      try {
        const match = await getMatchState(data.matchId);
        if (!match) {
          socket.emit('error', { code: 'MATCH_NOT_FOUND', message: 'Match not found' });
          return;
        }

        const fullCase = await getCaseFomMatch(data.matchId);
        if (!fullCase) {
          socket.emit('error', { code: 'CASE_NOT_FOUND', message: 'Case data not available' });
          return;
        }

        // Get player progress from DB
        const matchPlayer = await prisma.matchPlayer.findUnique({
          where: { matchId_playerId: { matchId: data.matchId, playerId } },
          include: { notebookEntries: true },
        });

        const progress = {
          playerId,
          matchId: data.matchId,
          discoveredEvidenceIds: [],
          interrogatedWitnessIds: [],
          notebookEntries: (matchPlayer?.notebookEntries ?? []).map((e) => ({
            ...e,
            relatedEvidenceId: e.relatedEvidenceId ?? undefined,
            relatedSuspectId: e.relatedSuspectId ?? undefined,
            relatedWitnessId: e.relatedWitnessId ?? undefined,
          })),
          suspectList: [],
          timeSpent: matchPlayer?.timeSpent ?? 0,
          hintsUsed: matchPlayer?.hintsUsed ?? 0,
          lastUpdated: new Date(),
        };

        const result = await requestHint(
          playerId,
          data.matchId,
          fullCase,
          progress,
          match.difficulty
        );

        if (!result.success) {
          socket.emit('error', {
            code: 'HINT_UNAVAILABLE',
            message: result.error ?? 'Hint unavailable',
          });
          return;
        }

        const hintsRemaining = 3 - ((matchPlayer?.hintsUsed ?? 0) + 1);
        socket.emit('investigation:hint', {
          hint: result.hint?.content ?? '',
          hintsRemaining: Math.max(0, hintsRemaining),
        });

        logger.debug('[PlayerEvents] Hint provided', { playerId, matchId: data.matchId });
      } catch (err) {
        logger.error('[PlayerEvents] Hint request failed', { err, playerId });
        socket.emit('error', { code: 'HINT_FAILED', message: 'Failed to generate hint' });
      }
    })();
  });

  // ============================================
  // Disconnect: leave queue
  // ============================================
  socket.on('disconnect', () => {
    void dequeueAllDifficulties(playerId).catch(() => void 0);
  });
}
