// ============================================
// Room Routes — /api/rooms
// ============================================

import { Router } from 'express';
import {
  listRooms,
  getRoomById,
  createRoomREST,
  getRoomPlayers,
  kickPlayer,
  deleteRoom,
  createRoomValidation,
} from '../controllers/roomController';
import { requireAuth, optionalAuth } from '../middleware/authMiddleware';

const router = Router();

// List public rooms — public (optional auth)
router.get('/', optionalAuth, listRooms);

// Single room — public
router.get('/:roomId', optionalAuth, getRoomById);

// Room players — public
router.get('/:roomId/players', getRoomPlayers);

// Create room via REST — authenticated
router.post('/', requireAuth, createRoomValidation, createRoomREST);

// Kick player — authenticated (host only)
router.post('/:roomId/kick/:targetPlayerId', requireAuth, kickPlayer);

// Delete room — authenticated (host only)
router.delete('/:roomId', requireAuth, deleteRoom);

export default router;
