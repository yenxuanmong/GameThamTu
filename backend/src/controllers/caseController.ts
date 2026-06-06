// ============================================
// Case Controller
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { query, param, validationResult } from 'express-validator';
import {
  listCases,
  getCaseById,
  getCaseSuspects,
  getCaseWitnesses,
  getCaseEvidence,
  getCaseTimeline,
  getCaseSolution,
  getCaseStats,
} from '../services/caseService';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';
import type { DifficultyLevel } from '../types/case.types';

// ============================================
// Validation
// ============================================

export const listCasesValidation = [
  query('page').optional().isInt({ min: 1 }).toInt(),
  query('pageSize').optional().isInt({ min: 1, max: 100 }).toInt(),
  query('difficulty')
    .optional()
    .isIn(['easy', 'medium', 'hard', 'expert', 'nightmare']),
];

// ============================================
// List cases
// ============================================

export async function getCases(
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
    const pageSize = (req.query['pageSize'] as unknown as number) ?? 20;
    const difficulty = req.query['difficulty'] as DifficultyLevel | undefined;

    const result = await listCases(page, pageSize, difficulty);

    res.json({
      cases: result.cases,
      pagination: {
        page,
        pageSize,
        total: result.total,
        totalPages: Math.ceil(result.total / pageSize),
      },
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get single case details
// ============================================

export async function getCase(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { caseId } = req.params as { caseId: string };

    const caseDetails = await getCaseById(caseId);
    if (!caseDetails) {
      throw new AppError(404, 'Case not found', 'NOT_FOUND');
    }

    res.json({ case: caseDetails });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get case suspects
// ============================================

export async function getCaseSuspectsHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { caseId } = req.params as { caseId: string };

    const caseDetails = await getCaseById(caseId);
    if (!caseDetails) {
      throw new AppError(404, 'Case not found', 'NOT_FOUND');
    }

    const suspects = await getCaseSuspects(caseId);
    res.json({ suspects });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get case witnesses
// ============================================

export async function getCaseWitnessesHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { caseId } = req.params as { caseId: string };

    const caseDetails = await getCaseById(caseId);
    if (!caseDetails) {
      throw new AppError(404, 'Case not found', 'NOT_FOUND');
    }

    const witnesses = await getCaseWitnesses(caseId);
    res.json({ witnesses });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get case evidence list
// ============================================

export async function getCaseEvidenceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { caseId } = req.params as { caseId: string };

    const caseDetails = await getCaseById(caseId);
    if (!caseDetails) {
      throw new AppError(404, 'Case not found', 'NOT_FOUND');
    }

    const evidence = await getCaseEvidence(caseId);
    res.json({ evidence });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get case timeline
// ============================================

export async function getCaseTimelineHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { caseId } = req.params as { caseId: string };

    const caseDetails = await getCaseById(caseId);
    if (!caseDetails) {
      throw new AppError(404, 'Case not found', 'NOT_FOUND');
    }

    const timeline = await getCaseTimeline(caseId);
    res.json({ timeline });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get case solution (only after match ends)
// ============================================

export async function getCaseSolutionHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { caseId } = req.params as { caseId: string };
    const { matchId } = req.query as { matchId: string };

    if (!matchId) {
      throw new AppError(400, 'matchId query param required', 'VALIDATION_ERROR');
    }

    const solution = await getCaseSolution(caseId, matchId);
    if (!solution) {
      throw new AppError(
        403,
        'Solution is only available after the match ends',
        'MATCH_NOT_FINISHED'
      );
    }

    res.json({ solution });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get case stats
// ============================================

export async function getCaseStatsHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { caseId } = req.params as { caseId: string };

    const caseDetails = await getCaseById(caseId);
    if (!caseDetails) {
      throw new AppError(404, 'Case not found', 'NOT_FOUND');
    }

    const stats = await getCaseStats(caseId);
    res.json({ stats });
  } catch (err) {
    next(err);
  }
}
