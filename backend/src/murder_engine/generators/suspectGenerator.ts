// ============================================
// Suspect Generator (non-killer suspects)
// ============================================

import type { Suspect, SuspectPersonality, SuspectAlibi, SuspectRelationship } from '../models/Suspect';
import type { SeededRandom } from '../../utils/random';
import { createSuspect } from '../models/Suspect';

const FIRST_NAMES_MALE = [
  'George', 'Albert', 'Frederick', 'Herbert', 'Stanley', 'Leonard',
  'Reginald', 'Clarence', 'Douglas', 'Raymond', 'Harold', 'Walter',
  'Bernard', 'Norman', 'Sidney', 'Wilfred', 'Bertram', 'Cecil',
];

const FIRST_NAMES_FEMALE = [
  'Edith', 'Florence', 'Mabel', 'Agnes', 'Dorothy', 'Ethel',
  'Gladys', 'Hilda', 'Irene', 'Kathleen', 'Lilian', 'Marjorie',
  'Nora', 'Olive', 'Phyllis', 'Rose', 'Vera', 'Winifred',
];

const LAST_NAMES = [
  'Butler', 'Chapman', 'Cooke', 'Dixon', 'Evans', 'Fletcher',
  'Gardner', 'Harris', 'Hughes', 'Jenkins', 'Knight', 'Lambert',
  'Mason', 'Nash', 'Owen', 'Parker', 'Quinn', 'Reynolds',
  'Shaw', 'Taylor', 'Underwood', 'Vaughan', 'Webb', 'Young',
];

const OCCUPATIONS = [
  'family solicitor', 'country doctor', 'retired banker', 'estate gardener',
  'personal secretary', 'cook and housekeeper', 'visiting clergyman', 'local constable',
  'schoolteacher', 'insurance clerk', 'telegraph operator', 'portrait painter',
  'bookkeeper', 'nurse', 'tradesman', 'governess', 'photographer', 'auctioneer',
];

const PERSONALITIES: SuspectPersonality[] = [
  'arrogant', 'timid', 'charming', 'aggressive', 'calculating',
  'paranoid', 'empathetic', 'cold',
];

const TRUE_ALIBI_TEMPLATES = [
  'Was having supper with family — three members can confirm.',
  'Attended the evening church service, corroborated by the vicar.',
  'Was at the public house until closing time — barman and patrons recall.',
  'Was visiting a patient all evening — medical records support this.',
  'Was on an overnight train, ticket stub found in coat pocket.',
  'Was at the theatre — programme and usher can confirm.',
  'Received and sent several telegrams that evening — times recorded.',
  'Was attending a lecture — signed the attendance register.',
];

const FALSE_ALIBI_TEMPLATES = [
  'Claims to have been home alone all evening — no one to confirm.',
  'Says they went for a long walk and returned late — no witnesses.',
  'Claims to have been working late at the office — building was locked, no sign of entry.',
  'States they were reading in the library — no one else present.',
  'Says they retired early due to illness — but the maid did not see them all evening.',
  'Claims they were at a friend\'s house — friend cannot be immediately reached.',
];

const RELATIONSHIP_TYPES = [
  'old friend', 'business associate', 'neighbour', 'distant relative',
  'former colleague', 'social acquaintance', 'childhood friend', 'rival',
  'creditor', 'debtor', 'professional contact', 'admirer',
];

export interface SuspectGenerationInput {
  caseId: string;
  victimId: string;
  victimName: string;
  existingSuspectNames: string[];
  orderOfCreation: number;
  rng: SeededRandom;
}

export function generateSuspect(input: SuspectGenerationInput): Suspect {
  const { caseId, victimId, victimName, existingSuspectNames, orderOfCreation, rng } = input;

  // Ensure unique names
  let name: string;
  let attempts = 0;
  do {
    const useMale = rng.nextBool();
    const firstName = rng.pick(useMale ? FIRST_NAMES_MALE : FIRST_NAMES_FEMALE);
    const lastName = rng.pick(LAST_NAMES);
    name = `${firstName} ${lastName}`;
    attempts++;
  } while (existingSuspectNames.includes(name) && attempts < 50);

  const age = rng.nextInt(22, 70);
  const occupation = rng.pick(OCCUPATIONS);
  const personality = rng.pick(PERSONALITIES);

  const alibiIsTrue = rng.nextBool(0.6); // 60% of non-killer suspects have true alibis
  const alibiTemplate = rng.pick(alibiIsTrue ? TRUE_ALIBI_TEMPLATES : FALSE_ALIBI_TEMPLATES);

  const alibi: SuspectAlibi = {
    description: alibiTemplate,
    isTrue: alibiIsTrue,
  };

  const victimRelationshipType = rng.pick(RELATIONSHIP_TYPES);
  const victimRelationship: SuspectRelationship = {
    targetId: victimId,
    type: victimRelationshipType,
    description: `${victimRelationshipType.charAt(0).toUpperCase() + victimRelationshipType.slice(1)} of ${victimName}`,
    isHidden: rng.nextBool(0.2), // 20% chance of hidden relationship
  };

  const backstory = buildSuspectBackstory(name, occupation, victimRelationshipType, victimName, rng);

  return createSuspect(caseId, {
    caseId,
    name,
    age,
    occupation,
    backstory,
    personality,
    isKiller: false,
    alibi,
    relationships: [victimRelationship],
    secretsKnown: [],
    nervousnessLevel: rng.nextFloat(0.1, 0.5),
    orderOfCreation,
  });
}

function buildSuspectBackstory(
  name: string,
  occupation: string,
  relationship: string,
  victimName: string,
  rng: SeededRandom
): string {
  const templates = [
    `${name}, a ${occupation}, had known ${victimName} for some years through their role as a ${relationship}. ` +
    `Outwardly composed, they carry an air of someone with something to protect.`,

    `A ${occupation} by trade, ${name} had a ${relationship} connection to ${victimName} that dated back several years. ` +
    `Their account of recent events has been notably inconsistent.`,

    `${name} describes themselves as merely a ${relationship} of ${victimName}, but those who know them suggest ` +
    `the relationship was rather more complicated. They work as a ${occupation}.`,

    `Known in the area as a ${occupation}, ${name} was on familiar terms with ${victimName} as a ${relationship}. ` +
    `${rng.nextBool() ? 'Several people report seeing them argue recently.' : 'Their manner has been noticeably agitated since the incident.'}`,
  ];

  return rng.pick(templates);
}
