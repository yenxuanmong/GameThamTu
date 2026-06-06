// ============================================
// Suspicion AI
// — tracks and updates per-player suspicion levels against suspects
// ============================================

import type { Suspect } from '../../murder_engine/models/Suspect';
import type { Evidence } from '../../murder_engine/models/Evidence';
import type { InvestigationProgress } from '../../types/player.types';
import type { FullCase } from '../../murder_engine/models/Case';

export interface SuspicionProfile {
  suspectId: string;
  suspicionScore: number;      // 0–100
  evidenceAgainst: string[];   // evidenceIds pointing to this suspect
  alibiStatus: 'unchecked' | 'verified' | 'disputed' | 'broken';
  keyFactors: string[];        // human-readable reasons for suspicion score
}

export interface SuspicionMap {
  [suspectId: string]: SuspicionProfile;
}

// ============================================
// Compute suspicion levels based on discovered evidence
// ============================================

export function computeSuspicionMap(
  fullCase: FullCase,
  progress: InvestigationProgress
): SuspicionMap {
  const map: SuspicionMap = {};

  // Initialise all suspects
  for (const suspect of fullCase.suspects) {
    map[suspect.id] = {
      suspectId: suspect.id,
      suspicionScore: 10, // baseline suspicion (everyone is slightly suspect)
      evidenceAgainst: [],
      alibiStatus: 'unchecked',
      keyFactors: [],
    };
  }

  // Process discovered evidence
  const discoveredEvidence = fullCase.evidencePool.filter((e) =>
    progress.discoveredEvidenceIds.includes(e.id)
  );

  for (const evidence of discoveredEvidence) {
    if (!evidence.pointsTo) continue;

    const profile = map[evidence.pointsTo];
    if (!profile) continue;

    const weight = computeEvidenceWeight(evidence);
    profile.suspicionScore = Math.min(100, profile.suspicionScore + weight);
    profile.evidenceAgainst.push(evidence.id);
    profile.keyFactors.push(`Evidence: ${evidence.name}`);
  }

  // Process notebook entries (player's own notes)
  for (const note of progress.suspectList) {
    const profile = map[note.suspectId];
    if (!profile) continue;
    // Blend player's manual suspicion with computed score
    profile.suspicionScore = Math.round(
      profile.suspicionScore * 0.7 + note.suspicionLevel * 0.3
    );
  }

  // Check alibi status for suspects questioned via witnesses
  for (const suspect of fullCase.suspects) {
    const profile = map[suspect.id];
    if (!profile) continue;

    // If a witness who knows the suspect has been interrogated, alibi status updates
    const interrogatedWitnesses = fullCase.witnesses.filter((w) =>
      progress.interrogatedWitnessIds.includes(w.id) &&
      w.relationships.some((r) => r.targetId === suspect.id)
    );

    if (interrogatedWitnesses.length > 0) {
      if (!suspect.alibi.isTrue) {
        profile.alibiStatus = 'disputed';
        profile.suspicionScore = Math.min(100, profile.suspicionScore + 15);
        profile.keyFactors.push('Alibi disputed by witness');
      } else {
        profile.alibiStatus = 'verified';
        profile.suspicionScore = Math.max(0, profile.suspicionScore - 10);
        profile.keyFactors.push('Alibi corroborated');
      }
    }
  }

  return map;
}

// ============================================
// Get top suspects by suspicion score
// ============================================

export function getRankedSuspects(
  suspicionMap: SuspicionMap,
  suspects: Suspect[]
): Array<{ suspect: Suspect; profile: SuspicionProfile }> {
  return suspects
    .map((s) => ({
      suspect: s,
      profile: suspicionMap[s.id] ?? {
        suspectId: s.id,
        suspicionScore: 0,
        evidenceAgainst: [],
        alibiStatus: 'unchecked' as const,
        keyFactors: [],
      },
    }))
    .sort((a, b) => b.profile.suspicionScore - a.profile.suspicionScore);
}

// ============================================
// Check if player is on the right track
// ============================================

export interface InvestigationAccuracy {
  topSuspectIsKiller: boolean;
  killerSuspicionRank: number;      // 1 = top suspect, higher = worse
  killerSuspicionScore: number;
  isOnRightTrack: boolean;
  suggestion?: string;
}

export function assessInvestigationAccuracy(
  suspicionMap: SuspicionMap,
  fullCase: FullCase
): InvestigationAccuracy {
  const killerId = fullCase.solution.killerId;
  const ranked = getRankedSuspects(suspicionMap, fullCase.suspects);

  const killerIndex = ranked.findIndex((r) => r.suspect.id === killerId);
  const killerProfile = suspicionMap[killerId];

  const killerRank = killerIndex + 1;
  const topSuspectIsKiller = killerRank === 1;
  const isOnRightTrack = killerRank <= 2;

  let suggestion: string | undefined;
  if (!isOnRightTrack && killerProfile) {
    const missingEvidence = fullCase.evidencePool.filter(
      (e) => e.isReal && e.pointsTo === killerId && !killerProfile.evidenceAgainst.includes(e.id)
    );
    if (missingEvidence.length > 0) {
      suggestion = `There is still undiscovered evidence at the scene.`;
    }
  }

  return {
    topSuspectIsKiller,
    killerSuspicionRank: killerRank,
    killerSuspicionScore: killerProfile?.suspicionScore ?? 0,
    isOnRightTrack,
    suggestion,
  };
}

// ============================================
// Helpers
// ============================================

function computeEvidenceWeight(evidence: Evidence): number {
  // Real evidence pointing to killer: high weight
  // Fake evidence: still adds weight (player doesn't know it's fake)
  const baseWeight: Record<string, number> = {
    physical:       20,
    forensic:       25,
    digital:        18,
    document:       15,
    testimonial:    10,
    circumstantial: 8,
    alibi:          12,
  };

  const base = baseWeight[evidence.type] ?? 10;

  // Fake evidence has same apparent weight — that's the point
  return base;
}
