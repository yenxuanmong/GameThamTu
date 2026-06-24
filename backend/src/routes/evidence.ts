// ============================================
// Evidence Routes — /api/matches/:matchId/evidence
// ============================================

import { Router } from 'express';
import {
  getMyEvidence,
  getEvidence,
  updateNotes,
  shareEvidenceHandler,
  getNotebookHandler,
  deleteNoteHandler,
  updateNotesValidation,
  shareEvidenceValidation,
} from '../controllers/evidenceController';
import { requireAuth } from '../middleware/authMiddleware';

const router = Router({ mergeParams: true });

// All routes require auth
router.use(requireAuth);

// Evidence in a match
router.get('/', getMyEvidence);
router.get('/:evidenceId', getEvidence);
router.patch('/:evidenceId/notes', updateNotesValidation, updateNotes);
router.post('/:evidenceId/share', shareEvidenceValidation, shareEvidenceHandler);

// Notebook
router.get('/notebook', getNotebookHandler);
router.delete('/notebook/:entryId', deleteNoteHandler);

export default router;
