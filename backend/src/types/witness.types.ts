// ============================================
// Witness / NPC Types — Detective Royale
// ============================================

export type WitnessPersonality =
  | 'cooperative'    // sẵn sàng chia sẻ, dễ nói chuyện
  | 'nervous'        // lo lắng, hay mâu thuẫn
  | 'hostile'        // chống đối, cần thuyết phục
  | 'deceptive'      // cố tình nói dối
  | 'confused'       // không nhớ rõ, dễ bị ám thị
  | 'protective'     // bảo vệ người khác
  | 'opportunistic'; // lợi dụng tình huống để trục lợi

export type HonestyLevel = 'always_honest' | 'mostly_honest' | 'mixed' | 'mostly_lying' | 'always_lying';

export type StressLevel = 'calm' | 'mild' | 'moderate' | 'high' | 'extreme';

export type RelationshipType =
  | 'friend'
  | 'enemy'
  | 'colleague'
  | 'family'
  | 'romantic_partner'
  | 'ex_partner'
  | 'neighbor'
  | 'stranger'
  | 'employer'
  | 'employee'
  | 'rival';

// ---- Core Witness ----

export interface WitnessRelationship {
  targetId: string;              // suspectId or 'victim'
  type: RelationshipType;
  description: string;
  isHidden: boolean;             // NPC may deny this relationship initially
  loyaltyScore: number;          // 0–1, how much they protect this person
}

export interface WitnessMemory {
  eventId: string;
  witnessId: string;
  description: string;           // what they remember
  accuracy: number;              // 0–1, how accurate this memory is
  isWillingToShare: boolean;     // false = need to convince them
  requiresStressBelow: StressLevel; // only shares if stress is manageable
  revealCondition?: string;      // e.g., "player mentions the knife"
}

export interface WitnessAlias {
  witnessId: string;
  alibi: string;                 // what they claim they were doing
  isTrue: boolean;               // whether alibi is actually true
  alibiCorroboratedBy?: string;  // suspectId who can confirm/deny
}

export interface Witness {
  id: string;
  caseId: string;
  name: string;
  age: number;
  occupation: string;
  backstory: string;
  personality: WitnessPersonality;
  honestyLevel: HonestyLevel;
  currentStress: StressLevel;
  relationships: WitnessRelationship[];
  memories: WitnessMemory[];
  alibi: WitnessAlias;
  isKiller: boolean;             // true = this witness is the killer
  isSuspect: boolean;
  knownFacts: string[];          // things they know about the case
  hiddenFacts: string[];         // things they know but won't reveal easily
  interrogationCount: number;    // how many times they've been questioned
  lastInterrogatedBy?: string;   // playerId
}

// ---- NPC Dialogue State ----

export interface NPCDialogueState {
  witnessId: string;
  matchId: string;
  playerId: string;
  sessionStart: Date;
  messageCount: number;
  stressLevel: StressLevel;
  revealedFacts: string[];
  contradictionCount: number;
  trustScore: number;            // 0–1, how much NPC trusts this player
  conversationHistory: DialogueTurn[];
}

export interface DialogueTurn {
  role: 'player' | 'npc';
  content: string;
  timestamp: Date;
  revealedMemoryId?: string;     // if this turn revealed a memory
  stressChange?: number;         // -1 to +1
}

// ---- Witness Behavior Config (per difficulty) ----

export interface WitnessBehaviorConfig {
  baseHonestyRate: number;       // 0–1
  stressThreshold: number;       // above this, NPC becomes less coherent
  maxInterrogationsBeforeHostile: number;
  aliasRevealThreshold: number;  // trust score needed to reveal alibi inconsistencies
  contradictionProbability: number; // prob of introducing contradictions under stress
}
