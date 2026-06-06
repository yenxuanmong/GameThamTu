// ============================================
// Room Manager — high-level room lifecycle management
// ============================================

import type { Room, RoomSettings, RoomVisibility } from '../../types/match.types';
import type { DifficultyLevel } from '../../types/case.types';
import {
  createRoomState,
  saveRoomState,
  loadRoomState,
  deleteRoomState,
  addPlayerToRoom,
  removePlayerFromRoom,
  isRoomEmpty,
  canJoinRoom,
} from './roomState';
import { prisma } from '../../configs/database';
import logger from '../../utils/logger';

// In-memory room index (roomId → Room) for hot access
const roomIndex = new Map<string, Room>();

// ============================================
// Create
// ============================================

export async function createRoom(
  hostId: string,
  name: string,
  settings: Partial<RoomSettings> = {},
  visibility: RoomVisibility = 'public',
  password?: string
): Promise<Room> {
  const room = createRoomState(hostId, name, settings, visibility, password);

  roomIndex.set(room.id, room);
  await saveRoomState(room);

  // Persist to DB
  await prisma.room.create({
    data: {
      id: room.id,
      name: room.name,
      hostId: room.hostId,
      visibility: visibility === 'public' ? 'public' : 'private',
      passwordHash: password ?? null,
      difficulty: mapDifficulty(room.difficulty),
      maxPlayers: room.maxPlayers,
      currentPlayers: 1,
      settings: room.settings as object,
    },
  });

  logger.info('[RoomManager] Room created', { roomId: room.id, hostId });
  return room;
}

// ============================================
// Join
// ============================================

export async function joinRoom(
  roomId: string,
  playerId: string,
  password?: string
): Promise<{ success: boolean; room?: Room; error?: string }> {
  let room = roomIndex.get(roomId) ?? await loadRoomState(roomId);

  if (!room) return { success: false, error: 'Room not found' };

  const check = canJoinRoom(room, playerId, password);
  if (!check.canJoin) return { success: false, error: check.reason };

  room = addPlayerToRoom(room, playerId);
  roomIndex.set(roomId, room);
  await saveRoomState(room);

  await prisma.room.update({
    where: { id: roomId },
    data: { currentPlayers: room.currentPlayers },
  });

  logger.info('[RoomManager] Player joined room', { roomId, playerId });
  return { success: true, room };
}

// ============================================
// Leave
// ============================================

export async function leaveRoom(
  roomId: string,
  playerId: string
): Promise<{ room: Room | null; wasEmpty: boolean }> {
  let room = roomIndex.get(roomId) ?? await loadRoomState(roomId);
  if (!room) return { room: null, wasEmpty: false };

  room = removePlayerFromRoom(room, playerId);

  if (isRoomEmpty(room)) {
    // Clean up empty room
    roomIndex.delete(roomId);
    await deleteRoomState(roomId);
    await prisma.room.delete({ where: { id: roomId } }).catch(() => void 0);
    logger.info('[RoomManager] Room deleted (empty)', { roomId });
    return { room: null, wasEmpty: true };
  }

  roomIndex.set(roomId, room);
  await saveRoomState(room);
  await prisma.room.update({
    where: { id: roomId },
    data: { currentPlayers: room.currentPlayers, hostId: room.hostId },
  });

  logger.info('[RoomManager] Player left room', { roomId, playerId, remaining: room.currentPlayers });
  return { room, wasEmpty: false };
}

// ============================================
// Get / list
// ============================================

export async function getRoom(roomId: string): Promise<Room | null> {
  return roomIndex.get(roomId) ?? loadRoomState(roomId);
}

export function getPublicRooms(): Room[] {
  return Array.from(roomIndex.values()).filter(
    (r) => r.visibility === 'public' && !r.isInMatch
  );
}

// ============================================
// Mark room as in-match
// ============================================

export async function setRoomInMatch(roomId: string, matchId: string): Promise<void> {
  const room = roomIndex.get(roomId) ?? await loadRoomState(roomId);
  if (!room) return;

  const updated: Room = { ...room, isInMatch: true, currentMatchId: matchId };
  roomIndex.set(roomId, updated);
  await saveRoomState(updated);
  await prisma.room.update({
    where: { id: roomId },
    data: { isInMatch: true },
  });
}

export async function setRoomMatchEnded(roomId: string): Promise<void> {
  const room = roomIndex.get(roomId) ?? await loadRoomState(roomId);
  if (!room) return;

  const updated: Room = { ...room, isInMatch: false, currentMatchId: undefined };
  roomIndex.set(roomId, updated);
  await saveRoomState(updated);
  await prisma.room.update({
    where: { id: roomId },
    data: { isInMatch: false },
  });
}

// ============================================
// Helpers
// ============================================

function mapDifficulty(d: DifficultyLevel) {
  const map = {
    easy: 'easy' as const,
    medium: 'medium' as const,
    hard: 'hard' as const,
    expert: 'expert' as const,
    nightmare: 'nightmare' as const,
  };
  return map[d];
}
