// ============================================
// Analytics Routes — /api/analytics
// ============================================

import { Router } from 'express';
import type { Request, Response, NextFunction } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';
import { buildPlayerReport } from '../analytics/playerAnalytics';
import { getPlayerAccuracyBreakdown, getAccuracyTrend, getCaseAccuracyStats } from '../analytics/accuracyAnalytics';
import { buildMatchSummary, getGlobalMatchStats } from '../analytics/matchAnalytics';
import { AppError } from '../middleware/errorHandler';

const router = Router();

// ---- My performance report ----
router.get('/me', requireAuth, async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const report = await buildPlayerReport(playerId);
    if (!report) throw new AppError(404, 'No data found', 'NOT_FOUND');
    res.json({ report });
  } catch (err) { next(err); }
});

// ---- My accuracy breakdown ----
router.get('/me/accuracy', requireAuth, async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const breakdown = await getPlayerAccuracyBreakdown(playerId);
    if (!breakdown) throw new AppError(404, 'No accuracy data', 'NOT_FOUND');
    res.json({ breakdown });
  } catch (err) { next(err); }
});

// ---- My score trend ----
router.get('/me/trend', requireAuth, async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const limit = Math.min(parseInt(req.query['limit'] as string ?? '20', 10), 50);
    const trend = await getAccuracyTrend(playerId, limit);
    res.json({ trend });
  } catch (err) { next(err); }
});

// ---- Match summary ----
router.get('/matches/:matchId', requireAuth, async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { matchId } = req.params as { matchId: string };
    const summary = await buildMatchSummary(matchId);
    if (!summary) throw new AppError(404, 'Match not found', 'NOT_FOUND');
    res.json({ summary });
  } catch (err) { next(err); }
});

// ---- Case accuracy stats ----
router.get('/cases/:caseId', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { caseId } = req.params as { caseId: string };
    const stats = await getCaseAccuracyStats(caseId);
    if (!stats) throw new AppError(404, 'Case not found', 'NOT_FOUND');
    res.json({ stats });
  } catch (err) { next(err); }
});

// ---- Global stats (public) ----
router.get('/global', async (_req: Request, res: Response, next: NextFunction) => {
  try {
    const stats = await getGlobalMatchStats();
    res.json({ stats });
  } catch (err) { next(err); }
});

export default router;
