// ============================================
// Case Routes — /api/cases
// ============================================

import { Router } from 'express';
import {
  getCases,
  getCase,
  getCaseSuspectsHandler,
  getCaseWitnessesHandler,
  getCaseEvidenceHandler,
  getCaseTimelineHandler,
  getCaseSolutionHandler,
  getCaseStatsHandler,
  listCasesValidation,
} from '../controllers/caseController';
import { requireAuth, optionalAuth } from '../middleware/authMiddleware';

const router = Router();

// List cases — public
router.get('/', optionalAuth, listCasesValidation, getCases);

// Case detail — public
router.get('/:caseId', optionalAuth, getCase);

// Case sub-resources — public (no killer reveal)
router.get('/:caseId/suspects', getCaseSuspectsHandler);
router.get('/:caseId/witnesses', getCaseWitnessesHandler);
router.get('/:caseId/evidence', getCaseEvidenceHandler);
router.get('/:caseId/timeline', getCaseTimelineHandler);
router.get('/:caseId/stats', getCaseStatsHandler);

// Solution — authenticated + match must be finished
router.get('/:caseId/solution', requireAuth, getCaseSolutionHandler);

export default router;
