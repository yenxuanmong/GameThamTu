// ============================================
// Motive Generator
// ============================================

import type { Motive } from '../../types/case.types';
import type { SeededRandom } from '../../utils/random';

export interface MotiveDetail {
  motive: Motive;
  shortLabel: string;
  description: string;          // generic description
  killerNarrative: string;      // first-person killer reasoning (used in solution reveal)
}

const MOTIVE_POOL: MotiveDetail[] = [
  {
    motive: 'greed',
    shortLabel: 'Greed',
    description: 'The killer stood to gain substantial financial benefit from the victim\'s death.',
    killerNarrative:
      'The inheritance alone was worth the risk. Years of waiting while they squandered what was rightfully mine — I simply accelerated the inevitable.',
  },
  {
    motive: 'jealousy',
    shortLabel: 'Jealousy',
    description: 'Bitter jealousy over status, possessions, or a romantic relationship drove the killer to act.',
    killerNarrative:
      'Every achievement, every accolade they received felt like a theft from me. I worked harder, deserved more — and they knew it.',
  },
  {
    motive: 'revenge',
    shortLabel: 'Revenge',
    description: 'A deep-seated grudge, rooted in past betrayal or humiliation, finally reached its boiling point.',
    killerNarrative:
      'They destroyed everything I built. My career, my family, my name — all reduced to ashes by their one selfish act. Balance had to be restored.',
  },
  {
    motive: 'blackmail',
    shortLabel: 'Blackmail',
    description: 'The victim had been extorting the killer with a dangerous secret, leaving no other way out.',
    killerNarrative:
      'The payments were endless. Each month, a new demand. I couldn\'t let them hold that over me for the rest of my life.',
  },
  {
    motive: 'inheritance',
    shortLabel: 'Inheritance',
    description: 'The killer\'s position as a primary beneficiary made the victim\'s death highly profitable.',
    killerNarrative:
      'The will was about to be changed — I had days, perhaps hours. Once it was done, everything would go to someone else. I couldn\'t allow that.',
  },
  {
    motive: 'fear',
    shortLabel: 'Fear',
    description: 'The killer acted out of desperate fear — of exposure, imprisonment, or losing everything.',
    killerNarrative:
      'They were going to talk. Once they did, my entire life would unravel. I had no choice but to silence them first.',
  },
  {
    motive: 'love',
    shortLabel: 'Crimes of Passion',
    description: 'An obsessive or scorned love turned fatal when the victim rejected or betrayed the killer.',
    killerNarrative:
      'If I couldn\'t have them, no one could. After what they did — the lies, the replacement — they left me no other option.',
  },
  {
    motive: 'power',
    shortLabel: 'Power',
    description: 'The victim stood between the killer and a position of significant power or influence.',
    killerNarrative:
      'They were the last obstacle. With them gone, the path forward was clear. Some sacrifices are simply necessary.',
  },
  {
    motive: 'rivalry',
    shortLabel: 'Rivalry',
    description: 'A long-standing professional or personal rivalry escalated to lethal consequences.',
    killerNarrative:
      'We had been competitors for decades. They cheated — always did. This time, I chose to settle it permanently.',
  },
  {
    motive: 'self_defense',
    shortLabel: 'Self-Defense Gone Too Far',
    description: 'What began as self-defense escalated beyond what was necessary, resulting in the victim\'s death.',
    killerNarrative:
      'It started as protecting myself — they threatened me first. But once it began, I couldn\'t stop. It went too far.',
  },
  {
    motive: 'ideology',
    shortLabel: 'Ideology',
    description: 'The killer acted on a deeply held belief — moral, political, or fanatical — that justified the act.',
    killerNarrative:
      'What they represented was a poison. Someone had to stop them before the damage spread further. I was the only one willing to act.',
  },
];

export function generateMotive(rng: SeededRandom): MotiveDetail {
  return rng.pick(MOTIVE_POOL);
}

export function getMotiveDetail(motive: Motive): MotiveDetail {
  const found = MOTIVE_POOL.find((m) => m.motive === motive);
  if (!found) throw new Error(`Unknown motive: ${motive}`);
  return found;
}
