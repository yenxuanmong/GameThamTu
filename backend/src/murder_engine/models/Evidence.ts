// ============================================
// Evidence Model
// ============================================

import { v4 as uuidv4 } from 'uuid';
import type { Evidence as IEvidence, EvidenceType, EvidenceLocation } from '../../types/evidence.types';

export type { IEvidence as Evidence };

export function createEvidence(
  caseId: string,
  data: {
    type: EvidenceType;
    name: string;
    description: string;
    location: EvidenceLocation;
    relatesTo: string[];
    isReal: boolean;
    isFakeEvidence?: boolean;
    pointsTo?: string;
    metadata?: Record<string, unknown>;
  }
): IEvidence {
  return {
    id: uuidv4(),
    caseId,
    type: data.type,
    name: data.name,
    description: data.description,
    location: data.location,
    relatesTo: data.relatesTo,
    reliability: data.isReal ? 'confirmed' : 'suspected',
    isReal: data.isReal,
    isFakeEvidence: data.isFakeEvidence ?? false,
    pointsTo: data.pointsTo,
    metadata: data.metadata ?? {},
  };
}
