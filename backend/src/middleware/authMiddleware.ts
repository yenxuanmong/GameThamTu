// ============================================
// Auth Middleware — Express JWT guard
// ============================================

import type { Request, Response, NextFunction } from 'express';
import { verifyAccessToken } from '../utils/auth';
import type { AuthTokenPayload } from '../types/player.types';

export interface AuthenticatedRequest extends Request {
  player: AuthTokenPayload;
}

export function requireAuth(req: Request, res: Response, next: NextFunction): void {
  const header = req.headers['authorization'];
  if (!header?.startsWith('Bearer ')) {
    res.status(401).json({ error: 'Authentication required' });
    return;
  }

  const token = header.slice(7);
  try {
    const payload = verifyAccessToken(token);
    (req as AuthenticatedRequest).player = payload;
    next();
  } catch {
    res.status(401).json({ error: 'Invalid or expired token' });
  }
}

export function optionalAuth(req: Request, _res: Response, next: NextFunction): void {
  const header = req.headers['authorization'];
  if (header?.startsWith('Bearer ')) {
    try {
      const payload = verifyAccessToken(header.slice(7));
      (req as AuthenticatedRequest).player = payload;
    } catch {
      // Token invalid but optional — continue without auth
    }
  }
  next();
}
