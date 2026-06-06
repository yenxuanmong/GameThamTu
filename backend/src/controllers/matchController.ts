// ============================================
// Match Controller
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { body, query, param, validationResult } from 'express-validator';
import { prisma } from '../configs/database';
import { getMatchState, getCaseFomMatch } from '../services/matchService';
import { getPlayerEvidence, getNotebook } from '../services/evidenceService';
import { getPlayerSessions } from '../services/npcService';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';
import logger from '../utils/logger';

// ============================================
// Get match state
// ============================================

export async function getMatch(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { matchId } = req.params as { matchId: string };

    const match = await getMatchState(matchId);
    if (!match) {
      throw new AppError(404, 'Match not found', 'NOT_FOUND');
    }

    // Verify player is in the match
    if (!match.playerIds.includes(playerId)) {
      throw new AppError(403, 'You are not a participant in this match', 'FORBIDDEN');
    }

    // Strip solution from case data
    const safeMatch = {
      ...match,
      // don't expose raw conclusions to other players
      conclusions: match.status === 'finished' ? match.conclusions : [],
    };

    res.json({ match: safeMatch });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get match players
// ============================================

export async function getMatchPlayers(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { matchId } = req.params as { matchId: string };

    const players = await prisma.matchPlayer.findMany({
      where: { matchId },
      include: {
        player: {
          select: {
            id: true,
            username: true,
            avatarUrl: true,
            rank: { select: { tier: true, points: true } },
          },
        },
      },
    });

    res.json({
      players: players.map((mp) => ({
        playerId: mp.playerId,
        username: mp.player.username,
        avatarUrl: mp.player.avatarUrl,
        tier: mp.player.rank?.tier ?? 'rookie',
        points: mp.player.rank?.points ?? 0,
        isHost: mp.isHost,
        isReady: mp.isReady,
        hasSubmitted: mp.hasSubmitted,
        joinedAt: mp.joinedAt,
      })),
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get match scores (only when finished)
// ============================================

export async function getMatchScores(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { matchId } = req.params as { matchId: string };

    const match = await getMatchState(matchId);
    if (!match) {
      throw new AppError(404, 'Match not found', 'NOT_FOUND');
    }

    if (match.status !== 'finished') {
      throw new AppError(403, 'Scores are not available until the match is finished', 'MATCH_NOT_FINISHED');
    }

    const scores = await prisma.matchScore.findMany({
      where: { matchId },
      orderBy: { finalRank: 'asc' },
      include: {
        player: {
          select: { id: true, username: true, avatarUrl: true },
        },
      },
    });

    res.json({
      scores: scores.map((s) => ({
        playerId: s.playerId,
        username: s.player.username,
        avatarUrl: s.player.avatarUrl,
        totalScore: s.totalScore,
        breakdown: {
          killer: s.killerScore,
          motive: s.motiveScore,
          weapon: s.weaponScore,
          location: s.locationScore,
          timeline: s.timelineScore,
          narrative: s.narrativeScore,
        },
        timeBonus: s.timeBonus,
        isCorrect: s.isCorrect,
        rank: s.finalRank,
        rpChange: s.rpChange,
        result: s.result,
      })),
      winnerId: match.winnerId,
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get player's investigation progress
// ============================================

export async function getInvestigationProgress(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { matchId } = req.params as { matchId: string };

    const match = await getMatchState(matchId);
    if (!match || !match.playerIds.includes(playerId)) {
      throw new AppError(403, 'Access denied', 'FORBIDDEN');
    }

    const [evidenceProgress, notebook, npcSessions] = await Promise.all([
      getPlayerEvidence(matchId, playerId),
      getNotebook(matchId, playerId),
      getPlayerSessions(matchId, playerId),
    ]);

    const matchPlayer = await prisma.matchPlayer.findUnique({
      where: { matchId_playerId: { matchId, playerId } },
      select: { hintsUsed: true, timeSpent: true },
    });

    res.json({
      evidence: evidenceProgress,
      notebook,
      npcSessions,
      hintsUsed: matchPlayer?.hintsUsed ?? 0,
      hintsRemaining: 3 - (matchPlayer?.hintsUsed ?? 0),
      timeSpent: matchPlayer?.timeSpent ?? 0,
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// List player's recent matches
// ============================================

export async function getPlayerRecentMatches(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const page = parseInt(req.query['page'] as string ?? '1', 10);
    const pageSize = Math.min(parseInt(req.query['pageSize'] as string ?? '10', 10), 50);

    const [matchPlayers, total] = await prisma.$transaction([
      prisma.matchPlayer.findMany({
        where: { playerId },
        orderBy: { joinedAt: 'desc' },
        skip: (page - 1) * pageSize,
        take: pageSize,
        select: {
          joinedAt: true,
          matchId: true,
          match: {
            select: {
              id: true,
              difficulty: true,
              status: true,
              startedAt: true,
              endedAt: true,
              case: { select: { title: true } },
            },
          },
        },
      }),
      prisma.matchPlayer.count({ where: { playerId } }),
    ]);

    const matchIds = matchPlayers.map((mp) => mp.matchId);
    const scores = await prisma.matchScore.findMany({
      where: { matchId: { in: matchIds }, playerId },
      select: {
        matchId: true,
        totalScore: true,
        isCorrect: true,
        finalRank: true,
        rpChange: true,
        result: true,
      },
    });
    const scoreMap = new Map(scores.map((s) => [s.matchId, s]));

    res.json({
      matches: matchPlayers.map((mp) => {
        const score = scoreMap.get(mp.matchId);
        return {
          matchId: mp.match.id,
          caseTitle: mp.match.case?.title ?? 'Unknown',
          difficulty: mp.match.difficulty,
          status: mp.match.status,
          startedAt: mp.match.startedAt,
          endedAt: mp.match.endedAt,
          score: score?.totalScore ?? null,
          isCorrect: score?.isCorrect ?? null,
          rank: score?.finalRank ?? null,
          rpChange: score?.rpChange ?? null,
          result: score?.result ?? null,
        };
      }),
      pagination: {
        page,
        pageSize,
        total,
        totalPages: Math.ceil(total / pageSize),
      },
    });
  } catch (err) {
    next(err);
  }
}
