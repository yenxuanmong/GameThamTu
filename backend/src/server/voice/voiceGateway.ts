// ============================================
// Voice Gateway — signalling for WebRTC voice chat
// ============================================

import type { Server as SocketIOServer } from 'socket.io';
import type { AuthenticatedSocket } from '../socket/socketServer';
import logger from '../../utils/logger';

// WebRTC signalling events (not in main socket type for simplicity)
interface RTCSessionDescriptionInit { type: string; sdp?: string }
interface RTCIceCandidateInit { candidate?: string; sdpMid?: string | null; sdpMLineIndex?: number | null }

interface VoiceEvents {
  'voice:offer': (data: { targetId: string; offer: RTCSessionDescriptionInit }) => void;
  'voice:answer': (data: { targetId: string; answer: RTCSessionDescriptionInit }) => void;
  'voice:ice_candidate': (data: { targetId: string; candidate: RTCIceCandidateInit }) => void;
  'voice:mute': (data: { matchId: string; muted: boolean }) => void;
  'voice:leave': (data: { matchId: string }) => void;
}

export function registerVoiceGateway(
  io: SocketIOServer,
  socket: AuthenticatedSocket
): void {
  const { playerId } = socket;

  // ---- WebRTC Offer (caller → callee) ----
  socket.on('voice:offer' as any, (data: VoiceEvents['voice:offer'] extends (d: infer D) => void ? D : never) => {
    if (!data.targetId || !data.offer) return;
    io.to(`player:${data.targetId}`).emit('voice:offer' as any, {
      fromId: playerId,
      offer: data.offer,
    });
    logger.debug('[VoiceGateway] Offer relayed', { from: playerId, to: data.targetId });
  });

  // ---- WebRTC Answer (callee → caller) ----
  socket.on('voice:answer' as any, (data: { targetId: string; answer: RTCSessionDescriptionInit }) => {
    if (!data.targetId || !data.answer) return;
    io.to(`player:${data.targetId}`).emit('voice:answer' as any, {
      fromId: playerId,
      answer: data.answer,
    });
  });

  // ---- ICE Candidate exchange ----
  socket.on('voice:ice_candidate' as any, (data: { targetId: string; candidate: RTCIceCandidateInit }) => {
    if (!data.targetId || !data.candidate) return;
    io.to(`player:${data.targetId}`).emit('voice:ice_candidate' as any, {
      fromId: playerId,
      candidate: data.candidate,
    });
  });

  // ---- Mute toggle ----
  socket.on('voice:mute' as any, (data: { matchId: string; muted: boolean }) => {
    socket.to(`room:${data.matchId}`).emit('voice:mute' as any, {
      playerId,
      muted: data.muted,
    });
  });

  // ---- Leave voice ----
  socket.on('voice:leave' as any, (data: { matchId: string }) => {
    socket.to(`room:${data.matchId}`).emit('voice:leave' as any, { playerId });
  });

  // ---- Disconnect: auto-leave voice ----
  socket.on('disconnect', () => {
    socket.broadcast.emit('voice:leave' as any, { playerId });
  });
}
