// ============================================
// Socket Server — initialises Socket.IO and registers all event handlers
// ============================================

import type { Server as HTTPServer } from 'http';
import { Server as SocketIOServer, Socket } from 'socket.io';
import { SOCKET_CONFIG } from '../../configs/socket';
import { registerRoomEvents } from './roomEvents';
import { registerPlayerEvents } from './playerEvents';
import { registerInvestigationEvents } from './investigationEvents';
import { registerVoiceGateway } from '../voice/voiceGateway';
import { startMatchmakingScheduler } from '../matchmaking/matchmaking';
import { verifyAccessToken } from '../../utils/auth';
import type { ServerToClientEvents, ClientToServerEvents, MatchmakingResult } from '../../types/match.types';
import logger from '../../utils/logger';

export type AuthenticatedSocket = Socket<ClientToServerEvents, ServerToClientEvents> & {
  playerId: string;
  username: string;
};

// ============================================
// Initialise
// ============================================

export function initSocketServer(httpServer: HTTPServer): SocketIOServer {
  const io = new SocketIOServer<ClientToServerEvents, ServerToClientEvents>(
    httpServer,
    SOCKET_CONFIG
  );

  // ---- Authentication middleware ----
  io.use((socket, next) => {
    const token =
      socket.handshake.auth['token'] as string | undefined ??
      socket.handshake.headers['authorization']?.replace('Bearer ', '');

    if (!token) {
      return next(new Error('Authentication required'));
    }

    try {
      const payload = verifyAccessToken(token);
      (socket as AuthenticatedSocket).playerId = payload.playerId;
      (socket as AuthenticatedSocket).username = payload.username;
      next();
    } catch {
      next(new Error('Invalid token'));
    }
  });

  // ---- Connection handler ----
  io.on('connection', (socket) => {
    const authedSocket = socket as AuthenticatedSocket;
    logger.info('[Socket] Player connected', {
      playerId: authedSocket.playerId,
      socketId: socket.id,
    });

    // Register event handlers
    registerRoomEvents(io, authedSocket);
    registerPlayerEvents(io, authedSocket);
    registerInvestigationEvents(io, authedSocket);
    registerVoiceGateway(io, authedSocket);

    // Join personal room for direct messages
    void socket.join(`player:${authedSocket.playerId}`);

    socket.on('disconnect', (reason) => {
      logger.info('[Socket] Player disconnected', {
        playerId: authedSocket.playerId,
        reason,
      });
    });

    socket.on('error', (err) => {
      logger.error('[Socket] Socket error', { playerId: authedSocket.playerId, err });
    });
  });

  // ---- Start matchmaking scheduler ----
  startMatchmakingScheduler((result: MatchmakingResult) => {
    if (!result.success || !result.matchId || !result.players) return;

    // Notify all players in the match
    for (const playerId of result.players) {
      io.to(`player:${playerId}`).emit('match:started', {
        matchId: result.matchId,
        caseId: '',  // populated after case generation
      });
    }
  });

  logger.info('[Socket] Server initialised');
  return io;
}

// ============================================
// Helpers for emitting to a room
// ============================================

export function emitToRoom<K extends keyof ServerToClientEvents>(
  io: SocketIOServer,
  roomId: string,
  event: K,
  data: Parameters<ServerToClientEvents[K]>[0]
): void {
  io.to(`room:${roomId}`).emit(event as string, data);
}

export function emitToPlayer<K extends keyof ServerToClientEvents>(
  io: SocketIOServer,
  playerId: string,
  event: K,
  data: Parameters<ServerToClientEvents[K]>[0]
): void {
  io.to(`player:${playerId}`).emit(event as string, data);
}
