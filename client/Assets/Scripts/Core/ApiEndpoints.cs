// ============================================
// ApiEndpoints — centralised API path constants
// Mirrors every backend route exactly
// ============================================
namespace DetectiveRoyale.Core
{
    public static class Api
    {
        // ---- Auth (/api/auth) ----
        public static class Auth
        {
            public const string Register       = "/auth/register";
            public const string Login          = "/auth/login";
            public const string Logout         = "/auth/logout";
            public const string Refresh        = "/auth/refresh";
            public const string Me             = "/auth/me";
            public const string ChangePassword = "/auth/me/change-password";
            public const string Avatar         = "/auth/me/avatar";
            public const string ForgotPassword = "/auth/forgot-password";
            public const string ResetPassword  = "/auth/reset-password";
            public const string VerifyReset    = "/auth/reset-password/verify";
        }

        // ---- Rooms (/api/rooms) ----
        public static class Rooms
        {
            public const string List    = "/rooms";
            public static string ById(string id)      => $"/rooms/{id}";
            public static string Players(string id)   => $"/rooms/{id}/players";
            public static string Delete(string id)    => $"/rooms/{id}";
        }

        // ---- Cases (/api/cases) ----
        public static class Cases
        {
            public const string List                          = "/cases";
            public static string ById(string id)             => $"/cases/{id}";
            public static string Suspects(string id)         => $"/cases/{id}/suspects";
            public static string Witnesses(string id)        => $"/cases/{id}/witnesses";
            public static string Evidence(string id)         => $"/cases/{id}/evidence";
            public static string Timeline(string id)         => $"/cases/{id}/timeline";
            public static string Solution(string id)         => $"/cases/{id}/solution";
            public static string Stats(string id)            => $"/cases/{id}/stats";
            public static string SolutionForMatch(string caseId, string matchId)
                => $"/cases/{caseId}/solution?matchId={matchId}";
        }

        // ---- Matches (/api/matches) ----
        public static class Matches
        {
            public const string MyRecent                       = "/matches/me";
            public static string ById(string id)              => $"/matches/{id}";
            public static string Players(string id)           => $"/matches/{id}/players";
            public static string Scores(string id)            => $"/matches/{id}/scores";
            public static string Progress(string id)          => $"/matches/{id}/progress";
        }

        // ---- Evidence (/api/matches/:matchId/evidence) ----
        public static class Evidence
        {
            public static string List(string matchId)                       => $"/matches/{matchId}/evidence";
            public static string ById(string matchId, string evidenceId)    => $"/matches/{matchId}/evidence/{evidenceId}";
            public static string Notes(string matchId, string evidenceId)   => $"/matches/{matchId}/evidence/{evidenceId}/notes";
            public static string Share(string matchId, string evidenceId)   => $"/matches/{matchId}/evidence/{evidenceId}/share";
            public static string Notebook(string matchId)                   => $"/matches/{matchId}/evidence/notebook";
            public static string DeleteNote(string matchId, string entryId) => $"/matches/{matchId}/evidence/notebook/{entryId}";
        }

        // ---- Ranking (/api/ranking) ----
        public static class Ranking
        {
            public const string Leaderboard    = "/ranking/leaderboard";
            public const string MyRank         = "/ranking/me";
            public const string MyHistory      = "/ranking/me/history";
            public const string CurrentSeason  = "/ranking/seasons/current";
            public const string AllSeasons     = "/ranking/seasons";
            public static string PlayerRank(string playerId) => $"/ranking/player/{playerId}";
            public static string LeaderboardPaged(int page = 1, int size = 50)
                => $"/ranking/leaderboard?page={page}&pageSize={size}";
        }

        // ---- Analytics (/api/analytics) ----
        public static class Analytics
        {
            public const string MyReport   = "/analytics/me";
            public const string MyAccuracy = "/analytics/me/accuracy";
            public const string Global     = "/analytics/global";
            public static string MyTrend(int limit = 20)         => $"/analytics/me/trend?limit={limit}";
            public static string MatchSummary(string matchId)    => $"/analytics/matches/{matchId}";
            public static string CaseStats(string caseId)        => $"/analytics/cases/{caseId}";
        }

        // ---- Health ----
        public const string Health = "/health";
    }
}
