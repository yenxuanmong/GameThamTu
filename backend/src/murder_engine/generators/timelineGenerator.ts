// ============================================
// Timeline Generator
// ============================================

import type { TimelineEvent } from '../models/Timeline';
import type { Suspect } from '../models/Suspect';
import type { CaseVictim, MurderLocation } from '../../types/case.types';
import type { SeededRandom } from '../../utils/random';
import { createTimelineEvent, sortTimeline } from '../models/Timeline';

export interface TimelineGenerationInput {
  caseId: string;
  victim: CaseVictim;
  killer: Suspect;
  suspects: Suspect[];
  murderLocation: MurderLocation;
  murderTime: Date;
  rng: SeededRandom;
  complexity: 'simple' | 'moderate' | 'complex';
}

export function generateTimeline(input: TimelineGenerationInput): TimelineEvent[] {
  const { caseId, victim, killer, suspects, murderLocation, murderTime, rng, complexity } = input;

  const events: TimelineEvent[] = [];
  let order = 0;

  // Base murder time reference (evening of the crime)
  const baseTime = new Date(murderTime);
  baseTime.setHours(18, 0, 0, 0); // 6 PM start of relevant period

  const addEvent = (
    offsetMinutes: number,
    description: string,
    involvedIds: string[],
    location: string,
    isKeyEvent: boolean,
    isPublicInfo: boolean,
    evidenceId?: string
  ) => {
    const timestamp = new Date(baseTime.getTime() + offsetMinutes * 60_000);
    events.push(
      createTimelineEvent(caseId, {
        timestamp,
        description,
        involvedIds,
        location,
        isKeyEvent,
        isPublicInfo,
        evidenceId,
        order: order++,
      })
    );
  };

  // ---- 1. Earlier that day (public knowledge) ----
  addEvent(30, `${victim.name} was seen having dinner at home, apparently in good health.`, [victim.id], 'dining_room', false, true);

  if (complexity !== 'simple') {
    addEvent(60, `${victim.name} received an unannounced visitor — identity disputed.`, [victim.id], 'living_room', false, true);
  }

  // ---- 2. Suspect movements (some public, some hidden) ----
  const innocentSuspects = suspects.filter((s) => !s.isKiller);
  for (const suspect of innocentSuspects.slice(0, Math.min(innocentSuspects.length, 2))) {
    const isPublic = rng.nextBool(0.7);
    const activities = [
      `${suspect.name} was seen leaving the premises at approximately 8:30 PM.`,
      `${suspect.name} was observed in the garden for a brief period before nightfall.`,
      `${suspect.name} was reported to have retired to their room early.`,
      `${suspect.name} was seen speaking quietly with ${victim.name} before the household retired.`,
    ];
    addEvent(rng.nextInt(60, 150), rng.pick(activities), [suspect.id], 'various', false, isPublic);
  }

  // ---- 3. Killer's clandestine movements (hidden by default) ----
  const timeBeforeMurder = rng.nextInt(20, 60);
  const murderOffsetMinutes = Math.round(
    (murderTime.getTime() - baseTime.getTime()) / 60_000
  );

  addEvent(
    murderOffsetMinutes - timeBeforeMurder,
    `${killer.name} was seen near the ${murderLocation.replace('_', ' ')} — purpose unknown.`,
    [killer.id],
    murderLocation,
    false,
    false // hidden — players must discover
  );

  // ---- 4. The murder itself (key event, hidden) ----
  addEvent(
    murderOffsetMinutes,
    `${victim.name} was murdered in the ${murderLocation.replace('_', ' ')}. ` +
    `The act was swift and premeditated.`,
    [killer.id, victim.id],
    murderLocation,
    true,
    false // solution only
  );

  // ---- 5. Post-murder cover-up (hidden) ----
  addEvent(
    murderOffsetMinutes + rng.nextInt(5, 20),
    `${killer.name} was observed leaving the area of the ${murderLocation.replace('_', ' ')} in apparent haste.`,
    [killer.id],
    murderLocation,
    true,
    false
  );

  // ---- 6. Discovery of the body (public) ----
  const discoveryOffset = murderOffsetMinutes + rng.nextInt(30, 90);
  addEvent(
    discoveryOffset,
    `The body of ${victim.name} was discovered in the ${murderLocation.replace('_', ' ')}. ` +
    `Authorities were summoned immediately.`,
    [victim.id],
    murderLocation,
    true,
    true
  );

  // ---- 7. Complex only: additional misdirection events ----
  if (complexity === 'complex') {
    if (innocentSuspects.length > 0) {
      const redHerring = rng.pick(innocentSuspects)!;
      addEvent(
        murderOffsetMinutes - rng.nextInt(5, 15),
        `${redHerring.name} was seen on the stairs near the ${murderLocation.replace('_', ' ')} — ` +
        `later confirmed to have been going to the kitchen.`,
        [redHerring.id],
        'hallway',
        false,
        true // public — meant to mislead
      );
    }

    addEvent(
      murderOffsetMinutes + rng.nextInt(60, 120),
      `A window was found open on the ground floor — possible exit route, though it was also often left open on warm evenings.`,
      [],
      'hallway',
      false,
      true
    );
  }

  return sortTimeline(events);
}
