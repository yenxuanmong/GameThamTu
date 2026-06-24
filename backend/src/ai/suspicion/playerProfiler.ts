// ============================================
// Player Profiler
// — builds a profile of player investigation style
// ============================================

import type { InvestigationProgress } from '../../types/player.types';
import type { PlayerStats } from '../../types/player.types';

export type InvestigationStyle =
  | 'evidence_focused'    // examines lots of physical evidence
  | 'social_engineer'     // talks to many witnesses
  | 'deductive'           // uses notes heavily, methodical
  | 'opportunistic'       // jumps around, no clear strategy
  | 'speed_runner';       // submits early, low accuracy but fast

export interface PlayerProfile {
  playerId: string;
  style: InvestigationStyle;
  strengths: string[];
  weaknesses: string[];
  adaptationHints: string[];    // hints for adaptive difficulty system
}

// ============================================
// Build profile from current match progress
// ============================================

export function buildMatchProfile(
  playerId: string,
  progress: InvestigationProgress,
  totalEvidence: number,
  totalWitnesses: number,
  matchDurationSeconds: number
): PlayerProfile {
  const evidenceRatio = progress.discoveredEvidenceIds.length / Math.max(1, totalEvidence);
  const witnessRatio = progress.interrogatedWitnessIds.length / Math.max(1, totalWitnesses);
  const noteCount = progress.notebookEntries.length;
  const timeRatio = progress.timeSpent / Math.max(1, matchDurationSeconds);

  // Style detection
  let style: InvestigationStyle;

  if (timeRatio < 0.4 && progress.timeSpent < matchDurationSeconds * 0.4) {
    style = 'speed_runner';
  } else if (evidenceRatio > 0.6 && witnessRatio < 0.3) {
    style = 'evidence_focused';
  } else if (witnessRatio > 0.6 && evidenceRatio < 0.3) {
    style = 'social_engineer';
  } else if (noteCount > 5 && evidenceRatio > 0.4 && witnessRatio > 0.4) {
    style = 'deductive';
  } else {
    style = 'opportunistic';
  }

  const strengths = getStrengths(style, progress);
  const weaknesses = getWeaknesses(style, evidenceRatio, witnessRatio);
  const adaptationHints = getAdaptationHints(style);

  return { playerId, style, strengths, weaknesses, adaptationHints };
}

// ============================================
// Combine match profile with historical stats
// ============================================

export function buildFullProfile(
  playerId: string,
  stats: PlayerStats,
  recentStyle: InvestigationStyle
): PlayerProfile {
  const avgAccuracy = stats.avgAccuracy;
  const winRate = stats.totalMatches > 0 ? stats.totalWins / stats.totalMatches : 0;

  const strengths: string[] = [];
  const weaknesses: string[] = [];

  if (winRate > 0.5) strengths.push('Strong overall win rate');
  if (avgAccuracy > 700) strengths.push('High accuracy — rarely guesses wrong');
  if (stats.perfectSolves > 0) strengths.push(`${stats.perfectSolves} perfect solve(s)`);
  if (stats.killerIdentifiedCount > stats.totalMatches * 0.6) {
    strengths.push('Reliably identifies the killer');
  }

  if (avgAccuracy < 400) weaknesses.push('Low accuracy — often submits incomplete conclusions');
  if (winRate < 0.3) weaknesses.push('Struggling to win matches consistently');
  if (stats.avgTimeToSolve < 300) weaknesses.push('May be submitting too quickly without full evidence');

  return {
    playerId,
    style: recentStyle,
    strengths,
    weaknesses,
    adaptationHints: getAdaptationHints(recentStyle),
  };
}

// ============================================
// Helpers
// ============================================

function getStrengths(style: InvestigationStyle, progress: InvestigationProgress): string[] {
  const strengths: string[] = [];
  switch (style) {
    case 'evidence_focused':
      strengths.push('Thorough evidence collection');
      break;
    case 'social_engineer':
      strengths.push('Strong NPC relationship building');
      break;
    case 'deductive':
      strengths.push('Methodical, note-taking approach');
      break;
    case 'speed_runner':
      strengths.push('Fast decision-making');
      break;
    case 'opportunistic':
      strengths.push('Flexible investigation approach');
      break;
  }
  if (progress.hintsUsed === 0) strengths.push('Solved without hints');
  return strengths;
}

function getWeaknesses(
  style: InvestigationStyle,
  evidenceRatio: number,
  witnessRatio: number
): string[] {
  const weaknesses: string[] = [];
  if (evidenceRatio < 0.4) weaknesses.push('Not enough evidence examined');
  if (witnessRatio < 0.3) weaknesses.push('Few witnesses interviewed');
  if (style === 'speed_runner') weaknesses.push('Rushed submissions reduce accuracy');
  if (style === 'opportunistic') weaknesses.push('Lacks systematic strategy');
  return weaknesses;
}

function getAdaptationHints(style: InvestigationStyle): string[] {
  const hints: Record<InvestigationStyle, string[]> = {
    evidence_focused: [
      'Consider interviewing more witnesses — they often reveal what physical evidence cannot',
    ],
    social_engineer: [
      'Physical evidence is the foundation — make sure to examine the scene thoroughly',
    ],
    deductive: [
      'Well-rounded approach — continue building on this strategy',
    ],
    opportunistic: [
      'Try to establish a systematic approach: evidence first, then witnesses, then notes',
    ],
    speed_runner: [
      'Spending more time increases accuracy significantly — a higher score is worth the extra minutes',
    ],
  };
  return hints[style] ?? [];
}
