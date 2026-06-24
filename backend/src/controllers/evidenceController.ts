// ============================================
// Evidence Controller
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { body, validationResult } from 'express-validator';
import {
  getPlayerEvidence,
  getEvidenceById,
  updateEvidenceNotes,
  shareEvidence,
  getNotebook,
  deleteNotebookEntry,
} from '../services/evidenceService';
import { getMatchState } from '../services/matchService';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';

// ============================================
// Get all evidence discovered by a player
// ============================================

export async function getMyEvidence(
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

    const progress = await getPlayerEvidence(matchId, playerId);
    res.json({ evidence: progress.discovered, stats: { total: progress.totalInCase, discoveryRate: progress.discoveryRate } });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get a single evidence item detail
// ============================================

export async function getEvidence(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { matchId, evidenceId } = req.params as { matchId: string; evidenceId: string };

    const evidence = await getEvidenceById(evidenceId, matchId, playerId);
    if (!evidence) {
      throw new AppError(404, 'Evidence not found or not accessible', 'NOT_FOUND');
    }

    res.json({ evidence });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Update notes on evidence
// ============================================

export const updateNotesValidation = [
  body('notes').isString().isLength({ max: 1000 }),
];

export async function updateNotes(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const { playerId } = (req as AuthenticatedRequest).player;
    const { matchId, evidenceId } = req.params as { matchId: string; evidenceId: string };
    const { notes } = req.body as { notes: string };

    const success = await updateEvidenceNotes(evidenceId, matchId, playerId, notes);
    if (!success) {
      throw new AppError(404, 'Evidence not found or not discovered by you', 'NOT_FOUND');
    }

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Share evidence with another player
// ============================================

export const shareEvidenceValidation = [
  body('toPlayerId').isUUID().withMessage('Invalid target player ID'),
];

export async function shareEvidenceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const { playerId } = (req as AuthenticatedRequest).player;
    const { matchId, evidenceId } = req.params as { matchId: string; evidenceId: string };
    const { toPlayerId } = req.body as { toPlayerId: string };

    if (toPlayerId === playerId) {
      throw new AppError(400, 'Cannot share evidence with yourself', 'INVALID_REQUEST');
    }

    const result = await shareEvidence(evidenceId, matchId, playerId, toPlayerId);
    if (!result.success) {
      throw new AppError(400, result.error ?? 'Share failed', 'SHARE_FAILED');
    }

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get notebook
// ============================================

export async function getNotebookHandler(
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

    const entries = await getNotebook(matchId, playerId);
    res.json({ notebook: entries });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Delete notebook entry
// ============================================

export async function deleteNoteHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { entryId } = req.params as { entryId: string };

    const success = await deleteNotebookEntry(entryId, playerId);
    if (!success) {
      throw new AppError(404, 'Note not found', 'NOT_FOUND');
    }

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}
