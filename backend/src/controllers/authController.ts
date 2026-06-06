// ============================================
// Auth Controller
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { body, validationResult } from 'express-validator';
import { prisma } from '../configs/database';
import {
  hashPassword,
  verifyPassword,
  generateTokens,
  verifyRefreshToken,
} from '../utils/auth';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';
import logger from '../utils/logger';
import { v4 as uuidv4 } from 'uuid';

// ============================================
// Validation rules
// ============================================

export const registerValidation = [
  body('username')
    .trim()
    .isLength({ min: 3, max: 30 })
    .matches(/^[a-zA-Z0-9_]+$/)
    .withMessage('Username must be 3-30 alphanumeric characters'),
  body('email').isEmail().normalizeEmail().withMessage('Invalid email address'),
  body('password')
    .isLength({ min: 8 })
    .matches(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/)
    .withMessage('Password must be 8+ chars with upper, lower, and number'),
];

export const loginValidation = [
  body('email').isEmail().normalizeEmail(),
  body('password').notEmpty(),
];

// ============================================
// Register
// ============================================

export async function register(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const { username, email, password } = req.body as {
      username: string;
      email: string;
      password: string;
    };

    // Check duplicate
    const existing = await prisma.player.findFirst({
      where: { OR: [{ email }, { username }] },
      select: { id: true, email: true, username: true },
    });

    if (existing) {
      const field = existing.email === email ? 'email' : 'username';
      throw new AppError(409, `This ${field} is already taken`, 'DUPLICATE');
    }

    const passwordHash = await hashPassword(password);

    const player = await prisma.player.create({
      data: {
        id: uuidv4(),
        username,
        email,
        passwordHash,
        status: 'offline',
        rank: {
          create: {
            tier: 'rookie',
            points: 0,
            peakPoints: 0,
            season: 1,
            wins: 0,
            losses: 0,
            streak: 0,
          },
        },
        stats: {
          create: {
            totalMatches: 0,
            totalWins: 0,
            totalAccuracyScore: 0,
            avgAccuracy: 0,
            avgTimeToSolve: 0,
            perfectSolves: 0,
            killerIdentifiedCount: 0,
          },
        },
        preferences: {
          create: {
            preferredDifficulty: 'medium',
            enableVoiceChat: false,
            enableNotifications: true,
          },
        },
      },
      select: { id: true, username: true, email: true, createdAt: true },
    });

    const tokens = generateTokens({
      playerId: player.id,
      username: player.username,
      email: player.email,
    });

    logger.info('[AuthController] Player registered', { playerId: player.id, username });

    res.status(201).json({
      player: {
        id: player.id,
        username: player.username,
        email: player.email,
        createdAt: player.createdAt,
      },
      ...tokens,
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Login
// ============================================

export async function login(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, 'Invalid credentials', 'VALIDATION_ERROR');
    }

    const { email, password } = req.body as { email: string; password: string };

    const player = await prisma.player.findUnique({
      where: { email },
      select: {
        id: true,
        username: true,
        email: true,
        passwordHash: true,
        status: true,
        avatarUrl: true,
      },
    });

    if (!player) {
      throw new AppError(401, 'Invalid email or password', 'INVALID_CREDENTIALS');
    }

    if (player.status === 'banned') {
      throw new AppError(403, 'This account has been banned', 'BANNED');
    }

    const valid = await verifyPassword(password, player.passwordHash);
    if (!valid) {
      throw new AppError(401, 'Invalid email or password', 'INVALID_CREDENTIALS');
    }

    // Update last active
    await prisma.player.update({
      where: { id: player.id },
      data: { status: 'online', lastActiveAt: new Date() },
    });

    const tokens = generateTokens({
      playerId: player.id,
      username: player.username,
      email: player.email,
    });

    logger.info('[AuthController] Player logged in', { playerId: player.id });

    res.json({
      player: {
        id: player.id,
        username: player.username,
        email: player.email,
        avatarUrl: player.avatarUrl,
        status: 'online',
      },
      ...tokens,
    });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Refresh token
// ============================================

export async function refreshToken(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { refreshToken: token } = req.body as { refreshToken: string };
    if (!token) {
      throw new AppError(400, 'Refresh token required', 'MISSING_TOKEN');
    }

    let payload;
    try {
      payload = verifyRefreshToken(token);
    } catch {
      throw new AppError(401, 'Invalid or expired refresh token', 'INVALID_TOKEN');
    }

    const player = await prisma.player.findUnique({
      where: { id: payload.playerId },
      select: { id: true, username: true, email: true, status: true },
    });

    if (!player || player.status === 'banned') {
      throw new AppError(401, 'Account not found or banned', 'UNAUTHORIZED');
    }

    const tokens = generateTokens({
      playerId: player.id,
      username: player.username,
      email: player.email,
    });

    res.json(tokens);
  } catch (err) {
    next(err);
  }
}

// ============================================
// Get current profile
// ============================================

export async function getProfile(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;

    const player = await prisma.player.findUnique({
      where: { id: playerId },
      select: {
        id: true,
        username: true,
        email: true,
        avatarUrl: true,
        status: true,
        createdAt: true,
        lastActiveAt: true,
        rank: {
          select: {
            tier: true,
            points: true,
            peakPoints: true,
            wins: true,
            losses: true,
            streak: true,
          },
        },
        stats: {
          select: {
            totalMatches: true,
            totalWins: true,
            avgAccuracy: true,
            avgTimeToSolve: true,
            perfectSolves: true,
          },
        },
        preferences: true,
      },
    });

    if (!player) {
      throw new AppError(404, 'Player not found', 'NOT_FOUND');
    }

    res.json({ player });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Update profile
// ============================================

export async function updateProfile(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { avatarUrl, preferences } = req.body as {
      avatarUrl?: string;
      preferences?: {
        preferredDifficulty?: import('../types/case.types').DifficultyLevel;
        enableVoiceChat?: boolean;
        enableNotifications?: boolean;
      };
    };

    const updates: Record<string, unknown> = {};
    if (avatarUrl !== undefined) updates['avatarUrl'] = avatarUrl;

    if (Object.keys(updates).length > 0) {
      await prisma.player.update({ where: { id: playerId }, data: updates });
    }

    if (preferences) {
      await prisma.playerPreferences.update({
        where: { playerId },
        data: preferences,
      });
    }

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Change password
// ============================================

export async function changePassword(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;
    const { currentPassword, newPassword } = req.body as {
      currentPassword: string;
      newPassword: string;
    };

    const player = await prisma.player.findUnique({
      where: { id: playerId },
      select: { passwordHash: true },
    });

    if (!player) throw new AppError(404, 'Player not found', 'NOT_FOUND');

    const valid = await verifyPassword(currentPassword, player.passwordHash);
    if (!valid) {
      throw new AppError(401, 'Current password is incorrect', 'INVALID_CREDENTIALS');
    }

    if (newPassword.length < 8) {
      throw new AppError(400, 'Password must be at least 8 characters', 'VALIDATION_ERROR');
    }

    const newHash = await hashPassword(newPassword);
    await prisma.player.update({
      where: { id: playerId },
      data: { passwordHash: newHash },
    });

    res.json({ success: true, message: 'Password updated' });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Logout (mark offline)
// ============================================

export async function logout(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;

    await prisma.player.update({
      where: { id: playerId },
      data: { status: 'offline', lastActiveAt: new Date() },
    });

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}
