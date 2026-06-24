// ============================================
// Evidence Types — Detective Royale
// ============================================

export type EvidenceType =
  | 'physical'       // vật chứng thực tế (dao, súng, ly...)
  | 'forensic'       // bằng chứng pháp y (dấu vân tay, DNA, mẫu máu...)
  | 'document'       // tài liệu (thư, nhật ký, email, hóa đơn...)
  | 'digital'        // bằng chứng kỹ thuật số (camera, điện thoại, máy tính...)
  | 'testimonial'    // lời khai
  | 'circumstantial' // bằng chứng gián tiếp
  | 'alibi';         // bằng chứng ngoại phạm

export type EvidenceReliability = 'confirmed' | 'suspected' | 'disputed';

export type EvidenceLocation =
  | 'crime_scene'
  | 'suspect_residence'
  | 'victim_residence'
  | 'public_area'
  | 'forensic_lab'
  | 'digital';

// ---- Core Evidence ----

export interface Evidence {
  id: string;
  caseId: string;
  type: EvidenceType;
  name: string;
  description: string;
  location: EvidenceLocation;
  discoveredAt?: Date;         // set when player finds it
  relatesTo: string[];         // suspectIds or 'victim' or 'killer'
  reliability: EvidenceReliability;
  isReal: boolean;             // true = real evidence, false = fake/planted
  isFakeEvidence: boolean;     // explicitly planted to mislead
  pointsTo?: string;           // suspectId it implicates (can be misleading if fake)
  imageUrl?: string;
  metadata: Record<string, unknown>;
}

export interface PhysicalEvidence extends Evidence {
  type: 'physical';
  condition: 'pristine' | 'damaged' | 'partial' | 'trace';
  weaponType?: string;
  fingerprints?: string[];     // suspectIds with matching prints
}

export interface ForensicEvidence extends Evidence {
  type: 'forensic';
  dnaMatch?: string;           // suspectId
  bloodType?: string;
  timeOfDeathRange?: { earliest: Date; latest: Date };
  causeOfDeathClue?: string;
}

export interface DocumentEvidence extends Evidence {
  type: 'document';
  content: string;             // redacted or full text
  author?: string;
  recipient?: string;
  dateWritten?: Date;
  isRedacted: boolean;
}

export interface DigitalEvidence extends Evidence {
  type: 'digital';
  deviceType: 'camera' | 'phone' | 'computer' | 'security_system';
  timestamp?: Date;
  recordedPersonIds?: string[];  // suspectIds visible in footage
  isEncrypted: boolean;
  requiresHacking: boolean;
}

// ---- Evidence Discovery ----

export interface EvidenceDiscovery {
  evidenceId: string;
  playerId: string;
  matchId: string;
  discoveredAt: Date;
  sharedWith: string[];        // playerIds this was shared with
  notes: string;               // player's personal notes on this evidence
}

export interface EvidencePool {
  caseId: string;
  realEvidence: Evidence[];
  fakeEvidence: Evidence[];
  allEvidence: Evidence[];     // shuffled mix, players can't tell which is real
}
