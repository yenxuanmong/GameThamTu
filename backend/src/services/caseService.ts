// ============================================
// Case Service
// ============================================

import { prisma } from '../configs/database';
import { redisGet, redisSet, REDIS_KEYS } from '../configs/redis';
import { CACHE_TTL } from '../utils/constants';
import type { DifficultyLevel } from '../types/case.types';
import type { FullCase } from '../murder_engine/models/Case';
import logger from '../utils/logger';

export interface CaseListItem {
  id: string;
  title: string;
  description: string;
  difficulty: DifficultyLevel;
  status: string;
  tags: string[];
  createdAt: Date;
  matchCount: number;
}

export interface CaseDetails {
  id: string;
  title: string;
  description: string;
  difficulty: DifficultyLevel;
  status: string;
  tags: string[];
  victimName: string;
  victimAge: number;
  victimOccupation: string;
  victimBackstory: string;
  causeOfDeath: string;
  timeOfDeath: Date;
  locationFound: string;
  suspectCount: number;
  witnessCount: number;
  evidenceCount: number;
  createdAt: Date;
}

// ============================================
// List cases (public info only, no solution)
// ============================================

export async function listCases(
  page = 1,
  pageSize = 20,
  difficulty?: DifficultyLevel
): Promise<{ cases: CaseListItem[]; total: number }> {
  const where = difficulty ? { difficulty: difficulty as any, status: 'active' as any } : { status: 'active' as any };

  const [cases, total] = await prisma.$transaction([
    prisma.case.findMany({
      where,
      orderBy: { createdAt: 'desc' },
      skip: (page - 1) * pageSize,
      take: pageSize,
      select: {
        id: true,
        title: true,
        description: true,
        difficulty: true,
        status: true,
        tags: true,
        createdAt: true,
        _count: { select: { matches: true } },
      },
    }),
    prisma.case.count({ where }),
  ]);

  return {
    cases: cases.map((c) => ({
      id: c.id,
      title: c.title,
      description: c.description,
      difficulty: c.difficulty as DifficultyLevel,
      status: c.status,
      tags: c.tags,
      createdAt: c.createdAt,
      matchCount: c._count.matches,
    })),
    total,
  };}

// ============================================
// Get case details (no solution revealed)
// ============================================

export async function getCaseById(caseId: string): Promise<CaseDetails | null> {
  const dbCase = await prisma.case.findUnique({
    where: { id: caseId },
    include: {
      _count: { select: { suspects: true, witnesses: true, evidences: true } },
    },
  });

  if (!dbCase) return null;

  return {
    id: dbCase.id,
    title: dbCase.title,
    description: dbCase.description,
    difficulty: dbCase.difficulty as DifficultyLevel,
    status: dbCase.status,
    tags: dbCase.tags,
    victimName: dbCase.victimName,
    victimAge: dbCase.victimAge,
    victimOccupation: dbCase.victimOccupation,
    victimBackstory: dbCase.victimBackstory,
    causeOfDeath: dbCase.causeOfDeath,
    timeOfDeath: dbCase.timeOfDeath,
    locationFound: dbCase.locationFound,
    suspectCount: dbCase._count.suspects,
    witnessCount: dbCase._count.witnesses,
    evidenceCount: dbCase._count.evidences,
    createdAt: dbCase.createdAt,
  };
}

// ============================================
// Get full case from Redis (for active matches)
// ============================================

export async function getFullCase(caseId: string): Promise<FullCase | null> {
  const cached = await redisGet<FullCase>(REDIS_KEYS.caseCache(caseId));
  if (cached) return cached;

  logger.warn('[CaseService] Case not in cache', { caseId });
  return null;
}

// ============================================
// Get case suspects (public info — no isKiller flag)
// ============================================

export async function getCaseSuspects(caseId: string) {
  const suspects = await prisma.suspect.findMany({
    where: { caseId },
    select: {
      id: true,
      name: true,
      age: true,
      occupation: true,
      backstory: true,
      personality: true,
      alibi: true,
      relationships: true,
    },
  });
  return suspects;
}

// ============================================
// Get case witnesses (public info — no hidden facts)
// ============================================

export async function getCaseWitnesses(caseId: string) {
  const witnesses = await prisma.witness.findMany({
    where: { caseId },
    select: {
      id: true,
      name: true,
      age: true,
      occupation: true,
      backstory: true,
      personality: true,
      alibi: true,
      knownFacts: true,
      relationships: true,
    },
  });
  return witnesses;
}

// ============================================
// Get case evidence (public info)
// ============================================

export async function getCaseEvidence(caseId: string) {
  const evidence = await prisma.evidence.findMany({
    where: { caseId },
    select: {
      id: true,
      type: true,
      name: true,
      description: true,
      location: true,
      imageUrl: true,
      metadata: true,
    },
  });
  return evidence;
}

// ============================================
// Get case timeline (public events only)
// ============================================

export async function getCaseTimeline(caseId: string) {
  const events = await prisma.timelineEvent.findMany({
    where: { caseId, isPublicInfo: true },
    orderBy: { order: 'asc' },
    select: {
      id: true,
      timestamp: true,
      description: true,
      involvedIds: true,
      location: true,
      isKeyEvent: true,
      order: true,
    },
  });
  return events;
}

// ============================================
// Get case solution (only after match ends)
// ============================================

export async function getCaseSolution(caseId: string, matchId: string) {
  // Only reveal solution if the match is finished
  const match = await prisma.match.findFirst({
    where: { id: matchId, caseId, status: 'finished' },
    select: { id: true },
  });

  if (!match) return null;

  const dbCase = await prisma.case.findUnique({
    where: { id: caseId },
    select: {
      killerId: true,
      motive: true,
      weapon: true,
      murderLocation: true,
      solutionTimeline: true,
      solutionMethod: true,
      solutionNarrative: true,
      suspects: { select: { id: true, name: true } },
    },
  });

  if (!dbCase) return null;

  const killerName = dbCase.suspects.find((s) => s.id === dbCase.killerId)?.name ?? 'Unknown';

  return {
    killerId: dbCase.killerId,
    killerName,
    motive: dbCase.motive,
    weapon: dbCase.weapon,
    location: dbCase.murderLocation,
    timeline: dbCase.solutionTimeline,
    method: dbCase.solutionMethod,
    narrative: dbCase.solutionNarrative,
  };
}

// ============================================
// Get case stats
// ============================================

export async function getCaseStats(caseId: string) {
  const [matchCount, scoreData] = await prisma.$transaction([
    prisma.match.count({ where: { caseId, status: 'finished' } }),
    prisma.matchScore.aggregate({
      where: { match: { caseId } },
      _avg: { totalScore: true },
      _max: { totalScore: true },
      _count: { isCorrect: true },
    }),
  ]);

  const correctSolves = await prisma.matchScore.count({
    where: { match: { caseId }, isCorrect: true },
  });

  return {
    totalMatches: matchCount,
    avgScore: Math.round(scoreData._avg.totalScore ?? 0),
    highScore: scoreData._max.totalScore ?? 0,
    correctSolveRate:
      matchCount > 0 ? Math.round((correctSolves / matchCount) * 100) : 0,
  };
}
