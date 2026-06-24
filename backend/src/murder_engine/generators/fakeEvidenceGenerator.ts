// ============================================
// Fake / Red Herring Evidence Generator
// ============================================

import type { Evidence } from '../models/Evidence';
import type { Suspect } from '../models/Suspect';
import type { SeededRandom } from '../../utils/random';
import { createEvidence } from '../models/Evidence';

const FAKE_EVIDENCE_TEMPLATES = [
  {
    name: 'Planted note',
    description: (suspect: Suspect) =>
      `A handwritten note found near the scene — the handwriting appears to match ${suspect.name}. ` +
      `However, the paper is unusually fresh for the supposed date.`,
    type: 'document' as const,
  },
  {
    name: 'Suspicious footprint',
    description: (suspect: Suspect) =>
      `A boot print in the garden — size and shape consistent with ${suspect.name}'s footwear. ` +
      `However, the print is suspiciously isolated with no trail leading to it.`,
    type: 'physical' as const,
  },
  {
    name: 'Misleading witness statement',
    description: (suspect: Suspect) =>
      `An anonymous letter claims to have seen ${suspect.name} near the crime scene at the time of death. ` +
      `The letter is unsigned and the claim cannot be independently corroborated.`,
    type: 'testimonial' as const,
  },
  {
    name: 'Forged receipt',
    description: (suspect: Suspect) =>
      `A receipt found in the victim's desk appears to show a transaction with ${suspect.name}. ` +
      `On closer inspection, the ink is inconsistent and the date may have been altered.`,
    type: 'document' as const,
  },
  {
    name: 'Tampered photograph',
    description: (suspect: Suspect) =>
      `A photograph showing ${suspect.name} with the victim in a heated exchange. ` +
      `The image quality and framing raise questions about its authenticity.`,
    type: 'digital' as const,
  },
  {
    name: 'Planted personal item',
    description: (suspect: Suspect) =>
      `An item belonging to ${suspect.name} — a button or handkerchief — found at the crime scene. ` +
      `${suspect.name} denies being there and cannot account for how it arrived.`,
    type: 'physical' as const,
  },
  {
    name: 'False alibi evidence',
    description: (suspect: Suspect) =>
      `Documents suggesting ${suspect.name} was at a specific location during the murder. ` +
      `On closer examination, the timestamps are internally inconsistent.`,
    type: 'document' as const,
  },
  {
    name: 'Misleading financial record',
    description: (suspect: Suspect) =>
      `A financial record appearing to show ${suspect.name} had significant dealings with the victim — ` +
      `dealings that, if real, would suggest a strong motive. The record\'s provenance is unclear.`,
    type: 'document' as const,
  },
];

export interface FakeEvidenceGenerationInput {
  caseId: string;
  innocentSuspects: Suspect[];
  numFakeEvidence: number;
  rng: SeededRandom;
}

export function generateFakeEvidence(input: FakeEvidenceGenerationInput): Evidence[] {
  const { caseId, innocentSuspects, numFakeEvidence, rng } = input;

  if (innocentSuspects.length === 0) return [];

  const templates = rng.shuffle(FAKE_EVIDENCE_TEMPLATES);
  const result: Evidence[] = [];

  for (let i = 0; i < numFakeEvidence; i++) {
    const template = templates[i % templates.length]!;
    // Distribute fake evidence across innocent suspects to maximise misdirection
    const target = innocentSuspects[i % innocentSuspects.length]!;

    result.push(
      createEvidence(caseId, {
        type: template.type,
        name: template.name,
        description: template.description(target),
        location: rng.pick(['crime_scene', 'suspect_residence', 'public_area', 'victim_residence']),
        relatesTo: [target.id],
        isReal: false,
        isFakeEvidence: true,
        pointsTo: target.id, // misleadingly points to innocent suspect
      })
    );
  }

  return result;
}
