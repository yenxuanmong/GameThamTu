// ============================================
// Dynamic Events
// — injects scripted narrative events during a match
// ============================================

import type { FullCase } from '../../murder_engine/models/Case';
import type { Match } from '../../types/match.types';
import type { SeededRandom } from '../../utils/random';

export type DynamicEventType =
  | 'witness_becomes_hostile'
  | 'new_evidence_found'
  | 'alibi_challenged'
  | 'suspect_panics'
  | 'anonymous_tip'
  | 'evidence_tampered';

export interface DynamicEvent {
  id: string;
  type: DynamicEventType;
  triggerAtPercent: number;      // % of match elapsed when this fires (0–1)
  message: string;
  affectedEntityId?: string;     // witnessId / suspectId / evidenceId
  isTriggered: boolean;
}

// ============================================
// Generate event schedule for a match
// ============================================

export function generateDynamicEvents(
  fullCase: FullCase,
  _match: Match,
  rng: SeededRandom
): DynamicEvent[] {
  const events: DynamicEvent[] = [];

  const killer = fullCase.suspects.find((s) => s.isKiller);
  const innocentSuspects = fullCase.suspects.filter((s) => !s.isKiller);
  const witnesses = fullCase.witnesses;

  // ---- Event 1: Anonymous tip (25% of match elapsed) ----
  const tipTargetIsKiller = rng.nextBool(0.4); // 40% chance the tip points to killer
  const tipTarget = tipTargetIsKiller
    ? killer
    : innocentSuspects.length > 0
      ? rng.pick(innocentSuspects)
      : killer;

  if (tipTarget) {
    events.push({
      id: `event_tip_${fullCase.id}`,
      type: 'anonymous_tip',
      triggerAtPercent: 0.25,
      message:
        `An anonymous note has been slipped under the door: ` +
        `"Don't overlook ${tipTarget.name.split(' ')[0]}. Things are not as they appear."`,
      affectedEntityId: tipTarget.id,
      isTriggered: false,
    });
  }

  // ---- Event 2: Witness becomes more hostile (40% elapsed) ----
  if (witnesses.length > 0) {
    const targetWitness = rng.pick(witnesses);
    events.push({
      id: `event_hostile_${fullCase.id}`,
      type: 'witness_becomes_hostile',
      triggerAtPercent: 0.40,
      message:
        `${targetWitness.name} has requested that all further questions be directed through counsel. ` +
        `Their cooperation level has decreased.`,
      affectedEntityId: targetWitness.id,
      isTriggered: false,
    });
  }

  // ---- Event 3: Alibi challenged (55% elapsed) ----
  // Target is always killer (their alibi is false)
  if (killer) {
    events.push({
      id: `event_alibi_${fullCase.id}`,
      type: 'alibi_challenged',
      triggerAtPercent: 0.55,
      message:
        `New information has come to light casting doubt on ${killer.name.split(' ')[0]}'s stated alibi. ` +
        `Investigators are advised to revisit this account.`,
      affectedEntityId: killer.id,
      isTriggered: false,
    });
  }

  // ---- Event 4: Suspect panic (70% elapsed, nightmare/expert only) ----
  if (rng.nextBool(0.5) && killer) {
    events.push({
      id: `event_panic_${fullCase.id}`,
      type: 'suspect_panics',
      triggerAtPercent: 0.70,
      message:
        `${killer.name.split(' ')[0]} was observed in a heated exchange with a member of staff. ` +
        `Witnesses describe their demeanour as agitated and evasive.`,
      affectedEntityId: killer.id,
      isTriggered: false,
    });
  }

  // ---- Event 5: Evidence tampered (80% elapsed, adds tension) ----
  const realEvidence = fullCase.evidencePool.filter((e) => e.isReal);
  if (realEvidence.length > 0 && rng.nextBool(0.3)) {
    const targetEvidence = rng.pick(realEvidence);
    events.push({
      id: `event_tampered_${fullCase.id}`,
      type: 'evidence_tampered',
      triggerAtPercent: 0.80,
      message:
        `Alert: "${targetEvidence.name}" has been reported disturbed since last examination. ` +
        `Someone in the house may be attempting to conceal the truth.`,
      affectedEntityId: targetEvidence.id,
      isTriggered: false,
    });
  }

  return events.sort((a, b) => a.triggerAtPercent - b.triggerAtPercent);
}

// ============================================
// Get events due to fire based on elapsed time
// ============================================

export function getDueEvents(
  events: DynamicEvent[],
  elapsedPercent: number
): DynamicEvent[] {
  return events.filter(
    (e) => !e.isTriggered && e.triggerAtPercent <= elapsedPercent
  );
}
