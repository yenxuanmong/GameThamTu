// ============================================
// Match Routes — /api/matches
// ============================================

import { Router } from 'express';
import {
  getMatch,
  getMatchPlayers,
  getMatchScores,
  getInvestigationProgress,
  getPlayerRecentMatches,
} from '../controllers/matchController';
import { requireAuth } from '../middleware/authMiddleware';

const router = Router();

// All match routes require auth
router.use(requireAuth);

// Player's recent matches
router.get('/me', getPlayerRecentMatches);

// Match detail
router.get('/:matchId', getMatch);
router.get('/:matchId/players', getMatchPlayers);
router.get('/:matchId/scores', getMatchScores);
router.get('/:matchId/progress', getInvestigationProgress);

export default router;
