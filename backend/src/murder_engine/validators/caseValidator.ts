// ============================================
// Case Validator
// ============================================

import type { FullCase } from '../models/Case';

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export function validateCase(fullCase: FullCase): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  // ---- Suspects ----
  const killers = fullCase.suspects.filter((s) => s.isKiller);
  if (killers.length === 0) errors.push('Case has no killer');
  if (killers.length > 1) errors.push(`Case has ${killers.length} killers — must have exactly 1`);

  if (fullCase.suspects.length < 2) errors.push('Case must have at least 2 suspects');

  // ---- Solution cross-reference ----
  const killer = killers[0];
  if (killer && killer.id !== fullCase.solution.killerId) {
    errors.push('Solution killerId does not match the generated killer suspect');
  }

  // ---- Evidence ----
  const realEvidence = fullCase.evidencePool.filter((e) => e.isReal);
  const fakeEvidence = fullCase.evidencePool.filter((e) => e.isFakeEvidence);

  if (realEvidence.length === 0) errors.push('Case has no real evidence');
  if (fakeEvidence.length === 0) warnings.push('Case has no fake evidence — consider adding red herrings');

  // Check key evidence exists — at least one piece points to killer
  const keyEvidence = realEvidence.filter((e) => e.pointsTo === fullCase.solution.killerId);
  if (keyEvidence.length === 0) errors.push('No real evidence points to the killer — case is unsolvable');

  // ---- Witnesses ----
  if (fullCase.witnesses.length === 0) errors.push('Case has no witnesses');

  // At least one witness should know something about the killer
  const informedWitnesses = fullCase.witnesses.filter((w) =>
    w.hiddenFacts.some(
      (f) => killer && f.toLowerCase().includes(killer.name.split(' ')[0]?.toLowerCase() ?? '')
    ) ||
    w.knownFacts.some(
      (f) => killer && f.toLowerCase().includes(killer.name.split(' ')[0]?.toLowerCase() ?? '')
    )
  );

  if (informedWitnesses.length === 0) {
    warnings.push('No witness has information about the killer — consider adding a key witness');
  }

  // ---- Timeline ----
  if (fullCase.timeline.length === 0) errors.push('Case has no timeline events');

  const keyEvents = fullCase.timeline.filter((e) => e.isKeyEvent);
  if (keyEvents.length === 0) errors.push('Timeline has no key events (murder itself not recorded)');

  // ---- Victim ----
  if (!fullCase.victim.timeOfDeath) errors.push('Victim has no time of death');

  // ---- Unique Solution ----
  const suspectIds = fullCase.suspects.map((s) => s.id);
  if (!suspectIds.includes(fullCase.solution.killerId)) {
    errors.push('Solution killer ID does not exist in the suspect list');
  }

  return {
    isValid: errors.length === 0,
    errors,
    warnings,
  };
}
