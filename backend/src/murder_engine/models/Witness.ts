// ============================================
// Witness Model
// ============================================

import { v4 as uuidv4 } from 'uuid';
import type {
  Witness as IWitness,
  WitnessPersonality,
  HonestyLevel,
  WitnessRelationship,
  WitnessMemory,
  WitnessAlias,
} from '../../types/witness.types';

export type { IWitness as Witness };

export function createWitness(
  caseId: string,
  data: {
    name: string;
    age: number;
    occupation: string;
    backstory: string;
    personality: WitnessPersonality;
    honestyLevel: HonestyLevel;
    isKiller?: boolean;
    isSuspect?: boolean;
    relationships: WitnessRelationship[];
    memories: WitnessMemory[];
    alibi: WitnessAlias;
    knownFacts: string[];
    hiddenFacts: string[];
  }
): IWitness {
  const id = uuidv4();
  return {
    id,
    caseId,
    name: data.name,
    age: data.age,
    occupation: data.occupation,
    backstory: data.backstory,
    personality: data.personality,
    honestyLevel: data.honestyLevel,
    currentStress: 'calm',
    relationships: data.relationships,
    memories: data.memories,
    alibi: { ...data.alibi, witnessId: id },
    isKiller: data.isKiller ?? false,
    isSuspect: data.isSuspect ?? false,
    knownFacts: data.knownFacts,
    hiddenFacts: data.hiddenFacts,
    interrogationCount: 0,
  };
}
