// ============================================
// Auth Utilities — JWT helpers
// ============================================

import jwt from 'jsonwebtoken';
import bcrypt from 'bcryptjs';
import type { AuthTokenPayload, AuthTokens } from '../types/player.types';

const JWT_SECRET = process.env['JWT_SECRET'] ?? 'change_this_in_production';
const JWT_REFRESH_SECRET = process.env['JWT_REFRESH_SECRET'] ?? 'change_this_refresh_secret';
const JWT_EXPIRES_IN = process.env['JWT_EXPIRES_IN'] ?? '7d';
const JWT_REFRESH_EXPIRES_IN = process.env['JWT_REFRESH_EXPIRES_IN'] ?? '30d';
const BCRYPT_ROUNDS = 12;

// ============================================
// JWT
// ============================================

export function signAccessToken(payload: Omit<AuthTokenPayload, 'iat' | 'exp'>): string {
  return jwt.sign(payload, JWT_SECRET, { expiresIn: JWT_EXPIRES_IN } as jwt.SignOptions);
}

export function signRefreshToken(payload: Omit<AuthTokenPayload, 'iat' | 'exp'>): string {
  return jwt.sign(payload, JWT_REFRESH_SECRET, { expiresIn: JWT_REFRESH_EXPIRES_IN } as jwt.SignOptions);
}

export function verifyAccessToken(token: string): AuthTokenPayload {
  return jwt.verify(token, JWT_SECRET) as AuthTokenPayload;
}

export function verifyRefreshToken(token: string): AuthTokenPayload {
  return jwt.verify(token, JWT_REFRESH_SECRET) as AuthTokenPayload;
}

export function generateTokens(payload: Omit<AuthTokenPayload, 'iat' | 'exp'>): AuthTokens {
  const accessToken = signAccessToken(payload);
  const refreshToken = signRefreshToken(payload);
  const decoded = jwt.decode(accessToken) as AuthTokenPayload;

  return {
    accessToken,
    refreshToken,
    expiresIn: (decoded.exp ?? 0) - Math.floor(Date.now() / 1000),
  };
}

// ============================================
// Password
// ============================================

export async function hashPassword(plain: string): Promise<string> {
  return bcrypt.hash(plain, BCRYPT_ROUNDS);
}

export async function verifyPassword(plain: string, hash: string): Promise<boolean> {
  return bcrypt.compare(plain, hash);
}
