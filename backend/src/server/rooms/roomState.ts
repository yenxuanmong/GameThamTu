// ============================================
// Room State — in-memory + Redis room state management
// ============================================

import type { Room, RoomSettings, RoomVisibility } from '../../types/match.types';
import type { DifficultyLevel } from '../../types/case.types';
import { getRedisClient, REDIS_KEYS } from '../../configs/redis';
import { CACHE_TTL, MATCH } from '../../utils/constants';
import { v4 as uuidv4 } from 'uuid';
import logger from '../../utils/logger';

// ============================================
// Default room settings
// ============================================

export function defaultRoomSettings(
  difficulty: DifficultyLevel = 'medium'
): RoomSettings {
  return {
    difficulty,
    maxPlayers: MATCH.MAX_PLAYERS,
    allowSpectators: false,
    enableVoiceChat: true,
    autoStart: false,
    startCountdownSeconds: MATCH.COUNTDOWN_SECONDS,
  };
}

// ============================================
// Create a new room
// ============================================

export function createRoomState(
  hostId: string,
  name: string,
  settings: Partial<RoomSettings> = {},
  visibility: RoomVisibility = 'public',
  password?: string
): Room {
  const merged: RoomSettings = { ...defaultRoomSettings(), ...settings };
  return {
    id: uuidv4(),
    name,
    hostId,
    visibility,
    password,
    difficulty: merged.difficulty,
    maxPlayers: merged.maxPlayers,
    currentPlayers: 1,
    playerIds: [hostId],
    isInMatch: false,
    createdAt: new Date(),
    settings: merged,
  };
}

// ============================================
// Redis persistence
// ============================================

export async function saveRoomState(room: Room): Promise<void> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.roomState(room.id);
  await redis.setex(key, CACHE_TTL.ROOM_STATE, JSON.stringify(room));
}

export async function loadRoomState(roomId: string): Promise<Room | null> {
  const redis = getRedisClient();
  const key = REDIS_KEYS.roomState(roomId);
  const raw = await redis.get(key);
  if (!raw) return null;
  try {
    const room = JSON.parse(raw) as Room;
    room.createdAt = new Date(room.createdAt);
    return room;
  } catch {
    logger.warn('[RoomState] Failed to parse room state', { roomId });
    return null;
  }
}

export async function deleteRoomState(roomId: string): Promise<void> {
  const redis = getRedisClient();
  await redis.del(REDIS_KEYS.roomState(roomId));
}

// ============================================
// Room mutations (return new state, persist)
// ============================================

export function addPlayerToRoom(room: Room, playerId: string): Room {
  if (room.playerIds.includes(playerId)) return room;
  return {
    ...room,
    playerIds: [...room.playerIds, playerId],
    currentPlayers: room.currentPlayers + 1,
  };
}

export function removePlayerFromRoom(room: Room, playerId: string): Room {
  const playerIds = room.playerIds.filter((id) => id !== playerId);
  // If host left, transfer host to next player
  let hostId = room.hostId;
  if (hostId === playerId && playerIds.length > 0) {
    hostId = playerIds[0]!;
  }
  return {
    ...room,
    playerIds,
    currentPlayers: Math.max(0, room.currentPlayers - 1),
    hostId,
  };
}

export function isRoomFull(room: Room): boolean {
  return room.currentPlayers >= room.maxPlayers;
}

export function isRoomEmpty(room: Room): boolean {
  return room.currentPlayers === 0;
}

export function canJoinRoom(room: Room, playerId: string, password?: string): {
  canJoin: boolean;
  reason?: string;
} {
  if (room.isInMatch) return { canJoin: false, reason: 'Match already in progress' };
  if (isRoomFull(room)) return { canJoin: false, reason: 'Room is full' };
  if (room.playerIds.includes(playerId)) return { canJoin: false, reason: 'Already in room' };
  if (room.visibility === 'private' && room.password && room.password !== password) {
    return { canJoin: false, reason: 'Incorrect password' };
  }
  return { canJoin: true };
}
