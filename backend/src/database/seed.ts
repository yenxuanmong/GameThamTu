// ============================================
// Database Seed — Detective Royale
// Creates initial data: first season, admin player, test accounts
// ============================================

import 'dotenv/config';
import { PrismaClient } from '@prisma/client';
import bcrypt from 'bcryptjs';
import { v4 as uuidv4 } from 'uuid';

const prisma = new PrismaClient();

async function main() {
  console.log('🌱 Seeding database...');

  // ============================================
  // Season 1
  // ============================================

  const existingSeason = await prisma.season.findFirst({ where: { number: 1 } });

  let season;
  if (!existingSeason) {
    const now = new Date();
    const endDate = new Date(now.getTime() + 90 * 24 * 60 * 60 * 1000); // 90 days

    season = await prisma.season.create({
      data: {
        id: uuidv4(),
        number: 1,
        name: 'Season 1: The Opening Case',
        startDate: now,
        endDate,
        isActive: true,
      },
    });
    console.log(`✅ Season 1 created (ends ${endDate.toDateString()})`);
  } else {
    season = existingSeason;
    console.log('ℹ️  Season 1 already exists — skipping');
  }

  // ============================================
  // Test players
  // ============================================

  const testPlayers = [
    { username: 'detective_ace', email: 'ace@test.com', password: 'TestPass1' },
    { username: 'sherlock_jr',   email: 'sherlock@test.com', password: 'TestPass1' },
    { username: 'miss_marple2',  email: 'marple@test.com', password: 'TestPass1' },
  ];

  for (const tp of testPlayers) {
    const exists = await prisma.player.findUnique({ where: { email: tp.email } });
    if (exists) {
      console.log(`ℹ️  Player ${tp.username} already exists — skipping`);
      continue;
    }

    const playerId = uuidv4();
    const passwordHash = await bcrypt.hash(tp.password, 12);

    await prisma.player.create({
      data: {
        id: playerId,
        username: tp.username,
        email: tp.email,
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
    });

    // Add to season leaderboard
    await prisma.seasonLeaderboard.create({
      data: {
        seasonId: season.id,
        playerId,
        rank: 0,
        points: 0,
        wins: 0,
        tier: 'rookie',
        updatedAt: new Date(),
      },
    });

    console.log(`✅ Player created: ${tp.username} (${tp.email}) / password: ${tp.password}`);
  }

  // ============================================
  // Summary
  // ============================================

  const playerCount = await prisma.player.count();
  const seasonCount = await prisma.season.count();

  console.log('\n📊 Seed complete:');
  console.log(`   Players: ${playerCount}`);
  console.log(`   Seasons: ${seasonCount}`);
  console.log('\n🔑 Test login credentials:');
  for (const tp of testPlayers) {
    console.log(`   ${tp.email} / ${tp.password}`);
  }
}

main()
  .catch((err) => {
    console.error('❌ Seed failed:', err);
    process.exit(1);
  })
  .finally(() => prisma.$disconnect());
