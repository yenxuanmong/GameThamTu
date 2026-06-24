// ============================================
// Uniqueness Validator — ensures no duplicate IDs / names
// ============================================

import type { FullCase } from '../models/Case';
import type { ValidationResult } from './caseValidator';

export function validateUniqueness(fullCase: FullCase): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  // ---- Suspect name uniqueness ----
  const suspectNames = fullCase.suspects.map((s) => s.name);
  const duplicateSuspectNames = suspectNames.filter(
    (name, index) => suspectNames.indexOf(name) !== index
  );
  if (duplicateSuspectNames.length > 0) {
    errors.push(`Duplicate suspect names: ${duplicateSuspectNames.join(', ')}`);
  }

  // ---- Witness name uniqueness ----
  const witnessNames = fullCase.witnesses.map((w) => w.name);
  const duplicateWitnessNames = witnessNames.filter(
    (name, index) => witnessNames.indexOf(name) !== index
  );
  if (duplicateWitnessNames.length > 0) {
    errors.push(`Duplicate witness names: ${duplicateWitnessNames.join(', ')}`);
  }

  // ---- Cross-check: suspect vs witness names ----
  const allNames = [...suspectNames, ...witnessNames, fullCase.victim.name];
  const crossDuplicates = allNames.filter((name, index) => allNames.indexOf(name) !== index);
  if (crossDuplicates.length > 0) {
    errors.push(`Name appears in multiple roles: ${[...new Set(crossDuplicates)].join(', ')}`);
  }

  // ---- Evidence ID uniqueness ----
  const evidenceIds = fullCase.evidencePool.map((e) => e.id);
  const duplicateEvidenceIds = evidenceIds.filter(
    (id, index) => evidenceIds.indexOf(id) !== index
  );
  if (duplicateEvidenceIds.length > 0) {
    errors.push(`Duplicate evidence IDs detected`);
  }

  // ---- Evidence name uniqueness (warning only) ----
  const evidenceNames = fullCase.evidencePool.map((e) => e.name);
  const duplicateEvidenceNames = evidenceNames.filter(
    (name, index) => evidenceNames.indexOf(name) !== index
  );
  if (duplicateEvidenceNames.length > 0) {
    warnings.push(`Duplicate evidence names (may confuse players): ${[...new Set(duplicateEvidenceNames)].join(', ')}`);
  }

  // ---- Case seed uniqueness check (can only warn, not enforce here) ----
  if (!fullCase.seed || fullCase.seed.trim() === '') {
    errors.push('Case has no seed — reproducibility is broken');
  }

  return { isValid: errors.length === 0, errors, warnings };
}
