// ============================================
// Random Utilities — Seeded RNG for reproducible cases
// ============================================

/**
 * Seeded pseudo-random number generator (Mulberry32).
 * Using a seed ensures the same case can be reproduced.
 */
export class SeededRandom {
  private seed: number;

  constructor(seed: string | number) {
    this.seed = typeof seed === 'string' ? this.hashString(seed) : seed;
  }

  private hashString(str: string): number {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = (hash << 5) - hash + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return Math.abs(hash);
  }

  /** Returns a float between 0 (inclusive) and 1 (exclusive) */
  next(): number {
    this.seed |= 0;
    this.seed = (this.seed + 0x6d2b79f5) | 0;
    let t = Math.imul(this.seed ^ (this.seed >>> 15), 1 | this.seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }

  /** Returns an integer between min (inclusive) and max (inclusive) */
  nextInt(min: number, max: number): number {
    return Math.floor(this.next() * (max - min + 1)) + min;
  }

  /** Returns true with the given probability (0–1) */
  nextBool(probability = 0.5): boolean {
    return this.next() < probability;
  }

  /** Returns a random element from an array */
  pick<T>(array: T[]): T {
    if (array.length === 0) throw new Error('Cannot pick from empty array');
    const index = this.nextInt(0, array.length - 1);
    return array[index] as T;
  }

  /** Returns n unique random elements from an array */
  pickN<T>(array: T[], n: number): T[] {
    if (n > array.length) throw new Error(`Cannot pick ${n} items from array of ${array.length}`);
    const copy = [...array];
    const result: T[] = [];
    for (let i = 0; i < n; i++) {
      const index = this.nextInt(0, copy.length - 1);
      result.push(copy[index] as T);
      copy.splice(index, 1);
    }
    return result;
  }

  /** Shuffles an array in-place (Fisher-Yates) */
  shuffle<T>(array: T[]): T[] {
    const arr = [...array];
    for (let i = arr.length - 1; i > 0; i--) {
      const j = this.nextInt(0, i);
      [arr[i], arr[j]] = [arr[j] as T, arr[i] as T];
    }
    return arr;
  }

  /** Returns a float between min and max */
  nextFloat(min: number, max: number): number {
    return this.next() * (max - min) + min;
  }
}

/** Generate a unique seed string for a new case */
export function generateSeed(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
}
