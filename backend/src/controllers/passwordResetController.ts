// ============================================
// Password Reset Controller
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { body, validationResult } from 'express-validator';
import crypto from 'crypto';
import { prisma } from '../configs/database';
import { hashPassword } from '../utils/auth';
import { sendPasswordResetEmail } from '../utils/email';
import { AppError } from '../middleware/errorHandler';
import logger from '../utils/logger';

const RESET_TOKEN_EXPIRY_MS = 30 * 60 * 1000; // 30 minutes

// ============================================
// Validation
// ============================================

export const forgotPasswordValidation = [
  body('email').isEmail().normalizeEmail().withMessage('Invalid email address'),
];

export const resetPasswordValidation = [
  body('token').notEmpty().withMessage('Reset token required'),
  body('newPassword')
    .isLength({ min: 8 })
    .matches(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/)
    .withMessage('Password must be 8+ chars with upper, lower, and number'),
];

// ============================================
// Step 1: Request reset — send email
// ============================================

export async function forgotPassword(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const { email } = req.body as { email: string };

    // Always return success to prevent email enumeration
    const successResponse = {
      success: true,
      message: 'If an account with that email exists, a reset link has been sent.',
    };

    const player = await prisma.player.findUnique({
      where: { email },
      select: { id: true, username: true, email: true },
    });

    if (!player) {
      // Silent — don't reveal if email exists
      res.json(successResponse);
      return;
    }

    // Generate cryptographically secure token
    const rawToken = crypto.randomBytes(32).toString('hex');
    const tokenHash = crypto.createHash('sha256').update(rawToken).digest('hex');
    const expiresAt = new Date(Date.now() + RESET_TOKEN_EXPIRY_MS);

    // Invalidate any existing tokens for this player
    await prisma.refreshToken.deleteMany({
      where: { playerId: player.id, isRevoked: false },
    });

    // Store hashed token in DB (reuse RefreshToken table with a special marker, 
    // or use a dedicated table — we store it as a short-lived refresh token)
    await prisma.refreshToken.create({
      data: {
        token: `reset:${tokenHash}`,
        playerId: player.id,
        expiresAt,
        isRevoked: false,
      },
    });

    // Build reset URL
    const frontendUrl = process.env['FRONTEND_URL'] ?? 'http://localhost:5173';
    const resetUrl = `${frontendUrl}/reset-password?token=${rawToken}`;

    await sendPasswordResetEmail(player.email, player.username, rawToken, resetUrl);

    logger.info('[PasswordReset] Reset email sent', { playerId: player.id });

    res.json(successResponse);
  } catch (err) {
    next(err);
  }
}

// ============================================
// Step 2: Verify token is valid (GET — for frontend check before showing form)
// ============================================

export async function verifyResetToken(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { token } = req.query as { token: string };

    if (!token) throw new AppError(400, 'Token required', 'MISSING_TOKEN');

    const tokenHash = crypto.createHash('sha256').update(token).digest('hex');

    const record = await prisma.refreshToken.findFirst({
      where: {
        token: `reset:${tokenHash}`,
        isRevoked: false,
        expiresAt: { gt: new Date() },
      },
      select: { id: true },
    });

    if (!record) {
      throw new AppError(400, 'Invalid or expired reset token', 'INVALID_TOKEN');
    }

    res.json({ valid: true });
  } catch (err) {
    next(err);
  }
}

// ============================================
// Step 3: Apply new password
// ============================================

export async function resetPassword(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const errors = validationResult(req);
    if (!errors.isEmpty()) {
      throw new AppError(400, errors.array()[0]?.msg ?? 'Validation error', 'VALIDATION_ERROR');
    }

    const { token, newPassword } = req.body as { token: string; newPassword: string };

    const tokenHash = crypto.createHash('sha256').update(token).digest('hex');

    const record = await prisma.refreshToken.findFirst({
      where: {
        token: `reset:${tokenHash}`,
        isRevoked: false,
        expiresAt: { gt: new Date() },
      },
      select: { id: true, playerId: true },
    });

    if (!record) {
      throw new AppError(400, 'Invalid or expired reset token', 'INVALID_TOKEN');
    }

    const newHash = await hashPassword(newPassword);

    // Update password + revoke the reset token
    await prisma.$transaction([
      prisma.player.update({
        where: { id: record.playerId },
        data: { passwordHash: newHash },
      }),
      prisma.refreshToken.update({
        where: { id: record.id },
        data: { isRevoked: true },
      }),
      // Also revoke all other refresh tokens to force re-login everywhere
      prisma.refreshToken.updateMany({
        where: { playerId: record.playerId, isRevoked: false },
        data: { isRevoked: true },
      }),
    ]);

    logger.info('[PasswordReset] Password reset successful', { playerId: record.playerId });

    res.json({ success: true, message: 'Password has been reset. Please log in.' });
  } catch (err) {
    next(err);
  }
}
