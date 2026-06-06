// ============================================
// Case Types — Detective Royale
// ============================================

export type DifficultyLevel = 'easy' | 'medium' | 'hard' | 'expert' | 'nightmare';

export type CaseStatus = 'generating' | 'active' | 'completed' | 'archived';

export type MurderWeapon =
  | 'knife'
  | 'gun'
  | 'poison'
  | 'blunt_object'
  | 'strangulation'
  | 'drowning'
  | 'fire'
  | 'explosion'
  | 'fall'
  | 'other';

export type MurderLocation =
  | 'living_room'
  | 'kitchen'
  | 'bedroom'
  | 'bathroom'
  | 'garden'
  | 'garage'
  | 'basement'
  | 'attic'
  | 'office'
  | 'library'
  | 'dining_room'
  | 'hallway'
  | 'cellar'
  | 'rooftop'
  | 'other';

export type Motive =
  | 'jealousy'
  | 'greed'
  | 'revenge'
  | 'love'
  | 'fear'
  | 'power'
  | 'blackmail'
  | 'inheritance'
  | 'rivalry'
  | 'self_defense'
  | 'ideology'
  | 'other';

// ---- Core Case ----

export interface CaseVictim {
  id: string;
  name: string;
  age: number;
  occupation: string;
  backstory: string;
  relationships: Record<string, string>; // suspectId → relationship description
  causeOfDeath: string;
  timeOfDeath: Date;
  locationFound: MurderLocation;
}

export interface CaseSolution {
  killerId: string;
  motive: Motive;
  weapon: MurderWeapon;
  location: MurderLocation;
  timeline: string;           // human-readable summary
  method: string;             // detailed description of how the murder happened
  narrative: string;          // full narrative of events
}

export interface GeneratedCase {
  id: string;
  title: string;
  description: string;
  difficulty: DifficultyLevel;
  status: CaseStatus;
  victim: CaseVictim;
  solution: CaseSolution;     // hidden from players until game ends
  createdAt: Date;
  seed: string;               // for reproducibility
  tags: string[];
}

// ---- Player Submission ----

export interface PlayerConclusion {
  playerId: string;
  matchId: string;
  caseId: string;
  submittedAt: Date;
  killerId: string;
  motive: Motive;
  weapon: MurderWeapon;
  location: MurderLocation;
  timeline: string;
  narrative: string;
}

export interface ConclusionScore {
  playerId: string;
  totalScore: number;          // 0–1000
  breakdown: {
    killer: number;            // 0–300
    motive: number;            // 0–200
    weapon: number;            // 0–150
    location: number;          // 0–100
    timeline: number;          // 0–150
    narrative: number;         // 0–100 (AI-evaluated)
  };
  isCorrect: boolean;          // all core fields correct
  rank: number;                // 1–4 among players
  timeBonus: number;           // bonus for fast submission
}

// ---- Case Generation Config ----

export interface CaseGenerationConfig {
  difficulty: DifficultyLevel;
  seed?: string;
  forceWeapon?: MurderWeapon;
  forceMotive?: Motive;
  forceLocation?: MurderLocation;
  numSuspects: number;         // 3–8
  numWitnesses: number;        // 2–6
  numRealEvidence: number;     // 5–15
  numFakeEvidence: number;     // 2–8
  includeRedHerrings: boolean;
  includeAlibi: boolean;
  timelineComplexity: 'simple' | 'moderate' | 'complex';
}

export interface DifficultyConfig {
  level: DifficultyLevel;
  numSuspects: [number, number];         // [min, max]
  numWitnesses: [number, number];
  numRealEvidence: [number, number];
  numFakeEvidence: [number, number];
  npcHonestyRate: number;                // 0–1, probability NPC tells truth
  redHerringCount: [number, number];
  alibiComplexity: 'simple' | 'moderate' | 'complex';
  timelineComplexity: 'simple' | 'moderate' | 'complex';
  matchDurationSeconds: number;
}

export const DIFFICULTY_CONFIGS: Record<DifficultyLevel, DifficultyConfig> = {
  easy: {
    level: 'easy',
    numSuspects: [3, 4],
    numWitnesses: [2, 3],
    numRealEvidence: [6, 8],
    numFakeEvidence: [1, 2],
    npcHonestyRate: 0.9,
    redHerringCount: [1, 2],
    alibiComplexity: 'simple',
    timelineComplexity: 'simple',
    matchDurationSeconds: 1800,
  },
  medium: {
    level: 'medium',
    numSuspects: [4, 5],
    numWitnesses: [3, 4],
    numRealEvidence: [7, 10],
    numFakeEvidence: [2, 4],
    npcHonestyRate: 0.75,
    redHerringCount: [2, 3],
    alibiComplexity: 'moderate',
    timelineComplexity: 'moderate',
    matchDurationSeconds: 1500,
  },
  hard: {
    level: 'hard',
    numSuspects: [5, 6],
    numWitnesses: [3, 5],
    numRealEvidence: [8, 12],
    numFakeEvidence: [3, 5],
    npcHonestyRate: 0.6,
    redHerringCount: [3, 4],
    alibiComplexity: 'moderate',
    timelineComplexity: 'complex',
    matchDurationSeconds: 1200,
  },
  expert: {
    level: 'expert',
    numSuspects: [6, 7],
    numWitnesses: [4, 6],
    numRealEvidence: [10, 14],
    numFakeEvidence: [4, 7],
    npcHonestyRate: 0.45,
    redHerringCount: [4, 6],
    alibiComplexity: 'complex',
    timelineComplexity: 'complex',
    matchDurationSeconds: 1000,
  },
  nightmare: {
    level: 'nightmare',
    numSuspects: [7, 8],
    numWitnesses: [5, 6],
    numRealEvidence: [12, 15],
    numFakeEvidence: [6, 8],
    npcHonestyRate: 0.3,
    redHerringCount: [5, 8],
    alibiComplexity: 'complex',
    timelineComplexity: 'complex',
    matchDurationSeconds: 900,
  },
};
