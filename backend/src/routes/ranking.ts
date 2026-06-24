// ============================================
// Ranking Routes — /api/ranking
// ============================================

import { Router } from 'express';
import {
  getLeaderboard,
  getMyRank,
  getPlayerRankHandler,
  getMatchHistory,
  getSeasonInfoHandler,
  getAllSeasonsHandler,
  leaderboardValidation,
} from '../controllers/rankingController';
import { requireAuth, optionalAuth } from '../middleware/authMiddleware';

const router = Router();

// Season info — public
router.get('/seasons', getAllSeasonsHandler);
router.get('/seasons/current', getSeasonInfoHandler);

// Leaderboard — public
router.get('/leaderboard', optionalAuth, leaderboardValidation, getLeaderboard);

// My rank & history — authenticated
router.get('/me', requireAuth, getMyRank);
router.get('/me/history', requireAuth, getMatchHistory);

// Other player's rank — public
router.get('/player/:playerId', getPlayerRankHandler);

export default router;
