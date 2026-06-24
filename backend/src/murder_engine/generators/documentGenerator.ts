// ============================================
// Document Generator (letters, notes, diary entries)
// ============================================

import type { Evidence } from '../models/Evidence';
import type { Suspect } from '../models/Suspect';
import type { CaseVictim, Motive } from '../../types/case.types';
import type { SeededRandom } from '../../utils/random';
import { createEvidence } from '../models/Evidence';

export interface DocumentGenerationInput {
  caseId: string;
  victim: CaseVictim;
  killer: Suspect;
  suspects: Suspect[];
  motive: Motive;
  rng: SeededRandom;
  includeRedHerring: boolean;
}

export function generateDocumentEvidence(input: DocumentGenerationInput): Evidence[] {
  const { caseId, victim, killer, suspects, motive, rng, includeRedHerring } = input;
  const documents: Evidence[] = [];

  // 1. Victim's diary — always generated
  const diary = generateVictimDiary(caseId, victim, killer, motive, rng);
  documents.push(diary);

  // 2. A letter related to the motive
  const letter = generateMotiveLetter(caseId, victim, killer, motive, rng);
  documents.push(letter);

  // 3. Optional red-herring document pointing to innocent suspect
  if (includeRedHerring && suspects.length > 1) {
    const innocent = rng.pick(suspects.filter((s) => !s.isKiller));
    if (innocent) {
      documents.push(generateRedHerringDocument(caseId, victim, innocent, rng));
    }
  }

  return documents;
}

function generateVictimDiary(
  caseId: string,
  victim: CaseVictim,
  killer: Suspect,
  motive: Motive,
  rng: SeededRandom
): Evidence {
  const motiveEntries: Record<Motive, string> = {
    greed:
      `"${new Date().toDateString()} — I must change my will before it is too late. ` +
      `${killer.name} has made their intentions clear, and I will not allow it."`,
    blackmail:
      `"The demands continue. ${killer.name} knows too much, and each month the price rises. ` +
      `I have decided I must go to the authorities, whatever the personal cost."`,
    revenge:
      `"${killer.name} came to see me again today. The old grievance — I thought it buried. ` +
      `Their expression as they left unsettled me greatly."`,
    inheritance:
      `"I have spoken with my solicitor. The new will is nearly ready. ` +
      `I do not expect everyone to receive this news graciously."`,
    jealousy:
      `"${killer.name} was here this evening. ` +
      `There is a bitterness in them I had not noticed before — it sits behind the eyes."`,
    fear:
      `"I told ${killer.name} what I know. I thought it would put an end to things. ` +
      `I may have been mistaken in making that disclosure."`,
    love:
      `"I must be more firm. ${killer.name} will not accept what I have told them. ` +
      `The devotion has become something else entirely, and it frightens me."`,
    power:
      `"The announcement will be made on Friday. ${killer.name} is the only one who stands ` +
      `to lose significantly from this decision — and they know it."`,
    rivalry:
      `"After all these years, the rivalry has taken a darker turn. ` +
      `${killer.name}'s conduct at yesterday's meeting was barely civil."`,
    self_defense:
      `"${killer.name} made a threat today that I do not believe was idle. ` +
      `I have taken precautions, but I confess I am uncertain they will be sufficient."`,
    ideology:
      `"They came again — ${killer.name}. The argument was fierce. ` +
      `I cannot yield on this point, and they know it."`,
    other:
      `"${killer.name} visited this evening. Something is wrong — I cannot yet say what."`,
  };

  const entry = motiveEntries[motive] ?? motiveEntries['other']!;
  const isRedacted = rng.nextBool(0.3);

  return createEvidence(caseId, {
    type: 'document',
    name: `${victim.name}'s private diary`,
    description:
      `A leather-bound diary belonging to the victim. ` +
      (isRedacted
        ? `Several recent pages have been torn out, but one entry remains: ${entry}`
        : `The final entry reads: ${entry}`),
    location: 'victim_residence',
    relatesTo: [victim.id, killer.id],
    isReal: true,
    isFakeEvidence: false,
    pointsTo: rng.nextBool(0.6) ? killer.id : undefined,
    metadata: { isRedacted, lastEntry: entry },
  });
}

function generateMotiveLetter(
  caseId: string,
  victim: CaseVictim,
  killer: Suspect,
  motive: Motive,
  rng: SeededRandom
): Evidence {
  const letters: Record<Motive, { from: string; to: string; content: string }> = {
    greed: {
      from: victim.name,
      to: 'My Solicitor',
      content: `'I wish to amend my testament with immediate effect. Certain parties are to be removed entirely.'`,
    },
    blackmail: {
      from: killer.name,
      to: victim.name,
      content: `'You know what I require. Failure to comply will have consequences you cannot afford.'`,
    },
    inheritance: {
      from: 'Solicitor\'s Office',
      to: victim.name,
      content: `'Your revised will is ready for signature. I note a significant change to the primary beneficiary.'`,
    },
    revenge: {
      from: killer.name,
      to: victim.name,
      content: `'What you did to me cannot simply be forgiven. You owe a debt that has never been settled.'`,
    },
    jealousy: {
      from: killer.name,
      to: victim.name,
      content: `'Congratulations on yet another triumph. I trust the recognition sits well with you.'`,
    },
    fear: {
      from: victim.name,
      to: 'unnamed recipient',
      content: `'If something should happen to me, look closely at what I have enclosed. Do not discount it.'`,
    },
    love: {
      from: killer.name,
      to: victim.name,
      content: `'You cannot simply end things as though they meant nothing. I will not allow it to conclude this way.'`,
    },
    power: {
      from: victim.name,
      to: 'Board Members',
      content: `'My decision is final. The appointment will proceed as announced. I expect no further objections.'`,
    },
    rivalry: {
      from: killer.name,
      to: victim.name,
      content: `'You have taken everything from me through deceit. It will not stand.'`,
    },
    self_defense: {
      from: victim.name,
      to: 'unnamed recipient',
      content: `'Should anything occur, please ensure the enclosed document reaches the proper authorities.'`,
    },
    ideology: {
      from: killer.name,
      to: victim.name,
      content: `'You have been warned. What you represent will not be permitted to continue.'`,
    },
    other: {
      from: 'unknown sender',
      to: victim.name,
      content: `'We need to speak urgently. Tonight, without fail. Come alone.'`,
    },
  };

  const letterData = letters[motive] ?? letters['other']!;

  return createEvidence(caseId, {
    type: 'document',
    name: 'Recovered correspondence',
    description:
      `A letter found at the scene, dated shortly before the murder. ` +
      `From: ${letterData.from}. To: ${letterData.to}. ` +
      `Content: ${letterData.content}`,
    location: rng.pick(['crime_scene', 'victim_residence', 'suspect_residence']),
    relatesTo: [victim.id, killer.id],
    isReal: true,
    isFakeEvidence: false,
    pointsTo: rng.nextBool(0.5) ? killer.id : undefined,
    metadata: { from: letterData.from, to: letterData.to },
  });
}

function generateRedHerringDocument(
  caseId: string,
  victim: CaseVictim,
  suspect: Suspect,
  rng: SeededRandom
): Evidence {
  const templates = [
    `A note in ${suspect.name}'s handwriting listing times and locations. ` +
    `At first glance, it resembles surveillance notes — but may simply be a schedule.`,
    `An exchange of letters between ${victim.name} and ${suspect.name} describing a heated dispute, ` +
    `though the dispute was apparently resolved amicably.`,
    `A financial document showing a transfer from ${victim.name} to ${suspect.name} — ` +
    `the purpose of the payment is not stated.`,
  ];

  return createEvidence(caseId, {
    type: 'document',
    name: 'Ambiguous document',
    description: rng.pick(templates),
    location: rng.pick(['crime_scene', 'victim_residence', 'public_area']),
    relatesTo: [suspect.id, victim.id],
    isReal: false,
    isFakeEvidence: true,
    pointsTo: suspect.id,
  });
}
