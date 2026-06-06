// ============================================
// Upload Middleware — multer config for avatar uploads
// ============================================

import multer from 'multer';
import path from 'path';
import fs from 'fs';
import { AppError } from './errorHandler';
import type { Request } from 'express';

// ---- Upload directory ----
const UPLOAD_DIR = process.env['UPLOAD_DIR'] ?? path.join(process.cwd(), 'uploads', 'avatars');

// Ensure directory exists
if (!fs.existsSync(UPLOAD_DIR)) {
  fs.mkdirSync(UPLOAD_DIR, { recursive: true });
}

// ---- Storage config ----
const storage = multer.diskStorage({
  destination: (_req, _file, cb) => cb(null, UPLOAD_DIR),
  filename: (req, file, cb) => {
    const playerId = (req as any).player?.playerId ?? 'unknown';
    const ext = path.extname(file.originalname).toLowerCase();
    cb(null, `avatar_${playerId}_${Date.now()}${ext}`);
  },
});

// ---- File filter: images only ----
const fileFilter = (
  _req: Request,
  file: Express.Multer.File,
  cb: multer.FileFilterCallback
) => {
  const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
  if (allowedTypes.includes(file.mimetype)) {
    cb(null, true);
  } else {
    cb(new AppError(400, 'Only JPEG, PNG, WebP, and GIF images are allowed', 'INVALID_FILE_TYPE'));
  }
};

// ---- Multer instance ----
export const avatarUpload = multer({
  storage,
  fileFilter,
  limits: {
    fileSize: 2 * 1024 * 1024, // 2 MB
    files: 1,
  },
});

// ---- Build public URL for saved file ----
export function getAvatarUrl(filename: string): string {
  const baseUrl = process.env['API_BASE_URL'] ?? 'http://localhost:3000';
  return `${baseUrl}/uploads/avatars/${filename}`;
}
