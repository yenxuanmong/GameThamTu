// ============================================
// Achievement Controller — /api/auth/me/achievements
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { getPlayerAchievements } from '../services/achievementService';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';

// GET /api/auth/me/achievements
export async function getMyAchievements(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const achievements = await getPlayerAchievements(playerId);
    res.json({ achievements });
  } catch (err) {
    next(err);
  }
}
