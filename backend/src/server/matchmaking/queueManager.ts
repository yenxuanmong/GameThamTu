// ============================================
// Queue Manager — manages matchmaking queues per difficulty
// ============================================

import type { QueueEntry } from '../../types/match.types';
import type { DifficultyLevel } from '../../types/case.types';
import { getRedisClient, REDIS_KEYS } from '../../configs/redis';
import { QUEUE } from '../../utils/constants';
import logger from '../../utils/logger';

// ============================================
// Join / Leave Queue
// ============================================

export async function enqueue(entry: QueueEntry): Promise<void> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.matchmakingQueue(entry.difficulty);

  // Remove any existing entry for this player (re-queue)
  await dequeue(entry.playerId, entry.difficulty);

  const serialized = JSON.stringify({ ...entry, queuedAt: entry.queuedAt.toISOString() });
  await redis.zadd(key, Date.now(), serialized); // score = timestamp for FIFO ordering

  logger.debug('[QueueManager] Player enqueued', {
    playerId: entry.playerId,
    difficulty: entry.difficulty,
  });
}

export async function dequeue(
  playerId: string,
  difficulty: DifficultyLevel
): Promise<boolean> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.matchmakingQueue(difficulty);

  // Find and remove player's entry
  const members = await redis.zrange(key, 0, -1);
  for (const member of members) {
    try {
      const entry = JSON.parse(member) as QueueEntry;
      if (entry.playerId === playerId) {
        await redis.zrem(key, member);
        logger.debug('[QueueManager] Player dequeued', { playerId, difficulty });
        return true;
      }
    } catch {
      // skip malformed entry
    }
  }
  return false;
}

export async function dequeueAllDifficulties(playerId: string): Promise<void> {
  const difficulties: DifficultyLevel[] = ['easy', 'medium', 'hard', 'expert', 'nightmare'];
  await Promise.all(difficulties.map((d) => dequeue(playerId, d)));
}

// ============================================
// Get queue contents
// ============================================

export async function getQueueEntries(difficulty: DifficultyLevel): Promise<QueueEntry[]> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.matchmakingQueue(difficulty);
  const members = await redis.zrange(key, 0, -1);

  const entries: QueueEntry[] = [];
  for (const member of members) {
    try {
      const raw = JSON.parse(member) as QueueEntry & { queuedAt: string };
      entries.push({ ...raw, queuedAt: new Date(raw.queuedAt) });
    } catch {
      // skip malformed
    }
  }
  return entries;
}

export async function getQueueSize(difficulty: DifficultyLevel): Promise<number> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.matchmakingQueue(difficulty);
  return redis.zcard(key);
}

// ============================================
// Find compatible players
// ============================================

export interface MatchGroup {
  players: QueueEntry[];
  difficulty: DifficultyLevel;
}

export async function findMatchGroup(
  difficulty: DifficultyLevel,
  requiredCount = 4
): Promise<MatchGroup | null> {
  const entries = await getQueueEntries(difficulty);

  if (entries.length < requiredCount) {
    // Check if we can start with fewer players
    if (entries.length >= 2) {
      // Check timeout — if oldest player has been waiting > TIMEOUT, start with whoever is available
      const oldest = entries[0];
      if (oldest) {
        const waitSeconds = (Date.now() - new Date(oldest.queuedAt).getTime()) / 1000;
        if (waitSeconds >= QUEUE.TIMEOUT_SECONDS && entries.length >= 2) {
          return { players: entries, difficulty };
        }
      }
    }
    return null;
  }

  // Rank-based matching: sort by rank points, group by proximity
  const sorted = [...entries].sort((a, b) => a.rankPoints - b.rankPoints);
  const group = findCompatibleGroup(sorted, requiredCount);

  return group ? { players: group, difficulty } : null;
}

function findCompatibleGroup(
  sorted: QueueEntry[],
  count: number
): QueueEntry[] | null {
  for (let i = 0; i <= sorted.length - count; i++) {
    const slice = sorted.slice(i, i + count);
    const minRP = slice[0]?.rankPoints ?? 0;
    const maxRP = slice[slice.length - 1]?.rankPoints ?? 0;

    if (maxRP - minRP <= QUEUE.MAX_RANK_DIFF_FOR_MATCH) {
      return slice;
    }
  }

  // No compatible group found — if range exceeded, just take first N players
  if (sorted.length >= count) {
    return sorted.slice(0, count);
  }

  return null;
}

// ============================================
// Estimate wait time
// ============================================

export async function estimateWaitTime(
  difficulty: DifficultyLevel,
  rankPoints: number
): Promise<number> {
  const queueSize = await getQueueSize(difficulty);

  if (queueSize === 0) return QUEUE.TIMEOUT_SECONDS;
  if (queueSize >= 3) return 15; // nearly enough for a match

  // Simple heuristic based on queue size
  const baseWait = QUEUE.TIMEOUT_SECONDS;
  const queueBonus = Math.max(0, 3 - queueSize) * 15;

  void rankPoints; // future: factor in rank for tighter estimate
  return baseWait - queueBonus;
}
