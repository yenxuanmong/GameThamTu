// ============================================
// Models — all data transfer objects matching backend types exactly
// Covers every API endpoint in /api/auth, /rooms, /cases,
//   /matches, /evidence, /ranking, /analytics
// ============================================
using System;
using System.Collections.Generic;

namespace DetectiveRoyale.Core
{
    // ==================================================
    // AUTH
    // ==================================================

    [Serializable]
    public class AuthTokens
    {
        public string accessToken;
        public string refreshToken;
        public long   expiresIn;
    }

    [Serializable]
    public class PlayerProfile
    {
        public string          id;
        public string          username;
        public string          email;
        public string          avatarUrl;
        public string          status;      // online | offline | in_match
        public PlayerRankData  rank;
        public PlayerStatsData stats;
        public PlayerPrefsData preferences;
        public string          createdAt;
    }

    [Serializable]
    public class PlayerRankData
    {
        public string tier;        // rookie | detective | inspector | sergeant | lieutenant | captain | commissioner
        public int    points;
        public int    peakPoints;
        public int    wins;
        public int    losses;
        public int    streak;
        public int    season;
    }

    [Serializable]
    public class PlayerStatsData
    {
        public int   totalMatches;
        public int   totalWins;
        public float avgAccuracy;
        public float avgTimeToSolve;
        public int   perfectSolves;
    }

    [Serializable]
    public class PlayerPrefsData
    {
        public string preferredDifficulty;
        public bool   enableVoiceChat;
        public bool   enableNotifications;
    }

    // ==================================================
    // ROOMS
    // ==================================================

    [Serializable]
    public class Room
    {
        public string       id;
        public string       name;
        public string       hostId;
        public string       visibility;    // public | private
        public string       difficulty;    // easy | medium | hard | expert | nightmare
        public int          maxPlayers;
        public int          currentPlayers;
        public bool         isInMatch;
        public bool         hasPassword;
        public string       currentMatchId;
        public RoomSettings settings;
        public string       createdAt;
    }

    [Serializable]
    public class RoomSettings
    {
        public string difficulty;
        public int    maxPlayers;
        public bool   allowSpectators;
        public bool   enableVoiceChat;
        public bool   autoStart;
        public int    startCountdownSeconds;
    }

    [Serializable]
    public class RoomPlayer
    {
        public string id;
        public string username;
        public string avatarUrl;
        public bool   isHost;
        public bool   isReady;
        public string tier;
        public int    points;
    }

    // ==================================================
    // CASES
    // ==================================================

    [Serializable]
    public class CaseListItem
    {
        public string   id;
        public string   title;
        public string   description;
        public string   difficulty;
        public string   status;      // active | archived | draft
        public string[] tags;
        public int      matchCount;
        public string   createdAt;
    }

    [Serializable]
    public class CaseDetails : CaseListItem
    {
        public string victimName;
        public int    victimAge;
        public string victimOccupation;
        public string victimBackstory;
        public string causeOfDeath;
        public string timeOfDeath;
        public string locationFound;
        public int    suspectCount;
        public int    witnessCount;
        public int    evidenceCount;
    }

    [Serializable]
    public class Suspect
    {
        public string   id;
        public string   name;
        public int      age;
        public string   occupation;
        public string   backstory;
        public string   personality;
        public string   alibi;
        public string   relationship;  // to victim
    }

    [Serializable]
    public class Witness
    {
        public string   id;
        public string   name;
        public int      age;
        public string   occupation;
        public string   backstory;
        public string   personality;
        public string   alibi;
        public string[] knownFacts;
        public string   relationship;  // to victim
    }

    [Serializable]
    public class Evidence
    {
        public string id;
        public string type;           // physical | digital | document | forensic | testimonial | environmental
        public string name;
        public string description;
        public string location;
        public string imageUrl;
        public string discoveredAt;
        public string notes;
        public bool   isShared;
    }

    [Serializable]
    public class TimelineEvent
    {
        public string id;
        public string timestamp;
        public string description;
        public string location;
        public bool   isKeyEvent;
        public int    order;
    }

    [Serializable]
    public class CaseSolution
    {
        public string killerId;
        public string killerName;
        public string motive;
        public string weapon;
        public string location;
        public string timeline;
        public string method;
        public string narrative;
    }

    [Serializable]
    public class CaseStats
    {
        public string caseId;
        public int    totalMatches;
        public float  solveRate;
        public float  avgSolveTime;
        public float  avgScore;
    }

    // ==================================================
    // MATCHES
    // ==================================================

    [Serializable]
    public class Match
    {
        public string   id;
        public string   roomId;
        public string   caseId;
        public string   difficulty;
        public string   status;          // waiting | in_progress | conclusion | finished
        public string   phase;           // investigation | final_minutes | submission | reveal
        public string[] playerIds;
        public int      maxPlayers;
        public string   startedAt;
        public string   endedAt;
        public int      durationSeconds;
        public int      timeRemainingSeconds;
        public string   winnerId;
    }

    [Serializable]
    public class MatchPlayer
    {
        public string playerId;
        public string username;
        public string avatarUrl;
        public string tier;
        public bool   hasSubmitted;
        public int    score;
    }

    [Serializable]
    public class MatchScore
    {
        public string          playerId;
        public string          username;
        public string          avatarUrl;
        public int             totalScore;
        public ScoreBreakdown  breakdown;
        public int             timeBonus;
        public bool            isCorrect;
        public int             rank;
        public int             rpChange;
        public string          result;    // win | lose | draw
    }

    [Serializable]
    public class ScoreBreakdown
    {
        public int killer;
        public int motive;
        public int weapon;
        public int location;
        public int timeline;
        public int narrative;
    }

    [Serializable]
    public class PlayerConclusion
    {
        public string playerId;
        public string matchId;
        public string caseId;
        public string killerId;
        public string motive;
        public string weapon;
        public string location;
        public string timeline;
        public string narrative;
    }

    [Serializable]
    public class InvestigationProgress
    {
        public string   playerId;
        public string   matchId;
        public string[] discoveredEvidenceIds;
        public string[] interrogatedWitnessIds;
        public int      hintsUsed;
        public bool     hasSubmitted;
        public int      timeSpent;
    }

    [Serializable]
    public class RecentMatch
    {
        public string matchId;
        public string caseTitle;
        public string difficulty;
        public int    score;
        public bool   isCorrect;
        public int    rank;
        public int    rpChange;
        public string endedAt;
        public int    durationSeconds;
    }

    // ==================================================
    // RANKING
    // ==================================================

    [Serializable]
    public class LeaderboardEntry
    {
        public int    rank;
        public string playerId;
        public string username;
        public string avatarUrl;
        public int    points;
        public int    wins;
        public string tier;
    }

    [Serializable]
    public class SeasonInfo
    {
        public string id;
        public int    number;
        public string name;
        public string startDate;
        public string endDate;
        public bool   isActive;
        public int    daysRemaining;
        public int    playerCount;
    }

    [Serializable]
    public class PlayerRankInfo
    {
        public string playerId;
        public string username;
        public string tier;
        public int    points;
        public int    peakPoints;
        public int    wins;
        public int    losses;
        public float  winRate;
        public int    streak;
        public int    globalRank;
        public int    season;
    }

    [Serializable]
    public class MatchHistoryEntry
    {
        public string matchId;
        public int    score;
        public bool   isCorrect;
        public int    rank;
        public int    rpChange;
        public string endedAt;
        public string difficulty;
    }

    // ==================================================
    // ANALYTICS
    // ==================================================

    [Serializable]
    public class PlayerReport
    {
        public string playerId;
        public string username;
        public int    totalMatches;
        public int    totalWins;
        public float  winRate;
        public int    avgScore;
        public int    highScore;
        public int    perfectSolves;
        public string recentTrend;    // improving | declining | stable
    }

    [Serializable]
    public class AccuracyBreakdown
    {
        public int   totalSubmissions;
        public float killerAccuracy;
        public float motiveAccuracy;
        public float weaponAccuracy;
        public float locationAccuracy;
        public float fullCorrectRate;
        public int   avgScore;
    }

    [Serializable]
    public class TrendPoint
    {
        public string matchId;
        public int    score;
        public bool   isCorrect;
        public string endedAt;
        public string difficulty;
    }

    [Serializable]
    public class MatchSummary
    {
        public string matchId;
        public string caseTitle;
        public string difficulty;
        public string winnerId;
        public int    totalPlayers;
        public int    solveCount;
        public float  avgScore;
        public int    durationSeconds;
    }

    [Serializable]
    public class GlobalStats
    {
        public int   totalMatches;
        public int   activePlayers;
        public float avgSolveRate;
        public float avgMatchDuration;
    }

    // ==================================================
    // NOTEBOOK
    // ==================================================

    [Serializable]
    public class NotebookEntry
    {
        public string id;
        public string content;
        public string relatedEvidenceId;
        public string createdAt;
    }

    // ==================================================
    // GENERIC API WRAPPERS
    // ==================================================

    [Serializable]
    public class ApiError
    {
        public string error;
        public string code;
    }

    [Serializable]
    public class ApiSuccess
    {
        public bool   success;
        public string message;
    }

    [Serializable]
    public class PaginationInfo
    {
        public int page;
        public int pageSize;
        public int total;
        public int totalPages;
    }
}

// ==================================================
// Socket payload types (used by SocketManager)
// ==================================================

namespace DetectiveRoyale.Core.Models
{
<<<<<<< HEAD
    [System.Serializable]
=======
    [Serializable]
>>>>>>> 6b8ce8b273d3571a7432b4fb850889e05f20d6a7
    public class ConclusionPayload
    {
        public string killerId;
        public string motive;
        public string weapon;
        public string location;
        public string timeline;
        public string narrative;
    }
}
