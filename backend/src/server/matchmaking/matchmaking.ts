// ============================================
// Matchmaking Service — orchestrates queue → match creation
// ============================================

import type { DifficultyLevel } from '../../types/case.types';
import type { Match, MatchmakingResult, QueueEntry } from '../../types/match.types';
import { findMatchGroup, dequeue } from './queueManager';
import { createMatch } from '../../services/matchService';
import { createRoom, joinRoom } from '../rooms/roomManager';
import logger from '../../utils/logger';

// ============================================
// Run one matchmaking tick for a difficulty
// ============================================

export async function runMatchmakingTick(
  difficulty: DifficultyLevel
): Promise<MatchmakingResult> {
  const group = await findMatchGroup(difficulty);

  if (!group) {
    return { success: false };
  }

  logger.info('[Matchmaking] Group found', {
    difficulty,
    players: group.players.map((p) => p.playerId),
  });

  try {
    // Create a room for this match
    const hostPlayer = group.players[0]!;
    const roomName = `Auto-${difficulty}-${Date.now().toString(36)}`;

    const room = await createRoom(
      hostPlayer.playerId,
      roomName,
      { difficulty, maxPlayers: group.players.length, autoStart: true },
      'public'
    );

    // Join remaining players to room
    for (const player of group.players.slice(1)) {
      await joinRoom(room.id, player.playerId);
    }

    // Remove all players from queue
    await Promise.all(group.players.map((p) => dequeue(p.playerId, difficulty)));

    // Create match
    const match = await createMatch(room.id, group.players.map((p) => p.playerId), difficulty);

    logger.info('[Matchmaking] Match created', {
      matchId: match.id,
      roomId: room.id,
      players: group.players.map((p) => p.playerId),
    });

    return {
      success: true,
      matchId: match.id,
      roomId: room.id,
      players: group.players.map((p) => p.playerId),
    };
  } catch (err) {
    logger.error('[Matchmaking] Failed to create match', { err });
    return { success: false, error: 'Failed to create match' };
  }
}

// ============================================
// Matchmaking scheduler (called by a setInterval)
// ============================================

const TICK_INTERVAL_MS = 5_000; // every 5 seconds
const DIFFICULTIES: DifficultyLevel[] = ['easy', 'medium', 'hard', 'expert', 'nightmare'];

let tickInterval: ReturnType<typeof setInterval> | null = null;

export function startMatchmakingScheduler(
  onMatchFound: (result: MatchmakingResult) => void
): void {
  if (tickInterval) return;

  tickInterval = setInterval(() => {
    void (async () => {
      for (const difficulty of DIFFICULTIES) {
        const result = await runMatchmakingTick(difficulty);
        if (result.success) {
          onMatchFound(result);
        }
      }
    })();
  }, TICK_INTERVAL_MS);

  logger.info('[Matchmaking] Scheduler started');
}

export function stopMatchmakingScheduler(): void {
  if (tickInterval) {
    clearInterval(tickInterval);
    tickInterval = null;
    logger.info('[Matchmaking] Scheduler stopped');
  }
}

// ============================================
// Build queue entry from player data
// ============================================

export function buildQueueEntry(
  playerId: string,
  difficulty: DifficultyLevel,
  rankPoints: number,
  region?: string
): QueueEntry {
  return {
    playerId,
    difficulty,
    rankPoints,
    region,
    queuedAt: new Date(),
  };
}
