// ============================================
// Match / Room Types — Detective Royale
// ============================================

import type { DifficultyLevel, PlayerConclusion, ConclusionScore } from './case.types';

export type MatchStatus =
  | 'waiting'        // chờ đủ người
  | 'starting'       // countdown
  | 'active'         // đang chơi
  | 'conclusion'     // mọi người đã submit, chờ reveal
  | 'finished'       // kết quả đã reveal
  | 'cancelled'      // bị hủy (người chơi rời phòng, lỗi...)
  | 'abandoned';     // không ai submit

export type RoomVisibility = 'public' | 'private';

export type GamePhase =
  | 'investigation'   // giai đoạn điều tra (gần hết thời gian)
  | 'final_minutes'   // 5 phút cuối
  | 'submission'      // mở form submit
  | 'reveal';         // reveal kết quả

// ---- Match ----

export interface Match {
  id: string;
  roomId: string;
  caseId: string;
  difficulty: DifficultyLevel;
  status: MatchStatus;
  phase: GamePhase;
  playerIds: string[];
  maxPlayers: number;
  startedAt?: Date;
  endedAt?: Date;
  durationSeconds: number;
  timeRemainingSeconds: number;
  conclusions: PlayerConclusion[];
  scores: ConclusionScore[];
  winnerId?: string;
  createdAt: Date;
}

// ---- Room ----

export interface Room {
  id: string;
  name: string;
  hostId: string;
  visibility: RoomVisibility;
  password?: string;           // for private rooms
  difficulty: DifficultyLevel;
  maxPlayers: number;
  currentPlayers: number;
  playerIds: string[];
  isInMatch: boolean;
  currentMatchId?: string;
  createdAt: Date;
  settings: RoomSettings;
}

export interface RoomSettings {
  difficulty: DifficultyLevel;
  maxPlayers: number;
  allowSpectators: boolean;
  enableVoiceChat: boolean;
  autoStart: boolean;          // auto start when room is full
  startCountdownSeconds: number;
}

// ---- Matchmaking ----

export interface QueueEntry {
  playerId: string;
  difficulty: DifficultyLevel;
  region?: string;
  rankPoints: number;
  queuedAt: Date;
  estimatedWaitSeconds?: number;
}

export interface MatchmakingResult {
  success: boolean;
  matchId?: string;
  roomId?: string;
  players?: string[];
  error?: string;
}

// ---- Socket Events ----

export type ServerToClientEvents = {
  // Room events
  'room:joined': (data: { room: Room; playerId: string }) => void;
  'room:left': (data: { roomId: string; playerId: string }) => void;
  'room:updated': (data: { room: Room }) => void;
  'room:countdown': (data: { seconds: number }) => void;

  // Match events
  'match:started': (data: { matchId: string; caseId: string }) => void;
  'match:phase_changed': (data: { phase: GamePhase; timeRemaining: number }) => void;
  'match:timer': (data: { timeRemaining: number }) => void;
  'match:player_submitted': (data: { playerId: string; submittedAt: Date }) => void;
  'match:ended': (data: { scores: ConclusionScore[]; winnerId: string }) => void;

  // Investigation events
  'investigation:evidence_found': (data: { evidenceId: string; playerId: string }) => void;
  'investigation:witness_available': (data: { witnessId: string }) => void;
  'investigation:hint': (data: { hint: string; hintsRemaining: number }) => void;

  // NPC events
  'npc:response': (data: { witnessId: string; message: string; stressLevel: string }) => void;

  // System events
  'error': (data: { code: string; message: string }) => void;
  'notification': (data: { type: 'info' | 'warning' | 'success'; message: string }) => void;
};

export type ClientToServerEvents = {
  // Room events
  'room:join': (data: { roomId: string; password?: string }) => void;
  'room:leave': (data: { roomId: string }) => void;
  'room:ready': (data: { roomId: string }) => void;
  'room:create': (data: { settings: RoomSettings }) => void;

  // Match events
  'match:submit_conclusion': (data: { matchId: string; conclusion: PlayerConclusion }) => void;
  'match:request_hint': (data: { matchId: string }) => void;

  // Investigation events
  'investigation:examine_evidence': (data: { matchId: string; evidenceId: string }) => void;
  'investigation:interrogate_witness': (data: { matchId: string; witnessId: string; message: string }) => void;
  'investigation:add_note': (data: { matchId: string; note: string; relatedId?: string }) => void;

  // Matchmaking events
  'queue:join': (data: { difficulty: DifficultyLevel; region?: string }) => void;
  'queue:leave': () => void;
};
