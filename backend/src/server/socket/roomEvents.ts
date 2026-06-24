// ============================================
// Room Socket Events
// ============================================

import type { Server as SocketIOServer } from 'socket.io';
import type { AuthenticatedSocket } from './socketServer';
import type { ServerToClientEvents } from '../../types/match.types';
import {
  createRoom,
  joinRoom,
  leaveRoom,
  getRoom,
  getPublicRooms,
} from '../rooms/roomManager';
import { startMatch } from '../../services/matchService';
import { defaultRoomSettings } from '../rooms/roomState';
import logger from '../../utils/logger';

export function registerRoomEvents(
  io: SocketIOServer<any, ServerToClientEvents>,
  socket: AuthenticatedSocket
): void {
  const { playerId } = socket;

  // ============================================
  // Create room
  // ============================================
  socket.on('room:create', (data) => {
    void (async () => {
      try {
        const settings = {
          ...defaultRoomSettings(data.settings.difficulty),
          ...data.settings,
        };

        const room = await createRoom(playerId, `${socket.username}'s Room`, settings, 'public');

        // Join socket room
        await socket.join(`room:${room.id}`);

        socket.emit('room:joined', { room, playerId });

        logger.debug('[RoomEvents] Room created', { roomId: room.id, playerId });
      } catch (err) {
        logger.error('[RoomEvents] Create room failed', { err, playerId });
        socket.emit('error', { code: 'ROOM_CREATE_FAILED', message: 'Failed to create room' });
      }
    })();
  });

  // ============================================
  // Join room
  // ============================================
  socket.on('room:join', (data) => {
    void (async () => {
      try {
        const result = await joinRoom(data.roomId, playerId, data.password);

        if (!result.success || !result.room) {
          socket.emit('error', { code: 'JOIN_FAILED', message: result.error ?? 'Failed to join' });
          return;
        }

        await socket.join(`room:${data.roomId}`);
        socket.emit('room:joined', { room: result.room, playerId });

        // Notify others in room
        socket.to(`room:${data.roomId}`).emit('room:updated', { room: result.room });

        logger.debug('[RoomEvents] Player joined room', { roomId: data.roomId, playerId });
      } catch (err) {
        logger.error('[RoomEvents] Join room failed', { err, playerId });
        socket.emit('error', { code: 'JOIN_FAILED', message: 'Failed to join room' });
      }
    })();
  });

  // ============================================
  // Leave room
  // ============================================
  socket.on('room:leave', (data) => {
    void (async () => {
      try {
        const { room, wasEmpty } = await leaveRoom(data.roomId, playerId);

        await socket.leave(`room:${data.roomId}`);
        socket.emit('room:left', { roomId: data.roomId, playerId });

        if (!wasEmpty && room) {
          io.to(`room:${data.roomId}`).emit('room:updated', { room });
        }

        logger.debug('[RoomEvents] Player left room', { roomId: data.roomId, playerId });
      } catch (err) {
        logger.error('[RoomEvents] Leave room failed', { err, playerId });
      }
    })();
  });

  // ============================================
  // Ready up
  // ============================================
  socket.on('room:ready', (data) => {
    void (async () => {
      try {
        const room = await getRoom(data.roomId);
        if (!room) {
          socket.emit('error', { code: 'ROOM_NOT_FOUND', message: 'Room not found' });
          return;
        }

        // Notify room of ready state
        io.to(`room:${data.roomId}`).emit('room:updated', { room });

        // Auto-start: if all players are ready and settings allow
        if (room.settings.autoStart && room.currentPlayers >= 2) {
          // Countdown
          let countdown = room.settings.startCountdownSeconds;
          const countdownInterval = setInterval(() => {
            io.to(`room:${data.roomId}`).emit('room:countdown', { seconds: countdown });
            countdown--;

            if (countdown < 0) {
              clearInterval(countdownInterval);
              void (async () => {
                const match = await startMatch(room.currentMatchId ?? '');
                if (match) {
                  io.to(`room:${data.roomId}`).emit('match:started', {
                    matchId: match.id,
                    caseId: match.caseId,
                  });
                }
              })();
            }
          }, 1000);
        }
      } catch (err) {
        logger.error('[RoomEvents] Ready failed', { err, playerId });
      }
    })();
  });

  // ============================================
  // Disconnect: auto-leave rooms
  // ============================================
  socket.on('disconnect', () => {
    // Find rooms this player is in and remove them
    // Socket.IO handles socket room cleanup automatically
    logger.debug('[RoomEvents] Socket disconnected, cleaning up', { playerId });
  });
}
