// ============================================
// Validation Engine — runs all validators
// ============================================

import type { FullCase } from '../models/Case';
import { validateCase } from '../validators/caseValidator';
import { validateTimeline } from '../validators/timelineValidator';
import { validateEvidence } from '../validators/evidenceValidator';
import { validateUniqueness } from '../validators/uniquenessValidator';
import logger from '../../utils/logger';

export interface FullValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  checks: {
    case: ReturnType<typeof validateCase>;
    timeline: ReturnType<typeof validateTimeline>;
    evidence: ReturnType<typeof validateEvidence>;
    uniqueness: ReturnType<typeof validateUniqueness>;
  };
}

export class ValidationEngine {
  validate(fullCase: FullCase): FullValidationResult {
    const killer = fullCase.suspects.find((s) => s.isKiller);
    const allSuspectIds = fullCase.suspects.map((s) => s.id);

    const caseResult = validateCase(fullCase);
    const timelineResult = validateTimeline(fullCase.timeline, fullCase.victim.timeOfDeath);
    const evidenceResult = killer
      ? validateEvidence(fullCase.evidencePool, killer, allSuspectIds)
      : { isValid: false, errors: ['No killer found'], warnings: [] };
    const uniquenessResult = validateUniqueness(fullCase);

    const allErrors = [
      ...caseResult.errors,
      ...timelineResult.errors,
      ...evidenceResult.errors,
      ...uniquenessResult.errors,
    ];

    const allWarnings = [
      ...caseResult.warnings,
      ...timelineResult.warnings,
      ...evidenceResult.warnings,
      ...uniquenessResult.warnings,
    ];

    const isValid = allErrors.length === 0;

    if (!isValid) {
      logger.warn('[ValidationEngine] Case failed validation', {
        caseId: fullCase.id,
        errors: allErrors,
      });
    }

    if (allWarnings.length > 0) {
      logger.debug('[ValidationEngine] Case validation warnings', {
        caseId: fullCase.id,
        warnings: allWarnings,
      });
    }

    return {
      isValid,
      errors: allErrors,
      warnings: allWarnings,
      checks: {
        case: caseResult,
        timeline: timelineResult,
        evidence: evidenceResult,
        uniqueness: uniquenessResult,
      },
    };
  }
}
