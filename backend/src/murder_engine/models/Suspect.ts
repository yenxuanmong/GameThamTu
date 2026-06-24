// ============================================
// Suspect Model
// ============================================

import { v4 as uuidv4 } from 'uuid';
import type { Motive, MurderWeapon, MurderLocation } from '../../types/case.types';

export type SuspectPersonality =
  | 'arrogant'
  | 'timid'
  | 'charming'
  | 'aggressive'
  | 'calculating'
  | 'paranoid'
  | 'empathetic'
  | 'cold';

export interface SuspectRelationship {
  targetId: string;         // another suspectId or 'victim'
  type: string;             // e.g. 'friend', 'rival', 'ex_partner'
  description: string;
  isHidden: boolean;        // whether this is known publicly
}

export interface SuspectAlibi {
  description: string;
  isTrue: boolean;
  corroboratedBy?: string; // suspectId or witnessId who can verify
  evidence?: string;       // evidenceId that supports/refutes this alibi
}

export interface Suspect {
  id: string;
  caseId: string;
  name: string;
  age: number;
  occupation: string;
  backstory: string;
  personality: SuspectPersonality;
  isKiller: boolean;
  alibi: SuspectAlibi;
  motive?: Motive;                  // only the killer has a clear motive
  weapon?: MurderWeapon;            // only the killer used a weapon
  relationships: SuspectRelationship[];
  secretsKnown: string[];           // things they know that could incriminate someone
  nervousnessLevel: number;         // 0–1, affects dialogue
  orderOfCreation: number;          // for internal ordering
}

export function createSuspect(
  caseId: string,
  data: Omit<Suspect, 'id'>,
): Suspect {
  return {
    id: uuidv4(),
    ...data,
    caseId,
  };
}

export function createKiller(
  caseId: string,
  data: Omit<Suspect, 'id' | 'isKiller'>,
  motive: Motive,
  weapon: MurderWeapon,
  _location: MurderLocation,
): Suspect {
  return {
    id: uuidv4(),
    ...data,
    caseId,
    isKiller: true,
    motive,
    weapon,
    // Killers tend to be slightly more nervous but can mask it
    nervousnessLevel: Math.min(1, data.nervousnessLevel + 0.15),
  };
}
