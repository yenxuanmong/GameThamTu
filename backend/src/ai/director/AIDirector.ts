// ============================================
// AI Director — orchestrates all AI systems during a match
// ============================================

import type { FullCase } from '../../murder_engine/models/Case';
import type { Match } from '../../types/match.types';
import type { InvestigationProgress } from '../../types/player.types';
import type { DifficultyLevel } from '../../types/case.types';
import { computeSuspicionMap, assessInvestigationAccuracy } from '../suspicion/suspicionAI';
import { computeAdaptiveModifiers } from '../difficulty/adaptiveDifficulty';
import { buildMatchProfile } from '../suspicion/playerProfiler';
import type { PlayerStats } from '../../types/player.types';
import logger from '../../utils/logger';

export interface DirectorState {
  matchId: string;
  phase: 'investigation' | 'final_minutes' | 'reveal';
  playerStates: Map<string, PlayerDirectorState>;
  globalEvents: DirectorEvent[];
}

export interface PlayerDirectorState {
  playerId: string;
  suspicionMap: ReturnType<typeof computeSuspicionMap>;
  isOnRightTrack: boolean;
  interventionsSent: number;
  lastInterventionAt: number;
}

export interface DirectorEvent {
  type: 'hint_nudge' | 'witness_highlight' | 'evidence_spotlight' | 'time_warning';
  targetPlayerId?: string;       // null = broadcast to all
  payload: Record<string, unknown>;
  triggeredAt: Date;
}

export class AIDirector {
  private states: Map<string, DirectorState> = new Map();

  // ============================================
  // Initialise director for a new match
  // ============================================

  initMatch(match: Match, fullCase: FullCase): DirectorState {
    const state: DirectorState = {
      matchId: match.id,
      phase: 'investigation',
      playerStates: new Map(
        match.playerIds.map((id) => [
          id,
          {
            playerId: id,
            suspicionMap: {},
            isOnRightTrack: false,
            interventionsSent: 0,
            lastInterventionAt: 0,
          },
        ])
      ),
      globalEvents: [],
    };

    this.states.set(match.id, state);
    logger.debug('[AIDirector] Match initialised', { matchId: match.id });
    return state;
  }

  // ============================================
  // Update director state for one player's progress
  // ============================================

  updatePlayerState(
    matchId: string,
    playerId: string,
    fullCase: FullCase,
    progress: InvestigationProgress,
    stats: PlayerStats,
    difficulty: DifficultyLevel,
    matchDurationSeconds: number
  ): { events: DirectorEvent[] } {
    const state = this.states.get(matchId);
    if (!state) return { events: [] };

    const playerState = state.playerStates.get(playerId);
    if (!playerState) return { events: [] };

    // Update suspicion map
    const suspicionMap = computeSuspicionMap(fullCase, progress);
    const accuracy = assessInvestigationAccuracy(suspicionMap, fullCase);
    const profile = buildMatchProfile(
      playerId,
      progress,
      fullCase.evidencePool.length,
      fullCase.witnesses.length,
      matchDurationSeconds
    );
    const modifiers = computeAdaptiveModifiers(stats, difficulty);

    playerState.suspicionMap = suspicionMap;
    playerState.isOnRightTrack = accuracy.isOnRightTrack;

    const events: DirectorEvent[] = [];

    // ---- Intervention: player is off-track and hasn't had a recent nudge ----
    const now = Date.now();
    const timeSinceLastIntervention = now - playerState.lastInterventionAt;
    const interventionCooldown = 120_000; // 2 minutes

    if (
      !accuracy.isOnRightTrack &&
      playerState.interventionsSent < 3 &&
      timeSinceLastIntervention > interventionCooldown
    ) {
      events.push({
        type: 'hint_nudge',
        targetPlayerId: playerId,
        payload: {
          message: accuracy.suggestion ?? 'There may be evidence you have not yet examined.',
          hintsRemaining: Math.max(0, 3 - progress.hintsUsed),
        },
        triggeredAt: new Date(),
      });
      playerState.interventionsSent++;
      playerState.lastInterventionAt = now;
    }

    // ---- Intervention: player hasn't talked to any witnesses ----
    if (
      progress.interrogatedWitnessIds.length === 0 &&
      progress.timeSpent > 300 &&
      !events.some((e) => e.type === 'witness_highlight')
    ) {
      events.push({
        type: 'witness_highlight',
        targetPlayerId: playerId,
        payload: {
          message: 'The witnesses are available for questioning.',
          suggestionStyle: profile.style,
        },
        triggeredAt: new Date(),
      });
    }

    // ---- Global: adaptive modifiers for this player ----
    void modifiers; // modifiers used externally by case generator on next match

    state.globalEvents.push(...events);
    return { events };
  }

  // ============================================
  // Phase transition
  // ============================================

  transitionPhase(
    matchId: string,
    newPhase: DirectorState['phase']
  ): DirectorEvent[] {
    const state = this.states.get(matchId);
    if (!state) return [];

    state.phase = newPhase;
    const events: DirectorEvent[] = [];

    if (newPhase === 'final_minutes') {
      events.push({
        type: 'time_warning',
        payload: { message: 'Five minutes remaining. Prepare your conclusions.' },
        triggeredAt: new Date(),
      });
    }

    state.globalEvents.push(...events);
    logger.debug('[AIDirector] Phase transition', { matchId, phase: newPhase });
    return events;
  }

  // ============================================
  // Get current state
  // ============================================

  getState(matchId: string): DirectorState | undefined {
    return this.states.get(matchId);
  }

  cleanupMatch(matchId: string): void {
    this.states.delete(matchId);
  }
}

// Singleton
export const aiDirector = new AIDirector();
