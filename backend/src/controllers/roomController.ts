// ============================================
// Room Controller — REST API for rooms
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { body, query, validationResult } from 'express-validator';
import { prisma } from '../configs/database';
import {
  createRoom,
  getRoom,
  getPublicRooms,
  leaveRoom,
} from '../server/rooms/roomManager';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';
import type { DifficultyLevel } from '../types/case.types';

// ============================================
// Validation
// ============================================

export const createRoomValidation = [
  body('name').optional().trim().isLength({ min: 1, max: 50 }),
  body('difficulty')
    .optional()
    .isIn(['easy', 'medium', 'hard', 'expert', 'nightmare']),
  body('maxPlayers').optional().isInt({ min: 2, max: 4 }),
  body('visibility').optional().isIn(['public', 'private']),
  body('password').optional().isString().isLength({ max: 32 }),
  body('settings.enableVoiceChat').optional().isBoolean(),
  body('settings.autoStart').optional().isBoolean(),
];

// ============================================
// List public rooms
// ============================================

export async function listRooms(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const difficulty = req.query['difficulty'] as DifficultyLevel | undefined;
    const page = Math.max(1, parseInt(req.query['page'] as string ?? '1', 10));
    const pageSize = Math.min(50, parseInt(req.query['pageSize'] as string ?? '20', 10));

    // In-memory public rooms (fast path — active rooms)
    const activeRooms = getPublicRooms().filter((r) =>
      !difficulty || r.difficulty === difficulty
    );

    // Also get rooms from DB that may not be in memory yet
    const where: Record<string, unknown> = {
      visibility: 'public',
      isInMatch: false,
    };
    if (difficulty) where['difficulty'] = difficulty;

    const [dbRooms, total] = await prisma.$transaction([
      prisma.room.findMany({
        where: where as any,
        orderBy: { createdAt: 'desc' },
        skip: (page - 1) * pageSize,
        take: pageSize,
        select: {
          id: true,
          name: true,
          hostId: true,
          visibility: true,
          difficulty: true,
          maxPlayers: true,
          currentPlayers: true,
          isInMatch: true,
          settings: true,
          createdAt: true,
          _count: { select: { matches: true } },
        },
      }),
      prisma.room.count({ where: where as any }),
    ]);

    // Merge with in-memory state (prefer memory for currentPlayers accuracy)
    const rooms = dbRooms.map((r) => {
      const live = activeRooms.find((a) => a.id === r.id);
      return {
        id: r.id,
        name: r.name,
        hostId: r.hostId,
        visibility: r.visibility,
        difficulty: r.difficulty,
        maxPlayers: r.maxPlayers,
        currentPlayers: live?.currentPlayers ?? r.currentPlayers,
        isInMatch: live?.isInMatch ?? r.isInMatch,
        hasPassword: false, // never expose whether private room has password in list
        settings: r.settings,
        createdAt: r.createdAt,
        totalMatches: r._count.matches,
      };
    });

    res.json({
      rooms,
      pagination: { page, pageSize, total, totalPages: Math.ceil(total / pageSize) },
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get single room
// ============================================

export async function getRoomById(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { roomId } = req.params as { roomId: string };

    // Try live state first
    const liveRoom = await getRoom(roomId);
    if (liveRoom) {
      // Strip password from response
      const { password: _pw, ...safeRoom } = liveRoom;
      res.json({ room: safeRoom });
      return;
    }

    // Fallback to DB
    const dbRoom = await prisma.room.findUnique({
      where: { id: roomId },
      select: {
        id: true,
        name: true,
        hostId: true,
        visibility: true,
        difficulty: true,
        maxPlayers: true,
        currentPlayers: true,
        isInMatch: true,
        settings: true,
        createdAt: true,
      },
    });

    if (!dbRoom) throw new AppError(404, 'Room not found', 'NOT_FOUND');

    res.json({ room: dbRoom });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Create room via REST (alternative to socket)
// ============================================

export async function createRoomREST(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const { playerId, username } = (req as AuthenticatedRequest).player;
    const {
      name,
      difficulty = 'medium',
      maxPlayers = 4,
      visibility = 'public',
      password,
      settings = {},
    } = req.body as {
      name?: string;
      difficulty?: DifficultyLevel;
      maxPlayers?: number;
      visibility?: 'public' | 'private';
      password?: string;
      settings?: Record<string, unknown>;
    };

    const roomName = name ?? `${username}'s Room`;

    const room = await createRoom(
      playerId,
      roomName,
      { difficulty, maxPlayers, ...settings } as any,
      visibility,
      password
    );

    const { password: _pw, ...safeRoom } = room;
    res.status(201).json({ room: safeRoom });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get players in a room
// ============================================

export async function getRoomPlayers(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { roomId } = req.params as { roomId: string };

    const liveRoom = await getRoom(roomId);
    if (!liveRoom) throw new AppError(404, 'Room not found', 'NOT_FOUND');

    // Get player details
    const players = await prisma.player.findMany({
      where: { id: { in: liveRoom.playerIds } },
      select: {
        id: true,
        username: true,
        avatarUrl: true,
        rank: { select: { tier: true, points: true } },
      },
    });

    res.json({
      players: players.map((p) => ({
        ...p,
        isHost: p.id === liveRoom.hostId,
        tier: p.rank?.tier ?? 'rookie',
        points: p.rank?.points ?? 0,
      })),
      hostId: liveRoom.hostId,
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Kick player from room (host only)
// ============================================

export async function kickPlayer(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId: hostId } = (req as AuthenticatedRequest).player;
    const { roomId, targetPlayerId } = req.params as {
      roomId: string;
      targetPlayerId: string;
    };

    const room = await getRoom(roomId);
    if (!room) throw new AppError(404, 'Room not found', 'NOT_FOUND');

    // Only host can kick
    if (room.hostId !== hostId) {
      throw new AppError(403, 'Only the host can kick players', 'FORBIDDEN');
    }

    // Cannot kick yourself
    if (targetPlayerId === hostId) {
      throw new AppError(400, 'Host cannot kick themselves', 'INVALID_TARGET');
    }

    // Target must be in the room
    if (!room.playerIds.includes(targetPlayerId)) {
      throw new AppError(404, 'Player not in this room', 'PLAYER_NOT_FOUND');
    }

    // Cannot kick during active match
    if (room.isInMatch) {
      throw new AppError(409, 'Cannot kick players during an active match', 'MATCH_IN_PROGRESS');
    }

    const { room: updatedRoom } = await leaveRoom(roomId, targetPlayerId);

    // Notify via Socket.IO if server is running
    const io = (global as any).io;
    if (io) {
      // Tell the kicked player
      io.to(`player:${targetPlayerId}`).emit('room:kicked', {
        roomId,
        reason: 'Kicked by host',
      });

      // Update remaining players
      if (updatedRoom) {
        io.to(`room:${roomId}`).emit('room:updated', { room: updatedRoom });
      }
    }

    res.json({ success: true, message: `Player ${targetPlayerId} was kicked` });
  } catch (err) {
    next(err);
  }
}

export async function deleteRoom(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { roomId } = req.params as { roomId: string };

    const room = await getRoom(roomId);
    if (!room) throw new AppError(404, 'Room not found', 'NOT_FOUND');
    if (room.hostId !== playerId) {
      throw new AppError(403, 'Only the host can close this room', 'FORBIDDEN');
    }
    if (room.isInMatch) {
      throw new AppError(409, 'Cannot close a room while a match is in progress', 'MATCH_IN_PROGRESS');
    }

    // Remove all players
    for (const pid of [...room.playerIds]) {
      await leaveRoom(roomId, pid).catch(() => void 0);
    }

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}
