// ============================================
// Redis Config
// ============================================

import Redis from 'ioredis';

let redisClient: Redis | null = null;

export function getRedisClient(): Redis {
  if (!redisClient) {
    redisClient = new Redis(process.env['REDIS_URL'] ?? 'redis://localhost:6379', {
      password: process.env['REDIS_PASSWORD'] || undefined,
      maxRetriesPerRequest: 3,
      lazyConnect: true,
      enableReadyCheck: true,
    });

    redisClient.on('connect', () => {
      console.log('✅ Redis connected');
    });

    redisClient.on('error', (err) => {
      console.error('❌ Redis error:', err);
    });

    redisClient.on('close', () => {
      console.warn('⚠️  Redis connection closed');
    });
  }

  return redisClient;
}

export async function connectRedis(): Promise<void> {
  const client = getRedisClient();
  await client.connect();
}

export async function disconnectRedis(): Promise<void> {
  if (redisClient) {
    await redisClient.quit();
    redisClient = null;
    console.log('🔌 Redis disconnected');
  }
}

// ---- Helpers ----

export async function redisSet(
  key: string,
  value: unknown,
  ttlSeconds?: number
): Promise<void> {
  const client = getRedisClient();
  const serialized = JSON.stringify(value);
  if (ttlSeconds) {
    await client.setex(key, ttlSeconds, serialized);
  } else {
    await client.set(key, serialized);
  }
}

export async function redisGet<T>(key: string): Promise<T | null> {
  const client = getRedisClient();
  const raw = await client.get(key);
  if (!raw) return null;
  return JSON.parse(raw) as T;
}

export async function redisDel(key: string): Promise<void> {
  const client = getRedisClient();
  await client.del(key);
}

export async function redisExists(key: string): Promise<boolean> {
  const client = getRedisClient();
  const result = await client.exists(key);
  return result === 1;
}

// Redis key namespaces
export const REDIS_KEYS = {
  matchState: (matchId: string) => `match:${matchId}:state`,
  roomState: (roomId: string) => `room:${roomId}:state`,
  playerSession: (playerId: string) => `player:${playerId}:session`,
  matchmakingQueue: (difficulty: string) => `queue:${difficulty}`,
  npcSession: (witnessId: string, matchId: string, playerId: string) =>
    `npc:${witnessId}:${matchId}:${playerId}`,
  caseCache: (caseId: string) => `case:${caseId}`,
  leaderboard: (season: number) => `leaderboard:season:${season}`,
  rateLimit: (ip: string) => `ratelimit:${ip}`,
} as const;
