// ============================================
// Camera / Digital Evidence Generator
// ============================================

import type { Evidence } from '../models/Evidence';
import type { Suspect } from '../models/Suspect';
import type { CaseVictim, MurderLocation } from '../../types/case.types';
import type { SeededRandom } from '../../utils/random';
import { createEvidence } from '../models/Evidence';

export interface CameraGenerationInput {
  caseId: string;
  victim: CaseVictim;
  killer: Suspect;
  suspects: Suspect[];
  murderLocation: MurderLocation;
  murderTime: Date;
  rng: SeededRandom;
}

export function generateCameraEvidence(input: CameraGenerationInput): Evidence[] {
  const { caseId, victim, killer, suspects, murderLocation, murderTime, rng } = input;
  const result: Evidence[] = [];

  const murderHour = murderTime.getHours();
  const murderMin = murderTime.getMinutes();
  const timeStr = `${murderHour.toString().padStart(2, '0')}:${murderMin.toString().padStart(2, '0')}`;

  // 1. Partial hallway footage — shows killer but timestamp is unclear
  const footageDesc = rng.nextBool(0.5)
    ? `Security footage from the hallway camera. At ${timeStr}, a figure matching ${killer.name}'s description ` +
      `is visible heading toward the ${murderLocation.replace('_', ' ')}. The timestamp is partially obscured.`
    : `Hallway camera footage — corrupted between ${timeStr} and ${String(murderHour + 1).padStart(2, '0')}:00. ` +
      `The gap coincides precisely with the estimated time of death.`;

  result.push(
    createEvidence(caseId, {
      type: 'digital',
      name: 'Security camera footage',
      description: footageDesc,
      location: 'crime_scene',
      relatesTo: [killer.id, victim.id],
      isReal: true,
      isFakeEvidence: false,
      pointsTo: rng.nextBool(0.5) ? killer.id : undefined,
      metadata: {
        deviceType: 'camera',
        timestamp: murderTime.toISOString(),
        isEncrypted: false,
        requiresHacking: false,
        recordedPersonIds: [killer.id],
      },
    })
  );

  // 2. Phone records (if complexity warrants it)
  if (rng.nextBool(0.6)) {
    const callTimeOffset = rng.nextInt(-60, -10); // call before murder
    const callTime = new Date(murderTime.getTime() + callTimeOffset * 60_000);
    const callTimeStr = `${callTime.getHours().toString().padStart(2, '0')}:${callTime.getMinutes().toString().padStart(2, '0')}`;

    result.push(
      createEvidence(caseId, {
        type: 'digital',
        name: 'Telephone exchange records',
        description:
          `Exchange records show a call made from the household telephone to ${killer.name}'s known number ` +
          `at ${callTimeStr} — approximately ${Math.abs(callTimeOffset)} minutes before the estimated time of death. ` +
          `Duration: ${rng.nextInt(2, 8)} minutes.`,
        location: 'digital',
        relatesTo: [killer.id, victim.id],
        isReal: true,
        isFakeEvidence: false,
        pointsTo: rng.nextBool(0.4) ? killer.id : undefined,
        metadata: {
          deviceType: 'phone',
          timestamp: callTime.toISOString(),
          isEncrypted: false,
          requiresHacking: false,
        },
      })
    );
  }

  // 3. Red-herring camera footage pointing to innocent suspect
  if (suspects.length > 1) {
    const innocent = rng.pick(suspects.filter((s) => !s.isKiller));
    if (innocent) {
      const innocentTime = new Date(murderTime.getTime() - rng.nextInt(10, 30) * 60_000);
      const innocentTimeStr = `${innocentTime.getHours().toString().padStart(2, '0')}:${innocentTime.getMinutes().toString().padStart(2, '0')}`;

      result.push(
        createEvidence(caseId, {
          type: 'digital',
          name: 'Gate camera log',
          description:
            `Entry log from the gate camera records ${innocent.name} entering the property at ${innocentTimeStr}. ` +
            `No corresponding exit is recorded until much later — though the camera is known to malfunction occasionally.`,
          location: 'crime_scene',
          relatesTo: [innocent.id],
          isReal: false,
          isFakeEvidence: true,
          pointsTo: innocent.id,
          metadata: {
            deviceType: 'security_system',
            timestamp: innocentTime.toISOString(),
            isEncrypted: false,
            requiresHacking: false,
          },
        })
      );
    }
  }

  return result;
}
