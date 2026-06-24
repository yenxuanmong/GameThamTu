// ============================================
// Murder Engine — main public API
// ============================================

import type { DifficultyLevel, CaseGenerationConfig, ConclusionScore, PlayerConclusion } from '../../types/case.types';
import type { FullCase } from '../models/Case';
import { CaseGenerator } from './CaseGenerator';
import { ValidationEngine } from './ValidationEngine';
import { SCORE_WEIGHTS, TIME_BONUS } from '../../utils/constants';
import logger from '../../utils/logger';

const MAX_GENERATION_RETRIES = 3;

export class MurderEngine {
  private readonly generator: CaseGenerator;
  private readonly validator: ValidationEngine;

  constructor() {
    this.generator = new CaseGenerator();
    this.validator = new ValidationEngine();
  }

  // ============================================
  // Case Generation
  // ============================================

  /**
   * Generate a new, fully validated case.
   * Retries up to MAX_GENERATION_RETRIES times if validation fails.
   */
  async generateCase(
    difficulty: DifficultyLevel,
    options: {
      seed?: string;
      overrides?: Partial<CaseGenerationConfig>;
      maxRetries?: number;
    } = {}
  ): Promise<FullCase> {
    const maxRetries = options.maxRetries ?? MAX_GENERATION_RETRIES;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        const fullCase = await this.generator.generate(difficulty, options);
        const validation = this.validator.validate(fullCase);

        if (validation.isValid) {
          logger.info('[MurderEngine] Case generated and validated', {
            caseId: fullCase.id,
            attempt,
            warnings: validation.warnings.length,
          });
          return fullCase;
        }

        logger.warn(`[MurderEngine] Case validation failed (attempt ${attempt}/${maxRetries})`, {
          errors: validation.errors,
        });

        // On last attempt, throw with the validation errors
        if (attempt === maxRetries) {
          throw new Error(
            `Case generation failed after ${maxRetries} attempts:\n${validation.errors.join('\n')}`
          );
        }
      } catch (err) {
        if (attempt === maxRetries) throw err;
        logger.warn(`[MurderEngine] Generation attempt ${attempt} failed, retrying...`, { err });
      }
    }

    // Should never reach here
    throw new Error('Case generation failed unexpectedly');
  }

  /**
   * Validate an existing case (useful after manual edits or loading from DB).
   */
  validateCase(fullCase: FullCase) {
    return this.validator.validate(fullCase);
  }

  // ============================================
  // Scoring
  // ============================================

  /**
   * Score a player's conclusion against the actual solution.
   */
  scoreConclusion(
    conclusion: PlayerConclusion,
    fullCase: FullCase,
    matchDurationSeconds: number,
    timeSpentSeconds: number,
    allConclusions: PlayerConclusion[]
  ): ConclusionScore {
    const solution = fullCase.solution;

    // ---- Field scores ----
    const killerScore = conclusion.killerId === solution.killerId ? SCORE_WEIGHTS.KILLER : 0;
    const motiveScore = conclusion.motive === solution.motive ? SCORE_WEIGHTS.MOTIVE : 0;
    const weaponScore = conclusion.weapon === solution.weapon ? SCORE_WEIGHTS.WEAPON : 0;
    const locationScore = conclusion.location === solution.location ? SCORE_WEIGHTS.LOCATION : 0;

    // Timeline: partial credit based on key elements mentioned
    const timelineScore = scoreTimeline(conclusion.timeline, fullCase, SCORE_WEIGHTS.TIMELINE);

    // Narrative: partial credit based on key facts
    const narrativeScore = scoreNarrative(conclusion.narrative, fullCase, SCORE_WEIGHTS.NARRATIVE);

    // ---- Time bonus ----
    const timeFraction = timeSpentSeconds / matchDurationSeconds;
    const timeBonus =
      timeFraction <= TIME_BONUS.THRESHOLD_PERCENT
        ? Math.round(TIME_BONUS.MAX * (1 - timeFraction / TIME_BONUS.THRESHOLD_PERCENT))
        : 0;

    const totalScore = killerScore + motiveScore + weaponScore + locationScore + timelineScore + narrativeScore + timeBonus;

    const isCorrect =
      conclusion.killerId === solution.killerId &&
      conclusion.motive === solution.motive &&
      conclusion.weapon === solution.weapon &&
      conclusion.location === solution.location;

    // Determine rank (will be set properly once all scores are calculated)
    const rank = determineRank(conclusion.playerId, allConclusions, solution);

    return {
      playerId: conclusion.playerId,
      totalScore,
      breakdown: {
        killer: killerScore,
        motive: motiveScore,
        weapon: weaponScore,
        location: locationScore,
        timeline: timelineScore,
        narrative: narrativeScore,
      },
      isCorrect,
      rank,
      timeBonus,
    };
  }

  /**
   * Score all player conclusions and rank them.
   */
  scoreAllConclusions(
    conclusions: PlayerConclusion[],
    fullCase: FullCase,
    matchDurationSeconds: number,
    playerTimeMap: Map<string, number> // playerId → timeSpentSeconds
  ): ConclusionScore[] {
    const rawScores = conclusions.map((conclusion) => {
      const timeSpent = playerTimeMap.get(conclusion.playerId) ?? matchDurationSeconds;
      return this.scoreConclusion(
        conclusion,
        fullCase,
        matchDurationSeconds,
        timeSpent,
        conclusions
      );
    });

    // Sort by total score descending and assign final ranks
    rawScores.sort((a, b) => b.totalScore - a.totalScore);
    return rawScores.map((score, index) => ({ ...score, rank: index + 1 }));
  }

  // ============================================
  // Case Summary (for players — no solution)
  // ============================================

  getCasePublicInfo(fullCase: FullCase) {
    return {
      id: fullCase.id,
      title: fullCase.title,
      description: fullCase.description,
      difficulty: fullCase.difficulty,
      victim: {
        name: fullCase.victim.name,
        age: fullCase.victim.age,
        occupation: fullCase.victim.occupation,
        backstory: fullCase.victim.backstory,
        causeOfDeath: fullCase.victim.causeOfDeath,
        timeOfDeath: fullCase.victim.timeOfDeath,
        locationFound: fullCase.victim.locationFound,
      },
      suspects: fullCase.suspects.map((s) => ({
        id: s.id,
        name: s.name,
        age: s.age,
        occupation: s.occupation,
        backstory: s.backstory,
        alibi: s.alibi.description,
        relationships: s.relationships
          .filter((r) => !r.isHidden)
          .map((r) => ({ type: r.type, description: r.description })),
      })),
      witnesses: fullCase.witnesses.map((w) => ({
        id: w.id,
        name: w.name,
        age: w.age,
        occupation: w.occupation,
        backstory: w.backstory,
      })),
      publicTimeline: fullCase.timeline
        .filter((t) => t.isPublicInfo)
        .map((t) => ({
          timestamp: t.timestamp,
          description: t.description,
          location: t.location,
        })),
      evidenceCount: fullCase.evidencePool.length,
    };
  }
}

// ============================================
// Scoring helpers
// ============================================

function scoreTimeline(
  playerTimeline: string,
  fullCase: FullCase,
  maxScore: number
): number {
  if (!playerTimeline || playerTimeline.trim().length === 0) return 0;

  const solution = fullCase.solution;
  const killerName = fullCase.suspects.find((s) => s.isKiller)?.name ?? '';
  const victimName = fullCase.victim.name;

  // Key facts that should appear in a good timeline
  const keyFacts = [
    killerName.split(' ')[0]?.toLowerCase() ?? '',
    victimName.split(' ')[0]?.toLowerCase() ?? '',
    solution.weapon.replace('_', ' ').toLowerCase(),
    solution.location.replace('_', ' ').toLowerCase(),
    solution.motive.replace('_', ' ').toLowerCase(),
  ].filter(Boolean);

  const normalised = playerTimeline.toLowerCase();
  const matchedFacts = keyFacts.filter((fact) => normalised.includes(fact));
  const ratio = matchedFacts.length / keyFacts.length;

  return Math.round(maxScore * ratio);
}

function scoreNarrative(
  playerNarrative: string,
  fullCase: FullCase,
  maxScore: number
): number {
  if (!playerNarrative || playerNarrative.trim().length < 20) return 0;

  const solution = fullCase.solution;
  const killerName = fullCase.suspects.find((s) => s.isKiller)?.name ?? '';

  const keyTerms = [
    killerName.split(' ')[0]?.toLowerCase() ?? '',
    solution.motive.replace('_', ' ').toLowerCase(),
    solution.weapon.replace('_', ' ').toLowerCase(),
  ].filter(Boolean);

  const normalised = playerNarrative.toLowerCase();
  const matchedTerms = keyTerms.filter((term) => normalised.includes(term));
  const ratio = matchedTerms.length / keyTerms.length;

  // Bonus for narrative length (shows effort)
  const lengthBonus = Math.min(0.2, playerNarrative.length / 1000);

  return Math.round(maxScore * Math.min(1, ratio + lengthBonus));
}

function determineRank(
  playerId: string,
  allConclusions: PlayerConclusion[],
  solution: FullCase['solution']
): number {
  // Simplified rank based on how many correct answers they have vs others
  const scores = allConclusions.map((c) => ({
    playerId: c.playerId,
    correct:
      (c.killerId === solution.killerId ? 3 : 0) +
      (c.motive === solution.motive ? 2 : 0) +
      (c.weapon === solution.weapon ? 1 : 0),
  }));

  scores.sort((a, b) => b.correct - a.correct);
  const rank = scores.findIndex((s) => s.playerId === playerId) + 1;
  return rank > 0 ? rank : allConclusions.length;
}
