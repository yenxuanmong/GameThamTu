// ============================================
// Upload Controller — avatar image upload
// ============================================

import type { Request, Response, NextFunction } from 'express';
import path from 'path';
import fs from 'fs';
import { prisma } from '../configs/database';
import { getAvatarUrl } from '../middleware/upload';
import { AppError } from '../middleware/errorHandler';
import type { AuthenticatedRequest } from '../middleware/authMiddleware';
import logger from '../utils/logger';

// ============================================
// Upload avatar
// ============================================

export async function uploadAvatar(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;

    if (!req.file) {
      throw new AppError(400, 'No image file provided', 'MISSING_FILE');
    }

    const avatarUrl = getAvatarUrl(req.file.filename);

    // Delete old avatar file if it was a local upload
    const existing = await prisma.player.findUnique({
      where: { id: playerId },
      select: { avatarUrl: true },
    });

    if (existing?.avatarUrl) {
      const oldFilename = existing.avatarUrl.split('/').pop();
      if (oldFilename) {
        const uploadDir = process.env['UPLOAD_DIR'] ?? path.join(process.cwd(), 'uploads', 'avatars');
        const oldPath = path.join(uploadDir, oldFilename);
        if (fs.existsSync(oldPath)) {
          fs.unlinkSync(oldPath);
        }
      }
    }

    await prisma.player.update({
      where: { id: playerId },
      data: { avatarUrl },
    });

    logger.info('[UploadController] Avatar uploaded', { playerId, avatarUrl });

    res.json({ avatarUrl });
  } catch (err) {
    // Clean up uploaded file on error
    if (req.file?.path && fs.existsSync(req.file.path)) {
      fs.unlinkSync(req.file.path);
    }
    next(err);
  }
}

// ============================================
// Delete avatar
// ============================================

export async function deleteAvatar(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { playerId } = (req as AuthenticatedRequest).player;

    const player = await prisma.player.findUnique({
      where: { id: playerId },
      select: { avatarUrl: true },
    });

    if (player?.avatarUrl) {
      const filename = player.avatarUrl.split('/').pop();
      if (filename) {
        const uploadDir = process.env['UPLOAD_DIR'] ?? path.join(process.cwd(), 'uploads', 'avatars');
        const filePath = path.join(uploadDir, filename);
        if (fs.existsSync(filePath)) {
          fs.unlinkSync(filePath);
        }
      }
    }

    await prisma.player.update({
      where: { id: playerId },
      data: { avatarUrl: null },
    });

    res.json({ success: true });
  } catch (err) {
    next(err);
  }
}
