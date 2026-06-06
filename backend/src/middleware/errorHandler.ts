// ============================================
// Error Handler Middleware
// ============================================

import type { Request, Response, NextFunction } from 'express';
import logger from '../utils/logger';

export class AppError extends Error {
  constructor(
    public readonly statusCode: number,
    message: string,
    public readonly code?: string
  ) {
    super(message);
    this.name = 'AppError';
  }
}

export function errorHandler(
  err: Error,
  _req: Request,
  res: Response,
  _next: NextFunction
): void {
  if (err instanceof AppError) {
    res.status(err.statusCode).json({
      error: err.message,
      code: err.code,
    });
    return;
  }

  // Prisma errors
  if (err.constructor.name === 'PrismaClientKnownRequestError') {
    const prismaErr = err as unknown as { code: string };
    if (prismaErr.code === 'P2002') {
      res.status(409).json({ error: 'Resource already exists', code: 'DUPLICATE' });
      return;
    }
    if (prismaErr.code === 'P2025') {
      res.status(404).json({ error: 'Resource not found', code: 'NOT_FOUND' });
      return;
    }
  }

  logger.error('Unhandled error', { error: err.message, stack: err.stack });

  res.status(500).json({
    error: process.env['NODE_ENV'] === 'production' ? 'Internal server error' : err.message,
  });
}
