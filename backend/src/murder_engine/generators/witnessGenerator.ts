// ============================================
// Witness Generator
// ============================================

import type { Witness } from '../../types/witness.types';
import type { WitnessPersonality, HonestyLevel, WitnessMemory, WitnessRelationship } from '../../types/witness.types';
import type { Suspect } from '../models/Suspect';
import type { TimelineEvent } from '../models/Timeline';
import type { DifficultyLevel } from '../../types/case.types';
import type { SeededRandom } from '../../utils/random';
import { DIFFICULTY_CONFIGS } from '../../types/case.types';
import { createWitness } from '../models/Witness';

const FIRST_NAMES = [
  'Alice', 'Thomas', 'Grace', 'William', 'Clara', 'James',
  'Beatrice', 'Arthur', 'Muriel', 'Charles', 'Ethel', 'Henry',
  'Agnes', 'Robert', 'Lillian', 'George', 'Mary', 'Alfred',
];

const LAST_NAMES = [
  'Potts', 'Heathcote', 'Birch', 'Stanton', 'Redwood', 'Crane',
  'Sayers', 'Fitch', 'Alderton', 'Drury', 'Merritt', 'Tully',
];

const OCCUPATIONS = [
  'butler', 'parlour maid', 'gardener', 'cook', 'lady\'s maid',
  'footman', 'stable hand', 'housekeeper', 'governess', 'valet',
  'night watchman', 'chauffeur', 'scullery maid', 'groundskeeper',
];

const PERSONALITIES: WitnessPersonality[] = [
  'cooperative', 'nervous', 'hostile', 'deceptive', 'confused', 'protective', 'opportunistic',
];

const HONESTY_BY_DIFFICULTY: Record<DifficultyLevel, HonestyLevel[]> = {
  easy:      ['always_honest', 'always_honest', 'mostly_honest'],
  medium:    ['always_honest', 'mostly_honest', 'mostly_honest', 'mixed'],
  hard:      ['mostly_honest', 'mixed', 'mixed', 'mostly_lying'],
  expert:    ['mixed', 'mostly_lying', 'mostly_lying', 'always_lying'],
  nightmare: ['mostly_lying', 'always_lying', 'always_lying', 'mixed'],
};

export interface WitnessGenerationInput {
  caseId: string;
  killer: Suspect;
  suspects: Suspect[];
  victimId: string;
  victimName: string;
  timeline: TimelineEvent[];
  numWitnesses: number;
  difficulty: DifficultyLevel;
  existingNames: string[];
  rng: SeededRandom;
}

export function generateWitnesses(input: WitnessGenerationInput): Witness[] {
  const { caseId, killer, suspects, victimId, victimName, timeline, numWitnesses, difficulty, existingNames, rng } = input;

  const witnesses: Witness[] = [];
  const usedNames = new Set(existingNames);
  const honestyPool = HONESTY_BY_DIFFICULTY[difficulty];

  for (let i = 0; i < numWitnesses; i++) {
    // At least one witness always knows something directly incriminating
    const isKeyWitness = i === 0;

    let name: string;
    let attempts = 0;
    do {
      const firstName = rng.pick(FIRST_NAMES);
      const lastName = rng.pick(LAST_NAMES);
      name = `${firstName} ${lastName}`;
      attempts++;
    } while (usedNames.has(name) && attempts < 50);
    usedNames.add(name);

    const occupation = rng.pick(OCCUPATIONS);
    const personality = rng.pick(PERSONALITIES);
    const honestyLevel = rng.pick(honestyPool);
    const age = rng.nextInt(18, 70);

    const { memories, knownFacts, hiddenFacts } = generateWitnessMemories(
      { id: name, witnessId: name, alibi: '', isTrue: false },
      killer,
      suspects,
      victimId,
      victimName,
      timeline,
      honestyLevel,
      isKeyWitness,
      rng
    );

    const relationships = buildWitnessRelationships(
      killer,
      suspects,
      victimId,
      victimName,
      rng
    );

    const alibiIsTrue = rng.nextBool(0.8); // witnesses mostly have true alibis
    const alibi = buildWitnessAlibi(name, alibiIsTrue, rng);

    const backstory = buildWitnessBackstory(name, occupation, victimName, personality, rng);

    witnesses.push(
      createWitness(caseId, {
        name,
        age,
        occupation,
        backstory,
        personality,
        honestyLevel,
        isKiller: false,
        isSuspect: false,
        relationships,
        memories,
        alibi,
        knownFacts,
        hiddenFacts,
      })
    );
  }

  return witnesses;
}

// ---- Memory Generation ----

interface MemoryOutput {
  memories: WitnessMemory[];
  knownFacts: string[];
  hiddenFacts: string[];
}

function generateWitnessMemories(
  self: { id: string; witnessId: string; alibi: string; isTrue: boolean },
  killer: Suspect,
  suspects: Suspect[],
  victimId: string,
  victimName: string,
  timeline: TimelineEvent[],
  honestyLevel: HonestyLevel,
  isKeyWitness: boolean,
  rng: SeededRandom
): MemoryOutput {
  const memories: WitnessMemory[] = [];
  const knownFacts: string[] = [];
  const hiddenFacts: string[] = [];

  const accuracyByHonesty: Record<HonestyLevel, number> = {
    always_honest:  0.95,
    mostly_honest:  0.75,
    mixed:          0.5,
    mostly_lying:   0.3,
    always_lying:   0.1,
  };

  const accuracy = accuracyByHonesty[honestyLevel];
  const isWillingToShare = honestyLevel !== 'always_lying' && honestyLevel !== 'mostly_lying';

  if (isKeyWitness) {
    // This witness saw something critical
    const criticalFact = `Saw ${killer.name} near the ${killer.relationships[0]?.description ?? 'scene'} around the time of the murder`;
    hiddenFacts.push(criticalFact);

    memories.push({
      eventId: `mem_critical_${self.witnessId}`,
      witnessId: self.witnessId,
      description: criticalFact,
      accuracy: Math.max(0.6, accuracy),
      isWillingToShare: isWillingToShare,
      requiresStressBelow: 'high',
      revealCondition: `player mentions ${killer.name}`,
    });
  }

  // Regular memories from timeline
  const publicTimeline = timeline.filter((e) => e.isPublicInfo).slice(0, 3);
  for (const event of rng.shuffle(publicTimeline).slice(0, rng.nextInt(1, 3))) {
    const fact = event.description;
    const isHidden = rng.nextBool(1 - accuracy);

    if (isHidden) {
      hiddenFacts.push(fact);
    } else {
      knownFacts.push(fact);
    }

    memories.push({
      eventId: `mem_timeline_${event.id}`,
      witnessId: self.witnessId,
      description: fact,
      accuracy,
      isWillingToShare: !isHidden || isWillingToShare,
      requiresStressBelow: isHidden ? 'moderate' : 'extreme',
    });
  }

  // Add a rumour about one of the suspects
  if (suspects.length > 0) {
    const subject = rng.pick(suspects);
    const rumours = [
      `Overheard ${subject.name} arguing with ${victimName} two days before the murder`,
      `Saw ${subject.name} coming down the stairs at an unusual hour`,
      `${subject.name} seemed unusually agitated the evening of the incident`,
      `Found a note in ${subject.name}'s handwriting near the victim's study`,
    ];
    const rumour = rng.pick(rumours);
    const isThisHidden = rng.nextBool(0.5);

    if (isThisHidden) {
      hiddenFacts.push(rumour);
    } else {
      knownFacts.push(rumour);
    }

    memories.push({
      eventId: `mem_rumour_${self.witnessId}`,
      witnessId: self.witnessId,
      description: rumour,
      accuracy: accuracy * (rng.nextBool(0.3) ? 0.5 : 1), // sometimes rumours are distorted
      isWillingToShare: !isThisHidden,
      requiresStressBelow: 'high',
    });
  }

  // Ignore victimId parameter to satisfy lint (used implicitly via victimName context)
  void victimId;

  return { memories, knownFacts, hiddenFacts };
}

// ---- Relationships ----

function buildWitnessRelationships(
  killer: Suspect,
  suspects: Suspect[],
  victimId: string,
  victimName: string,
  rng: SeededRandom
): WitnessRelationship[] {
  const relationships: WitnessRelationship[] = [];

  // Relationship with victim
  relationships.push({
    targetId: victimId,
    type: rng.pick(['employer', 'employer', 'neighbor', 'family', 'colleague']),
    description: `Works in the household of ${victimName}`,
    isHidden: false,
    loyaltyScore: rng.nextFloat(0.3, 0.9),
  });

  // Possibly knows the killer
  if (rng.nextBool(0.5)) {
    relationships.push({
      targetId: killer.id,
      type: rng.pick(['colleague', 'rival', 'neighbor', 'friend']),
      description: `Acquainted with ${killer.name}`,
      isHidden: rng.nextBool(0.3),
      loyaltyScore: rng.nextFloat(0.1, 0.6),
    });
  }

  // Possibly knows one other suspect
  if (suspects.length > 1 && rng.nextBool(0.4)) {
    const otherSuspect = rng.pick(suspects.filter((s) => !s.isKiller));
    if (otherSuspect) {
      relationships.push({
        targetId: otherSuspect.id,
        type: rng.pick(['stranger', 'colleague', 'neighbor']),
        description: `Knows ${otherSuspect.name} by sight`,
        isHidden: false,
        loyaltyScore: rng.nextFloat(0.1, 0.4),
      });
    }
  }

  return relationships;
}

// ---- Alibi ----

function buildWitnessAlibi(
  name: string,
  isTrue: boolean,
  rng: SeededRandom
): { witnessId: string; alibi: string; isTrue: boolean; alibiCorroboratedBy?: string } {
  const trueAlibis = [
    'Was serving dinner in the kitchen all evening.',
    'Was polishing silver in the butler\'s pantry.',
    'Was in bed with a cold — the other maid can confirm.',
    'Was at the local post office, then returned directly.',
  ];
  const falseAlibis = [
    'Claims to have been asleep the whole evening.',
    'Says they were outside walking the dog, but the dog was found inside.',
    'Claims they heard nothing unusual, though the study is adjacent to their room.',
  ];

  return {
    witnessId: name,
    alibi: rng.pick(isTrue ? trueAlibis : falseAlibis),
    isTrue,
  };
}

// ---- Backstory ----

function buildWitnessBackstory(
  name: string,
  occupation: string,
  victimName: string,
  personality: WitnessPersonality,
  rng: SeededRandom
): string {
  const personalityTraits: Record<WitnessPersonality, string> = {
    cooperative: 'known for being forthcoming and reliable',
    nervous:     'visibly on edge since the incident and prone to contradicting themselves',
    hostile:     'guarded and resistant to questioning',
    deceptive:   'outwardly calm, though inconsistencies in their account have been noted',
    confused:    'struggling to recall the evening clearly',
    protective:  'loyal to the household and reluctant to implicate anyone',
    opportunistic: 'quick to volunteer information, though their motivations are unclear',
  };

  const trait = personalityTraits[personality];
  const tenures = ['two years', 'six months', 'nearly a decade', 'three years', 'just over a year'];
  const tenure = rng.pick(tenures);

  return `${name} has worked as a ${occupation} in the household for ${tenure}. ` +
    `They are ${trait}. Their account of the evening of the murder remains one of the key areas of interest.`;
}
