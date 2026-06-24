// ============================================
// Evidence Service
// ============================================

import { prisma } from '../configs/database';
import logger from '../utils/logger';

export interface EvidenceDiscoveryRecord {
  evidenceId: string;
  evidenceName: string;
  evidenceType: string;
  description: string;
  location: string;
  discoveredAt: Date;
  notes: string;
  sharedWith: string[];
}

export interface PlayerEvidenceProgress {
  matchId: string;
  playerId: string;
  discovered: EvidenceDiscoveryRecord[];
  totalInCase: number;
  discoveryRate: number; // 0–1
}

// ============================================
// Get evidence discovered by a player in a match
// ============================================

export async function getPlayerEvidence(
  matchId: string,
  playerId: string
): Promise<PlayerEvidenceProgress> {
  const discoveries = await prisma.evidenceDiscovery.findMany({
    where: { matchId, playerId },
    include: {
      evidence: {
        select: {
          name: true,
          type: true,
          description: true,
          location: true,
        },
      },
    },
    orderBy: { discoveredAt: 'asc' },
  });

  const match = await prisma.match.findUnique({
    where: { id: matchId },
    select: { caseId: true },
  });

  const totalInCase = match
    ? await prisma.evidence.count({ where: { caseId: match.caseId } })
    : 0;

  const discovered: EvidenceDiscoveryRecord[] = discoveries.map((d) => ({
    evidenceId: d.evidenceId,
    evidenceName: d.evidence.name,
    evidenceType: d.evidence.type,
    description: d.evidence.description,
    location: d.evidence.location,
    discoveredAt: d.discoveredAt,
    notes: d.notes,
    sharedWith: d.sharedWith,
  }));

  return {
    matchId,
    playerId,
    discovered,
    totalInCase,
    discoveryRate: totalInCase > 0 ? discovered.length / totalInCase : 0,
  };
}

// ============================================
// Get a single evidence item (visible to match participants)
// ============================================

export async function getEvidenceById(evidenceId: string, matchId: string, playerId: string) {
  // Ensure the player is in this match
  const matchPlayer = await prisma.matchPlayer.findUnique({
    where: { matchId_playerId: { matchId, playerId } },
    select: { id: true },
  });

  if (!matchPlayer) return null;

  const evidence = await prisma.evidence.findUnique({
    where: { id: evidenceId },
    select: {
      id: true,
      type: true,
      name: true,
      description: true,
      location: true,
      imageUrl: true,
      metadata: true,
      caseId: true,
    },
  });

  if (!evidence) return null;

  // Verify evidence belongs to the match's case
  const match = await prisma.match.findUnique({
    where: { id: matchId },
    select: { caseId: true },
  });

  if (!match || evidence.caseId !== match.caseId) return null;

  // Get discovery info for this player
  const discovery = await prisma.evidenceDiscovery.findUnique({
    where: { evidenceId_playerId_matchId: { evidenceId, playerId, matchId } },
    select: { notes: true, sharedWith: true, discoveredAt: true },
  });

  return {
    ...evidence,
    discoveredAt: discovery?.discoveredAt ?? null,
    notes: discovery?.notes ?? '',
    sharedWith: discovery?.sharedWith ?? [],
  };
}

// ============================================
// Add/update notes on evidence
// ============================================

export async function updateEvidenceNotes(
  evidenceId: string,
  matchId: string,
  playerId: string,
  notes: string
): Promise<boolean> {
  const discovery = await prisma.evidenceDiscovery.findUnique({
    where: { evidenceId_playerId_matchId: { evidenceId, playerId, matchId } },
  });

  if (!discovery) return false;

  await prisma.evidenceDiscovery.update({
    where: { evidenceId_playerId_matchId: { evidenceId, playerId, matchId } },
    data: { notes },
  });

  logger.debug('[EvidenceService] Notes updated', { evidenceId, playerId });
  return true;
}

// ============================================
// Share evidence with another player in the same match
// ============================================

export async function shareEvidence(
  evidenceId: string,
  matchId: string,
  fromPlayerId: string,
  toPlayerId: string
): Promise<{ success: boolean; error?: string }> {
  // Verify both players are in the match
  const [fromPlayer, toPlayer] = await prisma.$transaction([
    prisma.matchPlayer.findUnique({
      where: { matchId_playerId: { matchId, playerId: fromPlayerId } },
      select: { id: true },
    }),
    prisma.matchPlayer.findUnique({
      where: { matchId_playerId: { matchId, playerId: toPlayerId } },
      select: { id: true },
    }),
  ]);

  if (!fromPlayer) return { success: false, error: 'You are not in this match' };
  if (!toPlayer) return { success: false, error: 'Target player is not in this match' };

  const discovery = await prisma.evidenceDiscovery.findUnique({
    where: { evidenceId_playerId_matchId: { evidenceId, playerId: fromPlayerId, matchId } },
  });

  if (!discovery) return { success: false, error: 'Evidence not yet discovered by you' };

  // Add toPlayerId to sharedWith list
  const updatedShared = Array.from(new Set([...discovery.sharedWith, toPlayerId]));
  await prisma.evidenceDiscovery.update({
    where: { evidenceId_playerId_matchId: { evidenceId, playerId: fromPlayerId, matchId } },
    data: { sharedWith: updatedShared },
  });

  // Also create discovery entry for the recipient
  await prisma.evidenceDiscovery.upsert({
    where: { evidenceId_playerId_matchId: { evidenceId, playerId: toPlayerId, matchId } },
    update: {},
    create: {
      evidenceId,
      playerId: toPlayerId,
      matchId,
      notes: '',
      sharedWith: [],
    },
  });

  logger.debug('[EvidenceService] Evidence shared', { evidenceId, from: fromPlayerId, to: toPlayerId });
  return { success: true };
}

// ============================================
// Get notebook entries for a player in a match
// ============================================

export async function getNotebook(matchId: string, playerId: string) {
  // Find the matchPlayer record to get matchPlayerId
  const matchPlayer = await prisma.matchPlayer.findUnique({
    where: { matchId_playerId: { matchId, playerId } },
    select: { id: true },
  });

  if (!matchPlayer) return [];

  const entries = await prisma.notebookEntry.findMany({
    where: { matchPlayerId: matchPlayer.id, playerId },
    orderBy: { createdAt: 'asc' },
    select: {
      id: true,
      content: true,
      relatedEvidenceId: true,
      createdAt: true,
      updatedAt: true,
    },
  });

  return entries;
}

// ============================================
// Delete a notebook entry
// ============================================

export async function deleteNotebookEntry(
  entryId: string,
  playerId: string
): Promise<boolean> {
  const entry = await prisma.notebookEntry.findUnique({
    where: { id: entryId },
    select: { playerId: true },
  });

  if (!entry || entry.playerId !== playerId) return false;

  await prisma.notebookEntry.delete({ where: { id: entryId } });
  return true;
}
