// ============================================
// Killer Generator
// ============================================

import type { Motive, MurderWeapon, MurderLocation } from '../../types/case.types';
import type { Suspect, SuspectAlibi, SuspectPersonality, SuspectRelationship } from '../models/Suspect';
import type { SeededRandom } from '../../utils/random';
import { createKiller } from '../models/Suspect';

const KILLER_FIRST_NAMES_MALE = [
  'Edward', 'Gerald', 'Roland', 'Calvin', 'Dorian', 'Wallace',
  'Norton', 'Jasper', 'Clifton', 'Horace', 'Alistair', 'Barnaby',
];

const KILLER_FIRST_NAMES_FEMALE = [
  'Helena', 'Vivian', 'Celeste', 'Miriam', 'Estelle', 'Dorothea',
  'Sylvia', 'Eugenia', 'Bernadette', 'Isadora', 'Winifred', 'Philippa',
];

const KILLER_LAST_NAMES = [
  'Morley', 'Crane', 'Ashby', 'Devereaux', 'Wickham', 'Stride',
  'Beaumont', 'Pelham', 'Thorne', 'Carver', 'Lorne', 'Madden',
];

const OCCUPATIONS = [
  'solicitor', 'physician', 'banker', 'estate manager', 'professor',
  'journalist', 'playwright', 'chemist', 'retired officer', 'merchant',
  'housekeeper', 'antiquarian', 'stockbroker', 'architect', 'inspector',
];

const PERSONALITIES: SuspectPersonality[] = [
  'arrogant', 'timid', 'charming', 'aggressive', 'calculating', 'paranoid',
];

const ALIBI_TEMPLATES: ((weapon: MurderWeapon, location: MurderLocation) => string)[] = [
  (_w, _l) => 'Was attending a dinner party across town — several guests can verify this.',
  (_w, _l) => 'Claims to have been at the library all evening, though no one can confirm the specific time.',
  (_w, _l) => 'States they were home alone, reading — no alibi witness.',
  (_w, _l) => 'Was seen at the train station, though the timing is inconclusive.',
  (_w, _l) => 'Claims to have been on a telephone call — records are being checked.',
  (_w, _l) => 'Was at their club, though members recall seeing them leave early.',
];

export interface KillerGenerationInput {
  caseId: string;
  motive: Motive;
  weapon: MurderWeapon;
  location: MurderLocation;
  victimId: string;
  victimName: string;
  orderOfCreation: number;
  rng: SeededRandom;
}

export function generateKiller(input: KillerGenerationInput): Suspect {
  const { caseId, motive, weapon, location, victimId, victimName, orderOfCreation, rng } = input;

  const useMale = rng.nextBool();
  const firstName = rng.pick(useMale ? KILLER_FIRST_NAMES_MALE : KILLER_FIRST_NAMES_FEMALE);
  const lastName = rng.pick(KILLER_LAST_NAMES);
  const name = `${firstName} ${lastName}`;
  const age = rng.nextInt(25, 65);
  const occupation = rng.pick(OCCUPATIONS);
  const personality = rng.pick(PERSONALITIES);

  const alibiTemplate = rng.pick(ALIBI_TEMPLATES);
  const alibi: SuspectAlibi = {
    description: alibiTemplate(weapon, location),
    isTrue: false, // killers always lie about their alibi
  };

  // Killer has a meaningful relationship with the victim tied to motive
  const victimRelationship = buildVictimRelationship(motive, victimId, victimName, rng);

  const backstory = buildKillerBackstory(name, occupation, motive, victimName);

  return createKiller(
    caseId,
    {
      caseId,
      name,
      age,
      occupation,
      backstory,
      personality,
      alibi,
      relationships: [victimRelationship],
      secretsKnown: [],
      nervousnessLevel: rng.nextFloat(0.4, 0.7),
      orderOfCreation,
    },
    motive,
    weapon,
    location
  );
}

function buildVictimRelationship(
  motive: Motive,
  victimId: string,
  victimName: string,
  rng: SeededRandom
): SuspectRelationship {
  const motiveRelationshipMap: Record<Motive, string[]> = {
    greed:       ['business partner', 'estranged relative', 'financial rival', 'co-investor'],
    jealousy:    ['colleague', 'rival', 'former friend', 'academic rival'],
    revenge:     ['former employer', 'old adversary', 'betrayed associate', 'former mentor turned enemy'],
    blackmail:   ['acquaintance', 'former colleague', 'reluctant associate'],
    inheritance: ['family member', 'distant relative', 'close relative', 'named heir'],
    fear:        ['witness to past crime', 'former confidant', 'disgruntled associate'],
    love:        ['former romantic partner', 'obsessive admirer', 'spurned lover'],
    power:       ['political rival', 'professional competitor', 'ambitious underling'],
    rivalry:     ['long-time rival', 'competing professional', 'academic adversary'],
    self_defense:['threatening associate', 'confrontational acquaintance'],
    ideology:    ['ideological opponent', 'representative of opposed faction'],
    other:       ['associate', 'acquaintance'],
  };

  const types = motiveRelationshipMap[motive] ?? ['acquaintance'];
  const type = rng.pick(types);

  return {
    targetId: victimId,
    type,
    description: `${type.charAt(0).toUpperCase() + type.slice(1)} of ${victimName}`,
    isHidden: rng.nextBool(0.4), // 40% chance they hide the relationship
  };
}

function buildKillerBackstory(
  name: string,
  occupation: string,
  motive: Motive,
  victimName: string
): string {
  const templates: Record<Motive, string> = {
    greed:
      `${name}, a ${occupation}, had long coveted the wealth tied up in ${victimName}'s estate. ` +
      `A seemingly polished exterior concealed a calculating mind obsessed with financial gain.`,
    jealousy:
      `${name} had spent years in the shadow of ${victimName}'s success. ` +
      `As a ${occupation}, the constant comparison had curdled admiration into something far darker.`,
    revenge:
      `${name} — a ${occupation} — had never forgotten what ${victimName} did years ago. ` +
      `The betrayal had cost everything, and the grudge had only deepened with time.`,
    blackmail:
      `${name} had been quietly paying ${victimName} for months, desperate to keep a secret buried. ` +
      `When the demands escalated, this ${occupation} concluded that silence could be bought another way.`,
    inheritance:
      `As a ${occupation} and direct beneficiary, ${name} stood to inherit considerably upon ${victimName}'s death — ` +
      `an arrangement that was about to be altered in favour of someone else.`,
    fear:
      `${name}, a ${occupation}, had lived in dread of ${victimName} exposing a past transgression. ` +
      `When exposure seemed imminent, desperation overtook reason.`,
    love:
      `A ${occupation} by profession, ${name}'s fixation on ${victimName} had crossed from devotion into obsession. ` +
      `Rejection, when it finally came, proved catastrophic.`,
    power:
      `${name}, an ambitious ${occupation}, viewed ${victimName} as the sole obstacle between them and ` +
      `a position of considerable influence. The opportunity arose, and patience ran out.`,
    rivalry:
      `${name} and ${victimName} had been rivals for decades. ` +
      `As a ${occupation}, ${name} had suffered too many defeats at the victim's hands.`,
    self_defense:
      `${name}, a ${occupation}, claims the confrontation with ${victimName} started differently — ` +
      `but the outcome speaks for itself, and the truth of that evening remains contested.`,
    ideology:
      `${name} is a ${occupation} whose deeply held convictions placed them in direct opposition to everything ${victimName} represented.`,
    other:
      `${name}, a ${occupation}, had a complicated history with ${victimName} that few fully understood.`,
  };

  return templates[motive] ?? templates['other']!;
}
