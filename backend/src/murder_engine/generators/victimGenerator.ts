// ============================================
// Victim Generator
// ============================================

import { v4 as uuidv4 } from 'uuid';
import type { CaseVictim, MurderLocation } from '../../types/case.types';
import type { SeededRandom } from '../../utils/random';

const VICTIM_FIRST_NAMES = [
  'Arthur', 'Eleanor', 'Victor', 'Margaret', 'Sebastian', 'Vivienne',
  'Edmund', 'Constance', 'Reginald', 'Beatrice', 'Leopold', 'Cordelia',
  'Francis', 'Harriet', 'Montgomery', 'Adelaide', 'Clifford', 'Rosalind',
  'Percival', 'Millicent', 'Rupert', 'Lavinia', 'Archibald', 'Prudence',
];

const VICTIM_LAST_NAMES = [
  'Blackwood', 'Ashford', 'Whitmore', 'Graves', 'Harrington', 'Crowe',
  'Dunmore', 'Finch', 'Pemberton', 'Voss', 'Langley', 'Holt',
  'Sinclair', 'Drayton', 'Montague', 'Wentworth', 'Fairfax', 'Mercer',
];

const VICTIM_OCCUPATIONS = [
  'wealthy businessman',
  'retired military officer',
  'renowned art collector',
  'celebrated novelist',
  'controversial politician',
  'famous stage actress',
  'secretive antique dealer',
  'prominent judge',
  'eccentric scientist',
  'disgraced diplomat',
  'celebrated chef',
  'reclusive heiress',
  'renowned physician',
  'mysterious art forger',
  'influential newspaper editor',
];

const BACKSTORY_TEMPLATES = [
  (name: string, occupation: string) =>
    `${name} was a ${occupation} known for ruthless ambition and numerous enemies. ` +
    `Despite outward success, dark secrets lurked beneath the polished facade.`,
  (name: string, occupation: string) =>
    `Once celebrated as a ${occupation}, ${name} had recently fallen from grace ` +
    `following a scandal that alienated former allies and emboldened enemies.`,
  (name: string, occupation: string) =>
    `${name}, a ${occupation} of considerable influence, was on the verge of ` +
    `revealing damaging information about several powerful individuals.`,
  (name: string, occupation: string) =>
    `A ${occupation} with a mysterious past, ${name} had accumulated wealth ` +
    `through means not entirely above reproach, making enemies along the way.`,
  (name: string, occupation: string) =>
    `${name} was a ${occupation} whose imminent death stood to benefit ` +
    `multiple people — each with their own compelling reason to act.`,
];

const CAUSE_OF_DEATH_MAP: Record<string, string[]> = {
  knife:           ['stab wound to the chest', 'multiple stab wounds', 'single precise stab to the heart'],
  gun:             ['gunshot wound to the head', 'single gunshot to the chest', 'gunshot wound at close range'],
  poison:          ['acute poisoning — traces found in the drink', 'poisoning by arsenic', 'lethal dose of cyanide'],
  blunt_object:    ['blunt force trauma to the head', 'severe blow to the skull', 'repeated blunt force injuries'],
  strangulation:   ['strangulation — ligature marks on the neck', 'manual strangulation', 'strangulation with cord'],
  drowning:        ['drowning — lungs filled with water', 'drowning, signs of struggle found', 'forced drowning'],
  fire:            ['smoke inhalation and burns', 'severe burns', 'set ablaze — burned beyond recognition'],
  explosion:       ['blast injuries from explosion', 'shrapnel wounds consistent with explosion', 'explosive device detonated at close range'],
  fall:            ['fatal fall from height', 'blunt trauma consistent with a fall', 'injuries consistent with a high fall — possible push'],
  other:           ['cause of death undetermined pending investigation', 'unusual circumstances'],
};

export function generateVictim(
  caseId: string,
  rng: SeededRandom,
  weapon: string,
  location: MurderLocation,
  murderTime: Date
): CaseVictim {
  const firstName = rng.pick(VICTIM_FIRST_NAMES);
  const lastName = rng.pick(VICTIM_LAST_NAMES);
  const name = `${firstName} ${lastName}`;
  const age = rng.nextInt(35, 75);
  const occupation = rng.pick(VICTIM_OCCUPATIONS);
  const backstoryTemplate = rng.pick(BACKSTORY_TEMPLATES);
  const backstory = backstoryTemplate(name, occupation);

  const causeOptions = CAUSE_OF_DEATH_MAP[weapon] ?? CAUSE_OF_DEATH_MAP['other']!;
  const causeOfDeath = rng.pick(causeOptions);

  return {
    id: uuidv4(),
    name,
    age,
    occupation,
    backstory,
    relationships: {}, // populated later when suspects are generated
    causeOfDeath,
    timeOfDeath: murderTime,
    locationFound: location,
  };
}
