-- CreateEnum
CREATE TYPE "PlayerStatus" AS ENUM ('online', 'offline', 'in_match', 'in_queue', 'banned');

-- CreateEnum
CREATE TYPE "RankTier" AS ENUM ('rookie', 'detective', 'inspector', 'senior_inspector', 'chief_inspector', 'superintendent', 'commissioner', 'legendary');

-- CreateEnum
CREATE TYPE "DifficultyLevel" AS ENUM ('easy', 'medium', 'hard', 'expert', 'nightmare');

-- CreateEnum
CREATE TYPE "CaseStatus" AS ENUM ('generating', 'active', 'completed', 'archived');

-- CreateEnum
CREATE TYPE "MatchStatus" AS ENUM ('waiting', 'starting', 'active', 'conclusion', 'finished', 'cancelled', 'abandoned');

-- CreateEnum
CREATE TYPE "EvidenceType" AS ENUM ('physical', 'forensic', 'document', 'digital', 'testimonial', 'circumstantial', 'alibi');

-- CreateEnum
CREATE TYPE "WitnessPersonality" AS ENUM ('cooperative', 'nervous', 'hostile', 'deceptive', 'confused', 'protective', 'opportunistic');

-- CreateEnum
CREATE TYPE "HonestyLevel" AS ENUM ('always_honest', 'mostly_honest', 'mixed', 'mostly_lying', 'always_lying');

-- CreateEnum
CREATE TYPE "MatchResult" AS ENUM ('win', 'loss', 'draw', 'abandoned');

-- CreateEnum
CREATE TYPE "RoomVisibility" AS ENUM ('public', 'private');

-- CreateTable
CREATE TABLE "players" (
    "id" TEXT NOT NULL,
    "username" VARCHAR(32) NOT NULL,
    "email" VARCHAR(256) NOT NULL,
    "passwordHash" TEXT NOT NULL,
    "avatarUrl" TEXT,
    "status" "PlayerStatus" NOT NULL DEFAULT 'offline',
    "isEmailVerified" BOOLEAN NOT NULL DEFAULT false,
    "isBanned" BOOLEAN NOT NULL DEFAULT false,
    "banReason" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,
    "lastActiveAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "players_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "refresh_tokens" (
    "id" TEXT NOT NULL,
    "token" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "expiresAt" TIMESTAMP(3) NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "isRevoked" BOOLEAN NOT NULL DEFAULT false,

    CONSTRAINT "refresh_tokens_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "player_ranks" (
    "id" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "tier" "RankTier" NOT NULL DEFAULT 'rookie',
    "points" INTEGER NOT NULL DEFAULT 0,
    "peakPoints" INTEGER NOT NULL DEFAULT 0,
    "season" INTEGER NOT NULL DEFAULT 1,
    "wins" INTEGER NOT NULL DEFAULT 0,
    "losses" INTEGER NOT NULL DEFAULT 0,
    "streak" INTEGER NOT NULL DEFAULT 0,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "player_ranks_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "player_stats" (
    "id" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "totalMatches" INTEGER NOT NULL DEFAULT 0,
    "totalWins" INTEGER NOT NULL DEFAULT 0,
    "totalAccuracyScore" INTEGER NOT NULL DEFAULT 0,
    "avgAccuracy" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "avgTimeToSolve" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "perfectSolves" INTEGER NOT NULL DEFAULT 0,
    "killerIdentifiedCount" INTEGER NOT NULL DEFAULT 0,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "player_stats_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "player_preferences" (
    "id" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "preferredDifficulty" "DifficultyLevel" NOT NULL DEFAULT 'medium',
    "preferRegion" TEXT,
    "enableVoiceChat" BOOLEAN NOT NULL DEFAULT true,
    "enableNotifications" BOOLEAN NOT NULL DEFAULT true,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "player_preferences_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "cases" (
    "id" TEXT NOT NULL,
    "title" TEXT NOT NULL,
    "description" TEXT NOT NULL,
    "difficulty" "DifficultyLevel" NOT NULL,
    "status" "CaseStatus" NOT NULL DEFAULT 'generating',
    "seed" TEXT NOT NULL,
    "tags" TEXT[],
    "victimName" TEXT NOT NULL,
    "victimAge" INTEGER NOT NULL,
    "victimOccupation" TEXT NOT NULL,
    "victimBackstory" TEXT NOT NULL,
    "causeOfDeath" TEXT NOT NULL,
    "timeOfDeath" TIMESTAMP(3) NOT NULL,
    "locationFound" TEXT NOT NULL,
    "killerId" TEXT NOT NULL,
    "motive" TEXT NOT NULL,
    "weapon" TEXT NOT NULL,
    "murderLocation" TEXT NOT NULL,
    "solutionTimeline" TEXT NOT NULL,
    "solutionMethod" TEXT NOT NULL,
    "solutionNarrative" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "cases_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "suspects" (
    "id" TEXT NOT NULL,
    "caseId" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "age" INTEGER NOT NULL,
    "occupation" TEXT NOT NULL,
    "backstory" TEXT NOT NULL,
    "personality" TEXT NOT NULL,
    "isKiller" BOOLEAN NOT NULL DEFAULT false,
    "alibi" TEXT NOT NULL,
    "alibiIsTrue" BOOLEAN NOT NULL,
    "motive" TEXT,
    "relationships" JSONB NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "suspects_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "witnesses" (
    "id" TEXT NOT NULL,
    "caseId" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "age" INTEGER NOT NULL,
    "occupation" TEXT NOT NULL,
    "backstory" TEXT NOT NULL,
    "personality" "WitnessPersonality" NOT NULL,
    "honestyLevel" "HonestyLevel" NOT NULL,
    "isKiller" BOOLEAN NOT NULL DEFAULT false,
    "isSuspect" BOOLEAN NOT NULL DEFAULT false,
    "alibi" TEXT NOT NULL,
    "alibiIsTrue" BOOLEAN NOT NULL,
    "knownFacts" TEXT[],
    "hiddenFacts" TEXT[],
    "relationships" JSONB NOT NULL,
    "memories" JSONB NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "witnesses_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "npc_dialogue_sessions" (
    "id" TEXT NOT NULL,
    "witnessId" TEXT NOT NULL,
    "matchId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "stressLevel" TEXT NOT NULL DEFAULT 'calm',
    "trustScore" DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    "revealedFacts" TEXT[],
    "contradictionCount" INTEGER NOT NULL DEFAULT 0,
    "messageCount" INTEGER NOT NULL DEFAULT 0,
    "conversationHistory" JSONB NOT NULL,
    "startedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "npc_dialogue_sessions_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "evidences" (
    "id" TEXT NOT NULL,
    "caseId" TEXT NOT NULL,
    "type" "EvidenceType" NOT NULL,
    "name" TEXT NOT NULL,
    "description" TEXT NOT NULL,
    "location" TEXT NOT NULL,
    "isReal" BOOLEAN NOT NULL,
    "isFakeEvidence" BOOLEAN NOT NULL DEFAULT false,
    "reliability" TEXT NOT NULL DEFAULT 'suspected',
    "pointsTo" TEXT,
    "imageUrl" TEXT,
    "metadata" JSONB NOT NULL DEFAULT '{}',

    CONSTRAINT "evidences_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "evidence_discoveries" (
    "id" TEXT NOT NULL,
    "evidenceId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "matchId" TEXT NOT NULL,
    "sharedWith" TEXT[],
    "notes" TEXT NOT NULL DEFAULT '',
    "discoveredAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "evidence_discoveries_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "timeline_events" (
    "id" TEXT NOT NULL,
    "caseId" TEXT NOT NULL,
    "timestamp" TIMESTAMP(3) NOT NULL,
    "description" TEXT NOT NULL,
    "involvedIds" TEXT[],
    "location" TEXT NOT NULL,
    "isKeyEvent" BOOLEAN NOT NULL DEFAULT false,
    "isPublicInfo" BOOLEAN NOT NULL DEFAULT true,
    "evidenceId" TEXT,
    "order" INTEGER NOT NULL,

    CONSTRAINT "timeline_events_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "rooms" (
    "id" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "hostId" TEXT NOT NULL,
    "visibility" "RoomVisibility" NOT NULL DEFAULT 'public',
    "passwordHash" TEXT,
    "difficulty" "DifficultyLevel" NOT NULL DEFAULT 'medium',
    "maxPlayers" INTEGER NOT NULL DEFAULT 4,
    "currentPlayers" INTEGER NOT NULL DEFAULT 0,
    "isInMatch" BOOLEAN NOT NULL DEFAULT false,
    "settings" JSONB NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "rooms_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "matches" (
    "id" TEXT NOT NULL,
    "roomId" TEXT NOT NULL,
    "caseId" TEXT NOT NULL,
    "difficulty" "DifficultyLevel" NOT NULL,
    "status" "MatchStatus" NOT NULL DEFAULT 'waiting',
    "phase" TEXT NOT NULL DEFAULT 'investigation',
    "maxPlayers" INTEGER NOT NULL DEFAULT 4,
    "durationSeconds" INTEGER NOT NULL,
    "startedAt" TIMESTAMP(3),
    "endedAt" TIMESTAMP(3),
    "winnerId" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "matches_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "match_players" (
    "id" TEXT NOT NULL,
    "matchId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "joinedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "isReady" BOOLEAN NOT NULL DEFAULT false,
    "isHost" BOOLEAN NOT NULL DEFAULT false,
    "hasSubmitted" BOOLEAN NOT NULL DEFAULT false,
    "hintsUsed" INTEGER NOT NULL DEFAULT 0,
    "timeSpent" INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT "match_players_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "player_conclusions" (
    "id" TEXT NOT NULL,
    "matchId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "submittedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "killerId" TEXT NOT NULL,
    "motive" TEXT NOT NULL,
    "weapon" TEXT NOT NULL,
    "location" TEXT NOT NULL,
    "timeline" TEXT NOT NULL,
    "narrative" TEXT NOT NULL,

    CONSTRAINT "player_conclusions_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "match_scores" (
    "id" TEXT NOT NULL,
    "matchId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "conclusionId" TEXT NOT NULL,
    "totalScore" INTEGER NOT NULL,
    "killerScore" INTEGER NOT NULL,
    "motiveScore" INTEGER NOT NULL,
    "weaponScore" INTEGER NOT NULL,
    "locationScore" INTEGER NOT NULL,
    "timelineScore" INTEGER NOT NULL,
    "narrativeScore" INTEGER NOT NULL,
    "timeBonus" INTEGER NOT NULL,
    "isCorrect" BOOLEAN NOT NULL,
    "finalRank" INTEGER NOT NULL,
    "rpChange" INTEGER NOT NULL,
    "result" "MatchResult" NOT NULL,

    CONSTRAINT "match_scores_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "notebook_entries" (
    "id" TEXT NOT NULL,
    "matchPlayerId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "content" TEXT NOT NULL,
    "relatedEvidenceId" TEXT,
    "relatedSuspectId" TEXT,
    "relatedWitnessId" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "notebook_entries_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "seasons" (
    "id" TEXT NOT NULL,
    "number" INTEGER NOT NULL,
    "name" TEXT NOT NULL,
    "startDate" TIMESTAMP(3) NOT NULL,
    "endDate" TIMESTAMP(3) NOT NULL,
    "isActive" BOOLEAN NOT NULL DEFAULT false,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "seasons_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "season_leaderboard" (
    "id" TEXT NOT NULL,
    "seasonId" TEXT NOT NULL,
    "playerId" TEXT NOT NULL,
    "rank" INTEGER NOT NULL,
    "points" INTEGER NOT NULL,
    "wins" INTEGER NOT NULL,
    "tier" "RankTier" NOT NULL,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "season_leaderboard_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "_EvidenceImplicatesSuspect" (
    "A" TEXT NOT NULL,
    "B" TEXT NOT NULL
);

-- CreateIndex
CREATE UNIQUE INDEX "players_username_key" ON "players"("username");

-- CreateIndex
CREATE UNIQUE INDEX "players_email_key" ON "players"("email");

-- CreateIndex
CREATE INDEX "players_email_idx" ON "players"("email");

-- CreateIndex
CREATE INDEX "players_username_idx" ON "players"("username");

-- CreateIndex
CREATE UNIQUE INDEX "refresh_tokens_token_key" ON "refresh_tokens"("token");

-- CreateIndex
CREATE INDEX "refresh_tokens_playerId_idx" ON "refresh_tokens"("playerId");

-- CreateIndex
CREATE UNIQUE INDEX "player_ranks_playerId_key" ON "player_ranks"("playerId");

-- CreateIndex
CREATE UNIQUE INDEX "player_stats_playerId_key" ON "player_stats"("playerId");

-- CreateIndex
CREATE UNIQUE INDEX "player_preferences_playerId_key" ON "player_preferences"("playerId");

-- CreateIndex
CREATE UNIQUE INDEX "cases_seed_key" ON "cases"("seed");

-- CreateIndex
CREATE INDEX "cases_difficulty_idx" ON "cases"("difficulty");

-- CreateIndex
CREATE INDEX "cases_status_idx" ON "cases"("status");

-- CreateIndex
CREATE INDEX "suspects_caseId_idx" ON "suspects"("caseId");

-- CreateIndex
CREATE INDEX "witnesses_caseId_idx" ON "witnesses"("caseId");

-- CreateIndex
CREATE INDEX "npc_dialogue_sessions_matchId_idx" ON "npc_dialogue_sessions"("matchId");

-- CreateIndex
CREATE UNIQUE INDEX "npc_dialogue_sessions_witnessId_matchId_playerId_key" ON "npc_dialogue_sessions"("witnessId", "matchId", "playerId");

-- CreateIndex
CREATE INDEX "evidences_caseId_idx" ON "evidences"("caseId");

-- CreateIndex
CREATE INDEX "evidences_isReal_idx" ON "evidences"("isReal");

-- CreateIndex
CREATE INDEX "evidence_discoveries_matchId_idx" ON "evidence_discoveries"("matchId");

-- CreateIndex
CREATE UNIQUE INDEX "evidence_discoveries_evidenceId_playerId_matchId_key" ON "evidence_discoveries"("evidenceId", "playerId", "matchId");

-- CreateIndex
CREATE INDEX "timeline_events_caseId_idx" ON "timeline_events"("caseId");

-- CreateIndex
CREATE INDEX "rooms_visibility_idx" ON "rooms"("visibility");

-- CreateIndex
CREATE INDEX "matches_status_idx" ON "matches"("status");

-- CreateIndex
CREATE INDEX "matches_roomId_idx" ON "matches"("roomId");

-- CreateIndex
CREATE INDEX "match_players_matchId_idx" ON "match_players"("matchId");

-- CreateIndex
CREATE UNIQUE INDEX "match_players_matchId_playerId_key" ON "match_players"("matchId", "playerId");

-- CreateIndex
CREATE INDEX "player_conclusions_matchId_idx" ON "player_conclusions"("matchId");

-- CreateIndex
CREATE UNIQUE INDEX "player_conclusions_matchId_playerId_key" ON "player_conclusions"("matchId", "playerId");

-- CreateIndex
CREATE UNIQUE INDEX "match_scores_conclusionId_key" ON "match_scores"("conclusionId");

-- CreateIndex
CREATE INDEX "match_scores_matchId_idx" ON "match_scores"("matchId");

-- CreateIndex
CREATE UNIQUE INDEX "match_scores_matchId_playerId_key" ON "match_scores"("matchId", "playerId");

-- CreateIndex
CREATE INDEX "notebook_entries_matchPlayerId_idx" ON "notebook_entries"("matchPlayerId");

-- CreateIndex
CREATE UNIQUE INDEX "seasons_number_key" ON "seasons"("number");

-- CreateIndex
CREATE INDEX "season_leaderboard_seasonId_rank_idx" ON "season_leaderboard"("seasonId", "rank");

-- CreateIndex
CREATE UNIQUE INDEX "season_leaderboard_seasonId_playerId_key" ON "season_leaderboard"("seasonId", "playerId");

-- CreateIndex
CREATE UNIQUE INDEX "_EvidenceImplicatesSuspect_AB_unique" ON "_EvidenceImplicatesSuspect"("A", "B");

-- CreateIndex
CREATE INDEX "_EvidenceImplicatesSuspect_B_index" ON "_EvidenceImplicatesSuspect"("B");

-- AddForeignKey
ALTER TABLE "refresh_tokens" ADD CONSTRAINT "refresh_tokens_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "player_ranks" ADD CONSTRAINT "player_ranks_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "player_stats" ADD CONSTRAINT "player_stats_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "player_preferences" ADD CONSTRAINT "player_preferences_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "suspects" ADD CONSTRAINT "suspects_caseId_fkey" FOREIGN KEY ("caseId") REFERENCES "cases"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "witnesses" ADD CONSTRAINT "witnesses_caseId_fkey" FOREIGN KEY ("caseId") REFERENCES "cases"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "npc_dialogue_sessions" ADD CONSTRAINT "npc_dialogue_sessions_witnessId_fkey" FOREIGN KEY ("witnessId") REFERENCES "witnesses"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "npc_dialogue_sessions" ADD CONSTRAINT "npc_dialogue_sessions_matchId_fkey" FOREIGN KEY ("matchId") REFERENCES "matches"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "evidences" ADD CONSTRAINT "evidences_caseId_fkey" FOREIGN KEY ("caseId") REFERENCES "cases"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "evidence_discoveries" ADD CONSTRAINT "evidence_discoveries_evidenceId_fkey" FOREIGN KEY ("evidenceId") REFERENCES "evidences"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "evidence_discoveries" ADD CONSTRAINT "evidence_discoveries_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "evidence_discoveries" ADD CONSTRAINT "evidence_discoveries_matchId_fkey" FOREIGN KEY ("matchId") REFERENCES "matches"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "timeline_events" ADD CONSTRAINT "timeline_events_caseId_fkey" FOREIGN KEY ("caseId") REFERENCES "cases"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "matches" ADD CONSTRAINT "matches_roomId_fkey" FOREIGN KEY ("roomId") REFERENCES "rooms"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "matches" ADD CONSTRAINT "matches_caseId_fkey" FOREIGN KEY ("caseId") REFERENCES "cases"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "match_players" ADD CONSTRAINT "match_players_matchId_fkey" FOREIGN KEY ("matchId") REFERENCES "matches"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "match_players" ADD CONSTRAINT "match_players_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "player_conclusions" ADD CONSTRAINT "player_conclusions_matchId_fkey" FOREIGN KEY ("matchId") REFERENCES "matches"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "player_conclusions" ADD CONSTRAINT "player_conclusions_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "match_scores" ADD CONSTRAINT "match_scores_matchId_fkey" FOREIGN KEY ("matchId") REFERENCES "matches"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "match_scores" ADD CONSTRAINT "match_scores_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "match_scores" ADD CONSTRAINT "match_scores_conclusionId_fkey" FOREIGN KEY ("conclusionId") REFERENCES "player_conclusions"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "notebook_entries" ADD CONSTRAINT "notebook_entries_matchPlayerId_fkey" FOREIGN KEY ("matchPlayerId") REFERENCES "match_players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "notebook_entries" ADD CONSTRAINT "notebook_entries_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "players"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "season_leaderboard" ADD CONSTRAINT "season_leaderboard_seasonId_fkey" FOREIGN KEY ("seasonId") REFERENCES "seasons"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "_EvidenceImplicatesSuspect" ADD CONSTRAINT "_EvidenceImplicatesSuspect_A_fkey" FOREIGN KEY ("A") REFERENCES "evidences"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "_EvidenceImplicatesSuspect" ADD CONSTRAINT "_EvidenceImplicatesSuspect_B_fkey" FOREIGN KEY ("B") REFERENCES "suspects"("id") ON DELETE CASCADE ON UPDATE CASCADE;
