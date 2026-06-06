// ============================================
// Rate Limiter Middleware
// ============================================

import rateLimit from 'express-rate-limit';

const WINDOW_MS = parseInt(process.env['RATE_LIMIT_WINDOW_MS'] ?? '900000', 10);
const MAX_REQUESTS = parseInt(process.env['RATE_LIMIT_MAX_REQUESTS'] ?? '100', 10);

export const globalRateLimiter = rateLimit({
  windowMs: WINDOW_MS,
  max: MAX_REQUESTS,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Too many requests — please try again later', code: 'RATE_LIMITED' },
});

export const authRateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,  // 15 minutes
  max: 10,
  message: { error: 'Too many auth attempts', code: 'AUTH_RATE_LIMITED' },
});

export const npcRateLimiter = rateLimit({
  windowMs: 60 * 1000,       // 1 minute
  max: 30,                    // 30 NPC messages per minute
  message: { error: 'Slow down — the witness needs a moment', code: 'NPC_RATE_LIMITED' },
});
