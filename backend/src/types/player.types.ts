// ============================================
// Player Types — Detective Royale
// ============================================

import type { DifficultyLevel } from './case.types';

export type PlayerStatus = 'online' | 'offline' | 'in_match' | 'in_queue' | 'banned';

export type RankTier =
  | 'rookie'
  | 'detective'
  | 'inspector'
  | 'senior_inspector'
  | 'chief_inspector'
  | 'superintendent'
  | 'commissioner'
  | 'legendary';

export type MatchResult = 'win' | 'loss' | 'draw' | 'abandoned';

// ---- Player Profile ----

export interface Player {
  id: string;
  username: string;
  email: string;
  avatarUrl?: string;
  status: PlayerStatus;
  rank: PlayerRank;
  stats: PlayerStats;
  preferences: PlayerPreferences;
  createdAt: Date;
  lastActiveAt: Date;
}

export interface PlayerRank {
  tier: RankTier;
  points: number;           // current RP
  peakPoints: number;       // all-time high RP
  season: number;
  wins: number;
  losses: number;
  winRate: number;          // 0–1
  streak: number;           // current win/loss streak (positive = wins)
}

export interface PlayerStats {
  totalMatches: number;
  totalWins: number;
  totalAccuracyScore: number;  // cumulative accuracy across all games
  avgAccuracy: number;         // 0–1000
  avgTimeToSolve: number;      // seconds
  perfectSolves: number;       // matches where all 5 fields correct
  killerIdentifiedCount: number;
  favoriteWeapon?: string;     // most accused weapon
  favoriteSuspect?: string;    // most accused suspect type
  difficultyBreakdown: Record<DifficultyLevel, { played: number; won: number }>;
}

export interface PlayerPreferences {
  preferredDifficulty: DifficultyLevel;
  preferRegion?: string;
  enableVoiceChat: boolean;
  enableNotifications: boolean;
}

// ---- Match Participation ----

export interface MatchPlayer {
  playerId: string;
  matchId: string;
  joinedAt: Date;
  isReady: boolean;
  isHost: boolean;
  investigationProgress: InvestigationProgress;
  hasSubmitted: boolean;
  finalScore?: number;
  finalRank?: number;
  rpChange?: number;
}

export interface InvestigationProgress {
  playerId: string;
  matchId: string;
  discoveredEvidenceIds: string[];
  interrogatedWitnessIds: string[];
  notebookEntries: NotebookEntry[];
  suspectList: SuspectNote[];
  timeSpent: number;           // seconds
  hintsUsed: number;
  lastUpdated: Date;
}

export interface NotebookEntry {
  id: string;
  playerId: string;
  content: string;
  relatedEvidenceId?: string;
  relatedSuspectId?: string;
  relatedWitnessId?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface SuspectNote {
  suspectId: string;
  suspicionLevel: number;   // 0–100, player's subjective rating
  notes: string;
  markedAsKiller: boolean;
}

// ---- Auth ----

export interface AuthTokenPayload {
  playerId: string;
  username: string;
  email: string;
  iat?: number;
  exp?: number;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  username: string;
  email: string;
  password: string;
}
