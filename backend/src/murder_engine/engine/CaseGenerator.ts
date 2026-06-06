// ============================================
// Case Generator — orchestrates all generators
// ============================================

import type { CaseGenerationConfig, DifficultyLevel } from '../../types/case.types';
import type { FullCase } from '../models/Case';
import type { Suspect } from '../models/Suspect';
import { DIFFICULTY_CONFIGS } from '../../types/case.types';
import { SeededRandom, generateSeed } from '../../utils/random';
import logger from '../../utils/logger';

// Generators
import { generateVictim } from '../generators/victimGenerator';
import { generateMotive } from '../generators/motiveGenerator';
import { generateKiller } from '../generators/killerGenerator';
import { generateSuspect } from '../generators/suspectGenerator';
import { generateWitnesses } from '../generators/witnessGenerator';
import { generateTimeline } from '../generators/timelineGenerator';
import { generateRealEvidence } from '../generators/evidenceGenerator';
import { generateFakeEvidence } from '../generators/fakeEvidenceGenerator';
import { generateDocumentEvidence } from '../generators/documentGenerator';
import { generateCameraEvidence } from '../generators/cameraGenerator';
import { createCase } from '../models/Case';

// Murder weapon / location pools
import type { MurderWeapon, MurderLocation } from '../../types/case.types';

const WEAPONS: MurderWeapon[] = [
  'knife', 'gun', 'poison', 'blunt_object', 'strangulation',
  'drowning', 'fire', 'fall', 'explosion', 'other',
];

const LOCATIONS: MurderLocation[] = [
  'living_room', 'kitchen', 'bedroom', 'garden', 'library',
  'office', 'basement', 'dining_room', 'hallway', 'cellar', 'attic',
];

export class CaseGenerator {
  private buildConfig(
    difficulty: DifficultyLevel,
    overrides: Partial<CaseGenerationConfig> = {}
  ): CaseGenerationConfig {
    const preset = DIFFICULTY_CONFIGS[difficulty];
    const rngForConfig = new SeededRandom(Date.now());

    return {
      difficulty,
      numSuspects: rngForConfig.nextInt(...preset.numSuspects),
      numWitnesses: rngForConfig.nextInt(...preset.numWitnesses),
      numRealEvidence: rngForConfig.nextInt(...preset.numRealEvidence),
      numFakeEvidence: rngForConfig.nextInt(...preset.numFakeEvidence),
      includeRedHerrings: true,
      includeAlibi: true,
      timelineComplexity: preset.timelineComplexity,
      ...overrides,
    };
  }

  async generate(
    difficulty: DifficultyLevel,
    options: {
      seed?: string;
      overrides?: Partial<CaseGenerationConfig>;
    } = {}
  ): Promise<FullCase> {
    const seed = options.seed ?? generateSeed();
    const config = this.buildConfig(difficulty, options.overrides);
    const rng = new SeededRandom(seed);

    logger.debug(`[CaseGenerator] Generating case`, { difficulty, seed, config });

    // ---- 1. Decide weapon / location / murder time ----
    const weapon = config.forceWeapon ?? rng.pick(WEAPONS);
    const murderLocation = config.forceLocation ?? rng.pick(LOCATIONS);
    const murderTime = generateMurderTime(rng);

    // ---- 2. Generate victim ----
    const victim = generateVictim('__temp__', rng, weapon, murderLocation, murderTime);

    // ---- 3. Generate motive ----
    const motiveDetail = config.forceMotive
      ? { motive: config.forceMotive, shortLabel: config.forceMotive, description: '', killerNarrative: '' }
      : generateMotive(rng);

    // ---- 4. Generate killer ----
    const killer = generateKiller({
      caseId: '__temp__',
      motive: motiveDetail.motive,
      weapon,
      location: murderLocation,
      victimId: victim.id,
      victimName: victim.name,
      orderOfCreation: 0,
      rng,
    });

    // ---- 5. Generate innocent suspects ----
    const allSuspectNames = [killer.name];
    const innocentSuspects: Suspect[] = [];

    for (let i = 1; i < config.numSuspects; i++) {
      const suspect = generateSuspect({
        caseId: '__temp__',
        victimId: victim.id,
        victimName: victim.name,
        existingSuspectNames: allSuspectNames,
        orderOfCreation: i,
        rng,
      });
      innocentSuspects.push(suspect);
      allSuspectNames.push(suspect.name);
    }

    // Shuffle killer into random position
    const allSuspects: Suspect[] = rng.shuffle([killer, ...innocentSuspects]);

    // ---- 6. Generate timeline ----
    const timeline = generateTimeline({
      caseId: '__temp__',
      victim,
      killer,
      suspects: allSuspects,
      murderLocation,
      murderTime,
      rng,
      complexity: config.timelineComplexity,
    });

    // ---- 7. Generate witnesses ----
    const witnessNames = [...allSuspectNames, victim.name];
    const witnesses = generateWitnesses({
      caseId: '__temp__',
      killer,
      suspects: allSuspects,
      victimId: victim.id,
      victimName: victim.name,
      timeline,
      numWitnesses: config.numWitnesses,
      difficulty,
      existingNames: witnessNames,
      rng,
    });

    // ---- 8. Generate evidence ----
    const realEvidence = generateRealEvidence({
      caseId: '__temp__',
      killer,
      suspects: allSuspects,
      victimId: victim.id,
      weapon,
      murderLocation,
      timeline,
      numRealEvidence: config.numRealEvidence,
      rng,
    });

    const fakeEvidence = generateFakeEvidence({
      caseId: '__temp__',
      innocentSuspects,
      numFakeEvidence: config.numFakeEvidence,
      rng,
    });

    const documentEvidence = generateDocumentEvidence({
      caseId: '__temp__',
      victim,
      killer,
      suspects: allSuspects,
      motive: motiveDetail.motive,
      rng,
      includeRedHerring: config.includeRedHerrings,
    });

    const cameraEvidence = generateCameraEvidence({
      caseId: '__temp__',
      victim,
      killer,
      suspects: allSuspects,
      murderLocation,
      murderTime,
      rng,
    });

    // Merge and shuffle all evidence
    const evidencePool = rng.shuffle([
      ...realEvidence,
      ...fakeEvidence,
      ...documentEvidence,
      ...cameraEvidence,
    ]);

    // ---- 9. Build solution ----
    const solution = {
      killerId: killer.id,
      motive: motiveDetail.motive,
      weapon,
      location: murderLocation,
      timeline: buildSolutionTimeline(timeline, killer, victim.name),
      method: `${killer.name} used ${weapon.replace('_', ' ')} to kill ${victim.name} in the ${murderLocation.replace('_', ' ')}. ${motiveDetail.description}`,
      narrative: buildSolutionNarrative(killer, victim.name, motiveDetail.killerNarrative, weapon, murderLocation, murderTime),
    };

    // Populate victim relationships from suspects
    victim.relationships = {};
    for (const suspect of allSuspects) {
      const rel = suspect.relationships.find((r) => r.targetId === victim.id);
      if (rel) {
        victim.relationships[suspect.id] = `${rel.type}: ${rel.description}`;
      }
    }

    // ---- 10. Assemble FullCase (assign real caseId) ----
    const fullCase = createCase(victim, solution, config, seed);

    // Assign real caseId to all entities
    const caseId = fullCase.id;
    fullCase.suspects = allSuspects.map((s) => ({ ...s, caseId }));
    fullCase.witnesses = witnesses.map((w) => ({ ...w, caseId }));
    fullCase.evidencePool = evidencePool.map((e) => ({ ...e, caseId }));
    fullCase.timeline = timeline.map((t) => ({ ...t, caseId }));

    logger.info(`[CaseGenerator] Case generated`, {
      caseId,
      title: fullCase.title,
      killer: killer.name,
      motive: motiveDetail.motive,
      weapon,
      location: murderLocation,
      suspects: allSuspects.length,
      witnesses: witnesses.length,
      evidence: evidencePool.length,
    });

    return fullCase;
  }
}

// ---- Helpers ----

function generateMurderTime(rng: SeededRandom): Date {
  const now = new Date();
  // Set to yesterday evening for narrative purposes
  now.setDate(now.getDate() - 1);
  now.setHours(rng.nextInt(20, 23), rng.nextInt(0, 59), 0, 0);
  return now;
}

function buildSolutionTimeline(
  events: ReturnType<typeof generateTimeline>,
  killer: Suspect,
  victimName: string
): string {
  return events
    .filter((e) => e.isKeyEvent || e.involvedIds.includes(killer.id))
    .map((e) => {
      const time = e.timestamp.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
      return `[${time}] ${e.description}`;
    })
    .join('\n');
}

function buildSolutionNarrative(
  killer: Suspect,
  victimName: string,
  killerNarrative: string,
  weapon: MurderWeapon,
  location: MurderLocation,
  murderTime: Date
): string {
  const time = murderTime.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  const weaponLabel = weapon.replace('_', ' ');
  const locationLabel = location.replace('_', ' ');

  return (
    `At ${time}, ${killer.name} — ${killer.occupation} — committed the murder ` +
    `in the ${locationLabel} using ${weaponLabel}.\n\n` +
    `In ${killer.name}'s own words: "${killerNarrative}"\n\n` +
    `${killer.name}'s stated alibi — "${killer.alibi.description}" — was false.`
  );
}
