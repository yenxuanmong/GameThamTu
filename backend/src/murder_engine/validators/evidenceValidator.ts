// ============================================
// Evidence Validator
// ============================================

import type { Evidence } from '../models/Evidence';
import type { Suspect } from '../models/Suspect';
import type { ValidationResult } from './caseValidator';

export function validateEvidence(
  evidencePool: Evidence[],
  killer: Suspect,
  allSuspectIds: string[]
): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (evidencePool.length === 0) {
    errors.push('Evidence pool is empty');
    return { isValid: false, errors, warnings };
  }

  const realEvidence = evidencePool.filter((e) => e.isReal);
  const fakeEvidence = evidencePool.filter((e) => e.isFakeEvidence);

  // At least one piece of real evidence must point to killer
  const killerEvidence = realEvidence.filter((e) => e.pointsTo === killer.id);
  if (killerEvidence.length === 0) {
    errors.push('No real evidence implicates the killer — case is unsolvable');
  }

  // No real evidence should point to non-existent suspects
  for (const e of realEvidence) {
    if (e.pointsTo && !allSuspectIds.includes(e.pointsTo) && e.pointsTo !== 'victim') {
      errors.push(`Evidence "${e.name}" points to unknown suspect ID: ${e.pointsTo}`);
    }
  }

  // Fake evidence should not accidentally point to killer
  const fakeThatPointsToKiller = fakeEvidence.filter((e) => e.pointsTo === killer.id);
  if (fakeThatPointsToKiller.length > 0) {
    warnings.push(
      `${fakeThatPointsToKiller.length} fake evidence piece(s) point to killer — may reduce red-herring effectiveness`
    );
  }

  // Warn if ratio is skewed too heavily toward fake
  const fakeRatio = fakeEvidence.length / evidencePool.length;
  if (fakeRatio > 0.6) {
    warnings.push(`High fake evidence ratio (${(fakeRatio * 100).toFixed(0)}%) may overwhelm players`);
  }

  // All evidences must have caseId
  const missingCaseId = evidencePool.filter((e) => !e.caseId);
  if (missingCaseId.length > 0) {
    errors.push(`${missingCaseId.length} evidence item(s) missing caseId`);
  }

  return { isValid: errors.length === 0, errors, warnings };
}
