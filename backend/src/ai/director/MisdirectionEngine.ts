// ============================================
// Misdirection Engine
// — manages how red herrings and fake evidence are surfaced
// ============================================

import type { FullCase } from '../../murder_engine/models/Case';
import type { Evidence } from '../../murder_engine/models/Evidence';
import type { InvestigationProgress } from '../../types/player.types';
import type { SeededRandom } from '../../utils/random';

export interface MisdirectionEvent {
  type: 'surface_fake_evidence' | 'npc_redirect' | 'false_rumour';
  evidenceId?: string;
  suspectId?: string;       // the innocent suspect being misdirected toward
  message?: string;
  scheduledAt: Date;
}

// ============================================
// Determine which fake evidence to surface first
// ============================================

export function planMisdirectionSequence(
  fullCase: FullCase,
  rng: SeededRandom
): MisdirectionEvent[] {
  const fakeEvidence = fullCase.evidencePool.filter((e) => e.isFakeEvidence);
  const shuffled = rng.shuffle(fakeEvidence);

  // Space out the misdirection events over the match
  const baseTime = new Date();
  const events: MisdirectionEvent[] = [];

  shuffled.forEach((e, index) => {
    const offsetMinutes = 3 + index * 4; // surface every ~4 minutes
    const scheduledAt = new Date(baseTime.getTime() + offsetMinutes * 60_000);

    events.push({
      type: 'surface_fake_evidence',
      evidenceId: e.id,
      suspectId: e.pointsTo,
      scheduledAt,
    });
  });

  return events;
}

// ============================================
// Get the next pending misdirection event
// ============================================

export function getNextMisdirectionEvent(
  events: MisdirectionEvent[],
  triggered: string[],    // already-triggered evidenceIds
  now: Date
): MisdirectionEvent | null {
  return (
    events.find(
      (e) =>
        e.scheduledAt <= now &&
        e.evidenceId &&
        !triggered.includes(e.evidenceId)
    ) ?? null
  );
}

// ============================================
// Assess misdirection effectiveness
// ============================================

export interface MisdirectionAssessment {
  playersLed_astray: string[];    // playerIds who suspected wrong person
  effectiveness: number;          // 0–1
}

export function assessMisdirection(
  fullCase: FullCase,
  progressMap: Map<string, InvestigationProgress>  // playerId → progress
): MisdirectionAssessment {
  const killerId = fullCase.solution.killerId;
  const playersLed_astray: string[] = [];

  for (const [playerId, progress] of progressMap) {
    const topSuspect = progress.suspectList
      .filter((s) => s.markedAsKiller)
      .sort((a, b) => b.suspicionLevel - a.suspicionLevel)[0];

    if (topSuspect && topSuspect.suspectId !== killerId) {
      playersLed_astray.push(playerId);
    }
  }

  const totalPlayers = progressMap.size;
  const effectiveness = totalPlayers > 0 ? playersLed_astray.length / totalPlayers : 0;

  return { playersLed_astray, effectiveness };
}

// ============================================
// Get fake evidence that implicates a specific suspect
// ============================================

export function getFakeEvidenceFor(
  fullCase: FullCase,
  suspectId: string
): Evidence[] {
  return fullCase.evidencePool.filter(
    (e) => e.isFakeEvidence && e.pointsTo === suspectId
  );
}
