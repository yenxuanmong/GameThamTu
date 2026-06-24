// ============================================
// Narrative Controller
// — manages case narrative state and pacing during a match
// ============================================

import type { FullCase } from '../../murder_engine/models/Case';
import type { Match } from '../../types/match.types';
import { buildTimelineNarrative } from '../../murder_engine/models/Timeline';

export interface NarrativeState {
  matchId: string;
  revealedTimelineEventIds: string[];
  broadcastMessages: NarrativeBroadcast[];
  solutionRevealed: boolean;
}

export interface NarrativeBroadcast {
  id: string;
  message: string;
  type: 'atmosphere' | 'timeline_update' | 'witness_update' | 'solution_reveal';
  timestamp: Date;
  targetPlayerIds?: string[];   // null = broadcast all
}

export class NarrativeController {
  private states: Map<string, NarrativeState> = new Map();

  initMatch(matchId: string): NarrativeState {
    const state: NarrativeState = {
      matchId,
      revealedTimelineEventIds: [],
      broadcastMessages: [],
      solutionRevealed: false,
    };
    this.states.set(matchId, state);
    return state;
  }

  // ============================================
  // Reveal public timeline events progressively
  // ============================================

  revealNextTimelineEvent(matchId: string, fullCase: FullCase): NarrativeBroadcast | null {
    const state = this.states.get(matchId);
    if (!state) return null;

    const publicEvents = fullCase.timeline.filter(
      (e) => e.isPublicInfo && !state.revealedTimelineEventIds.includes(e.id)
    );

    if (publicEvents.length === 0) return null;

    const next = publicEvents[0]!;
    state.revealedTimelineEventIds.push(next.id);

    const broadcast: NarrativeBroadcast = {
      id: `timeline_${next.id}`,
      message: `[New information] ${next.description}`,
      type: 'timeline_update',
      timestamp: new Date(),
    };

    state.broadcastMessages.push(broadcast);
    return broadcast;
  }

  // ============================================
  // Build solution reveal narrative (end of match)
  // ============================================

  buildSolutionReveal(fullCase: FullCase, match: Match): NarrativeBroadcast {
    const killer = fullCase.suspects.find((s) => s.isKiller)!;
    const solution = fullCase.solution;

    const keyTimeline = buildTimelineNarrative(
      fullCase.timeline.filter((e) => e.isKeyEvent)
    );

    const narrative =
      `== THE TRUTH ==\n\n` +
      `The killer was ${killer.name}, ${killer.occupation}.\n\n` +
      `Motive: ${solution.motive.replace('_', ' ').toUpperCase()}\n` +
      `Weapon: ${solution.weapon.replace('_', ' ')}\n` +
      `Location: ${solution.location.replace('_', ' ')}\n\n` +
      `How it happened:\n${solution.method}\n\n` +
      `Key timeline:\n${keyTimeline}\n\n` +
      `In ${killer.name}'s own words:\n"${solution.narrative}"`;

    const state = this.states.get(match.id);
    const broadcast: NarrativeBroadcast = {
      id: `solution_${match.id}`,
      message: narrative,
      type: 'solution_reveal',
      timestamp: new Date(),
    };

    if (state) {
      state.solutionRevealed = true;
      state.broadcastMessages.push(broadcast);
    }

    return broadcast;
  }

  // ============================================
  // Atmosphere messages (flavour)
  // ============================================

  generateAtmosphereMessage(fullCase: FullCase, timeRemainingSeconds: number): string {
    if (timeRemainingSeconds > 900) {
      return ATMOSPHERE_EARLY[Math.floor(Math.random() * ATMOSPHERE_EARLY.length)] ?? '';
    } else if (timeRemainingSeconds > 300) {
      return ATMOSPHERE_MIDGAME[Math.floor(Math.random() * ATMOSPHERE_MIDGAME.length)] ?? '';
    } else {
      return ATMOSPHERE_FINAL[Math.floor(Math.random() * ATMOSPHERE_FINAL.length)] ?? '';
    }
  }

  getState(matchId: string): NarrativeState | undefined {
    return this.states.get(matchId);
  }

  cleanupMatch(matchId: string): void {
    this.states.delete(matchId);
  }
}

// ============================================
// Atmosphere text pools
// ============================================

const ATMOSPHERE_EARLY = [
  'The house is silent save for the tick of the grandfather clock.',
  'Somewhere outside, rain begins to fall against the windows.',
  'The fire in the grate crackles. Every suspect is still in the building.',
  'A door closes quietly somewhere on the upper floor.',
];

const ATMOSPHERE_MIDGAME = [
  'The investigation is entering a critical phase. Evidence does not lie — people do.',
  'Several accounts of the evening contradict one another. The truth is in the discrepancy.',
  'The killer is still among you, composing themselves with practiced calm.',
  'Time passes. Somewhere, a clock chimes the half-hour.',
];

const ATMOSPHERE_FINAL = [
  'The moment of reckoning approaches. What do you know — and what can you prove?',
  'The killer\'s confidence has been quietly growing. Do not let them walk away.',
  'There is no more time for hesitation. The evidence must speak for itself.',
  'Every detective reaches a moment when instinct must be backed by evidence. This is yours.',
];

export const narrativeController = new NarrativeController();
