// ============================================
// Timeline Model
// ============================================

import { v4 as uuidv4 } from 'uuid';

export interface TimelineEvent {
  id: string;
  caseId: string;
  timestamp: Date;
  description: string;
  involvedIds: string[];   // suspectId | 'victim' | 'killer'
  location: string;
  isKeyEvent: boolean;     // part of the murder sequence
  isPublicInfo: boolean;   // false = only discoverable via clues
  evidenceId?: string;
  order: number;
}

export function createTimelineEvent(
  caseId: string,
  data: Omit<TimelineEvent, 'id' | 'caseId'>
): TimelineEvent {
  return {
    id: uuidv4(),
    caseId,
    ...data,
  };
}

export function sortTimeline(events: TimelineEvent[]): TimelineEvent[] {
  return [...events].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
}

export function buildTimelineNarrative(events: TimelineEvent[]): string {
  const sorted = sortTimeline(events);
  return sorted
    .filter((e) => e.isPublicInfo || e.isKeyEvent)
    .map((e) => {
      const time = e.timestamp.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
      });
      return `[${time}] ${e.description}`;
    })
    .join('\n');
}
