// ============================================
// Timeline Validator
// ============================================

import type { TimelineEvent } from '../models/Timeline';
import type { ValidationResult } from './caseValidator';

export function validateTimeline(events: TimelineEvent[], murderTime: Date): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (events.length === 0) {
    errors.push('Timeline is empty');
    return { isValid: false, errors, warnings };
  }

  // Must have at least one key event
  const keyEvents = events.filter((e) => e.isKeyEvent);
  if (keyEvents.length === 0) errors.push('No key events in timeline');

  // Murder event should be within 30 minutes of murderTime
  const murderEvents = events.filter(
    (e) => e.isKeyEvent && e.involvedIds.length >= 2
  );
  if (murderEvents.length === 0) {
    warnings.push('No event clearly marks the murder moment');
  } else {
    const closest = murderEvents.reduce((prev, curr) =>
      Math.abs(curr.timestamp.getTime() - murderTime.getTime()) <
      Math.abs(prev.timestamp.getTime() - murderTime.getTime())
        ? curr
        : prev
    );
    const diffMinutes = Math.abs(closest.timestamp.getTime() - murderTime.getTime()) / 60_000;
    if (diffMinutes > 30) {
      warnings.push(`Murder event is ${diffMinutes.toFixed(0)} minutes from stated murder time`);
    }
  }

  // Events should be in chronological order
  for (let i = 1; i < events.length; i++) {
    if ((events[i]?.timestamp.getTime() ?? 0) < (events[i - 1]?.timestamp.getTime() ?? 0)) {
      errors.push(`Timeline event at index ${i} is out of chronological order`);
      break;
    }
  }

  // Ensure discovery event exists
  const discoveryEvent = events.find((e) => e.description.toLowerCase().includes('discovered'));
  if (!discoveryEvent) {
    warnings.push('No body-discovery event found in timeline');
  }

  return { isValid: errors.length === 0, errors, warnings };
}
