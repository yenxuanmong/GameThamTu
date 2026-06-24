// ============================================
// Case Model
// ============================================

import { v4 as uuidv4 } from 'uuid';
import type {
  GeneratedCase,
  CaseVictim,
  CaseSolution,
  DifficultyLevel,
  CaseGenerationConfig,
} from '../../types/case.types';
import type { Suspect } from './Suspect';
import type { Witness } from './Witness';
import type { Evidence } from './Evidence';
import type { TimelineEvent } from './Timeline';

export interface FullCase extends GeneratedCase {
  suspects: Suspect[];
  witnesses: Witness[];
  evidencePool: Evidence[];       // shuffled mix of real + fake
  timeline: TimelineEvent[];
  config: CaseGenerationConfig;
}

export function createCase(
  victim: CaseVictim,
  solution: CaseSolution,
  config: CaseGenerationConfig,
  seed: string
): FullCase {
  const id = uuidv4();

  const title = generateCaseTitle(victim, solution);
  const description = generateCaseDescription(victim);

  return {
    id,
    title,
    description,
    difficulty: config.difficulty,
    status: 'active',
    victim,
    solution,
    createdAt: new Date(),
    seed,
    tags: generateTags(config.difficulty, solution),
    suspects: [],
    witnesses: [],
    evidencePool: [],
    timeline: [],
    config,
  };
}

function generateCaseTitle(victim: CaseVictim, solution: CaseSolution): string {
  const locationLabels: Record<string, string> = {
    living_room: 'the Living Room',
    kitchen: 'the Kitchen',
    bedroom: 'the Bedroom',
    garden: 'the Garden',
    library: 'the Library',
    office: 'the Office',
    basement: 'the Basement',
    rooftop: 'the Rooftop',
    dining_room: 'the Dining Room',
    garage: 'the Garage',
    hallway: 'the Hallway',
    cellar: 'the Cellar',
    attic: 'the Attic',
    bathroom: 'the Bathroom',
    other: 'an Unknown Location',
  };
  const locationLabel = locationLabels[solution.location] ?? solution.location;
  return `The ${victim.occupation} Found Dead in ${locationLabel}`;
}

function generateCaseDescription(victim: CaseVictim): string {
  return (
    `${victim.name}, a ${victim.age}-year-old ${victim.occupation}, ` +
    `was found dead at the scene. The circumstances are suspicious ` +
    `and investigators have been called in to uncover the truth.`
  );
}

function generateTags(difficulty: DifficultyLevel, solution: CaseSolution): string[] {
  return [difficulty, solution.motive, solution.weapon, solution.location];
}
