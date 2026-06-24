// ============================================
// Ranking Controller
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { query, validationResult } from 'express-validator';
import {
  getLeaderboardPage,
  getPlayerRankInfo,
  getSeasonInfo,
  getAllSeasons,
  getPlayerMatchHistory,
  getNearbyRankings,
} from '../services/rankingService';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';

// ============================================
// Validation
// ============================================

export const leaderboardValidation = [
  query('page').optional().isInt({ min: 1 }).toInt(),
  query('pageSize').optional().isInt({ min: 1, max: 100 }).toInt(),
  query('season').optional().isInt({ min: 1 }).toInt(),
];

// ============================================
// Get leaderboard
// ============================================

export async function getLeaderboard(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const page = (req.query['page'] as unknown as number) ?? 1;
    const pageSize = (req.query['pageSize'] as unknown as number) ?? 50;
    const season = req.query['season'] as unknown as number | undefined;

    const entries = await getLeaderboardPage(page, pageSize, season);
    const seasonInfo = await getSeasonInfo();

    res.json({
      leaderboard: entries,
      season: seasonInfo,
      pagination: { page, pageSize },
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get current player's rank
// ============================================

export async function getMyRank(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;

    const rankInfo = await getPlayerRankInfo(playerId);
    if (!rankInfo) {
      throw new AppError(404, 'Rank information not found', 'NOT_FOUND');
    }

    const nearby = await getNearbyRankings(playerId, 3);

    res.json({ rank: rankInfo, nearby });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get specific player's rank
// ============================================

export async function getPlayerRankHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = req.params as { playerId: string };

    const rankInfo = await getPlayerRankInfo(playerId);
    if (!rankInfo) {
      throw new AppError(404, 'Player not found', 'NOT_FOUND');
    }

    // Don't expose sensitive info for other players
    const { ...publicRankInfo } = rankInfo;
    res.json({ rank: publicRankInfo });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get player match history
// ============================================

export async function getMatchHistory(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const page = parseInt(req.query['page'] as string ?? '1', 10);
    const pageSize = Math.min(parseInt(req.query['pageSize'] as string ?? '20', 10), 50);

    const history = await getPlayerMatchHistory(playerId, page, pageSize);
    res.json(history);
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get season info
// ============================================

export async function getSeasonInfoHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const season = await getSeasonInfo();
    if (!season) {
      throw new AppError(404, 'No active season', 'NOT_FOUND');
    }
    res.json({ season });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get all seasons
// ============================================

export async function getAllSeasonsHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const seasons = await getAllSeasons();
    res.json({ seasons });
  } catch (err) {
    next(err);
  }
}
