// ============================================
// Stress System
// — models how witness stress changes during interrogation
// ============================================

import type { StressLevel, WitnessPersonality } from '../../types/witness.types';

export interface StressUpdate {
  newLevel: StressLevel;
  delta: number;       // -1 to +1, actual change in stress score
  reason: string;
}

const STRESS_LEVELS: StressLevel[] = ['calm', 'mild', 'moderate', 'high', 'extreme'];

export function stressToScore(level: StressLevel): number {
  return STRESS_LEVELS.indexOf(level) / (STRESS_LEVELS.length - 1); // 0.0–1.0
}

export function scoreToStress(score: number): StressLevel {
  const clamped = Math.max(0, Math.min(1, score));
  const index = Math.round(clamped * (STRESS_LEVELS.length - 1));
  return STRESS_LEVELS[index] ?? 'calm';
}

// ============================================
// Triggers that raise stress
// ============================================

const ACCUSATION_KEYWORDS = [
  'you did it', 'you killed', 'you were there', 'you\'re lying',
  'you lied', 'we know you', 'you\'re the killer', 'did you murder',
  'your fingerprints', 'you were seen', 'caught you',
];

const PRESSURE_KEYWORDS = [
  'tell me the truth', 'stop lying', 'admit it', 'we have evidence',
  'i know you know', 'don\'t pretend', 'why are you hiding',
  'you\'re not being honest', 'your alibi is false', 'prove it',
];

const CALMING_KEYWORDS = [
  'i understand', 'take your time', 'i believe you', 'you\'re safe',
  'no one is accusing you', 'just want to understand', 'thank you',
  'that\'s helpful', 'i appreciate', 'you\'ve been very helpful',
];

// ============================================
// Calculate stress change from player message
// ============================================

export function calculateStressChange(
  playerMessage: string,
  currentScore: number,
  personality: WitnessPersonality,
  isKiller: boolean
): StressUpdate {
  const msg = playerMessage.toLowerCase();

  let delta = 0;
  let reason = 'no_change';

  // Direct accusations
  if (ACCUSATION_KEYWORDS.some((k) => msg.includes(k))) {
    delta += 0.2;
    reason = 'accused';
  }

  // Pressure tactics
  if (PRESSURE_KEYWORDS.some((k) => msg.includes(k))) {
    delta += 0.1;
    reason = 'pressured';
  }

  // Calming language
  if (CALMING_KEYWORDS.some((k) => msg.includes(k))) {
    delta -= 0.15;
    reason = 'calmed';
  }

  // Personality modifiers
  const personalityMods: Record<WitnessPersonality, number> = {
    cooperative:    -0.05,  // stress dissipates more easily
    nervous:        +0.05,  // always slightly more stressed
    hostile:        -0.05,  // accusations don't affect them as much (they're already defensive)
    deceptive:      +0.0,   // neutral
    confused:       +0.03,  // confusion compounds stress
    protective:     -0.02,  // slightly resilient
    opportunistic:  -0.08,  // stress rarely affects them
  };

  delta += personalityMods[personality] ?? 0;

  // Killers are more stressed by accurate accusations
  if (isKiller && ACCUSATION_KEYWORDS.some((k) => msg.includes(k))) {
    delta += 0.1;
  }

  // Natural decay (interrogation proceeds, stress settles slightly over time)
  if (delta === 0) {
    delta = -0.02; // slight natural decay per message
    reason = 'natural_decay';
  }

  const newScore = Math.max(0, Math.min(1, currentScore + delta));
  return {
    newLevel: scoreToStress(newScore),
    delta,
    reason,
  };
}

// ============================================
// Stress affects response quality
// ============================================

export interface StressEffect {
  respondsCoherently: boolean;   // false = may give confused/fragmented answer
  willContradictSelf: boolean;   // true = may slip up
  refusesToAnswer: boolean;      // true = goes silent or demands to stop
}

export function getStressEffects(
  score: number,
  personality: WitnessPersonality
): StressEffect {
  const isHighStress = score > 0.7;
  const isExtremeStress = score > 0.9;

  return {
    respondsCoherently: !isExtremeStress,
    willContradictSelf: isHighStress && Math.random() < 0.3,
    refusesToAnswer:
      isExtremeStress &&
      (personality === 'hostile' || personality === 'deceptive') &&
      Math.random() < 0.4,
  };
}
