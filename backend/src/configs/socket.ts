// ============================================
// Socket.IO Config
// ============================================

import { ServerOptions } from 'socket.io';

export const SOCKET_CONFIG: Partial<ServerOptions> = {
  cors: {
    origin: process.env['FRONTEND_URL'] ?? 'http://localhost:5173',
    methods: ['GET', 'POST'],
    credentials: true,
  },
  pingTimeout: 20000,
  pingInterval: 25000,
  transports: ['websocket', 'polling'],
  maxHttpBufferSize: 1e6, // 1MB
};
