// ============================================
// Logger Middleware
// ============================================

import morgan from 'morgan';
import logger from '../utils/logger';

export const httpLogger = morgan('combined', {
  stream: {
    write: (message: string) => logger.http(message.trim()),
  },
  skip: (_req, res) => {
    // Skip health check logs in production
    if (process.env['NODE_ENV'] === 'production' && res.statusCode < 400) {
      return _req.url === '/health';
    }
    return false;
  },
});
