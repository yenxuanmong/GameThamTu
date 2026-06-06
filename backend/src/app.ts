// ============================================
// Detective Royale — App Entry Point
// ============================================

import 'dotenv/config';
import express from 'express';
import http from 'http';
import cors from 'cors';
import helmet from 'helmet';
import compression from 'compression';
import morgan from 'morgan';
import path from 'path';

import { connectDatabase, disconnectDatabase } from './configs/database';
import { connectRedis, disconnectRedis } from './configs/redis';
import { globalRateLimiter } from './middleware/rateLimiter';
import { errorHandler } from './middleware/errorHandler';
import { initSocketServer } from './server/socket/socketServer';
import { checkSeasonRotation } from './server/ranking/seasonSystem';
import logger from './utils/logger';



// ---- Route imports ----
import authRouter from './routes/auth';
import casesRouter from './routes/cases';
import matchesRouter from './routes/matches';
import rankingRouter from './routes/ranking';
import evidenceRouter from './routes/evidence';
import analyticsRouter from './routes/analytics';
import roomsRouter from './routes/rooms';

// ---- App Setup ----

const app = express();
const server = http.createServer(app);

// ---- Middleware ----

app.use(helmet());
app.use(
  cors({
    origin: process.env['FRONTEND_URL'] ?? 'http://localhost:5173',
    credentials: true,
    methods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH'],
  })
);
app.use(compression());
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true }));
app.use(
  morgan('combined', {
    stream: { write: (msg) => logger.http(msg.trim()) },
    skip: () => process.env['NODE_ENV'] === 'test',
  })
);
app.use(globalRateLimiter);

// ---- Health Check ----

app.get('/health', (_req, res) => {
  res.json({
    status: 'ok',
    app: process.env['APP_NAME'],
    version: process.env['APP_VERSION'],
    env: process.env['NODE_ENV'],
    timestamp: new Date().toISOString(),
  });
});

// ---- Static files (avatar uploads) ----
const uploadDir = process.env['UPLOAD_DIR'] ?? path.join(process.cwd(), 'uploads', 'avatars');
app.use(
  '/uploads/avatars',
  express.static(uploadDir, {
    maxAge: '7d',
    setHeaders: (res) => {
      res.setHeader('X-Content-Type-Options', 'nosniff');
    },
  })
);

// ---- API Routes ----

app.use('/api/auth', authRouter);
app.use('/api/rooms', roomsRouter);
app.use('/api/cases', casesRouter);
app.use('/api/matches', matchesRouter);
app.use('/api/ranking', rankingRouter);
app.use('/api/analytics', analyticsRouter);
// Evidence routes nested under matches: /api/matches/:matchId/evidence
app.use('/api/matches/:matchId/evidence', evidenceRouter);

// ---- 404 Handler ----

app.use((_req, res) => {
  res.status(404).json({ error: 'Route not found', code: 'NOT_FOUND' });
});

// ---- Global Error Handler ----

app.use(errorHandler);

// ---- Bootstrap ----

async function bootstrap(): Promise<void> {
  const PORT = parseInt(process.env['PORT'] ?? '3000', 10);

  try {
    await connectDatabase();
    await connectRedis();

    // Initialise Socket.IO
    const io = initSocketServer(server);
    logger.info('[App] Socket.IO server initialised');

    // Check season rotation on startup
    await checkSeasonRotation().catch((err) =>
      logger.warn('[App] Season rotation check failed', { err })
    );

    // Schedule season rotation check every 6 hours
    setInterval(() => {
      void checkSeasonRotation().catch((err) =>
        logger.warn('[App] Periodic season rotation failed', { err })
      );
    }, 6 * 60 * 60 * 1000);

    server.listen(PORT, () => {
      logger.info(`🚀 ${process.env['APP_NAME']} server running on port ${PORT}`);
      logger.info(`   Environment: ${process.env['NODE_ENV']}`);
      logger.info(`   Health:      http://localhost:${PORT}/health`);
      logger.info(`   API:         http://localhost:${PORT}/api`);
    });

    // Export io for use in services
    (global as any).io = io;
  } catch (error) {
    logger.error('Bootstrap failed', { error });
    process.exit(1);
  }
}

// ---- Graceful Shutdown ----

async function shutdown(signal: string): Promise<void> {
  logger.info(`Received ${signal}. Graceful shutdown...`);
  server.close(async () => {
    await disconnectDatabase();
    await disconnectRedis();
    logger.info('✅ Server shut down cleanly');
    process.exit(0);
  });

  // Force exit after 10 seconds
  setTimeout(() => {
    logger.error('Forced shutdown after timeout');
    process.exit(1);
  }, 10_000);
}

process.on('SIGTERM', () => void shutdown('SIGTERM'));
process.on('SIGINT', () => void shutdown('SIGINT'));
process.on('uncaughtException', (err) => {
  logger.error('Uncaught Exception', { error: err.message, stack: err.stack });
  process.exit(1);
});
process.on('unhandledRejection', (reason) => {
  logger.error('Unhandled Rejection', { reason });
  process.exit(1);
});

void bootstrap();

export default app;
