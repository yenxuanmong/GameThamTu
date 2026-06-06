// ============================================
// GameSession — static in-match state shared across scenes
// ============================================
using System.Collections.Generic;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Core
{
    public static class GameSession
    {
        // Set before loading Investigation scene
        public static string MatchId;
        public static string CaseId;
        public static string RoomId;
        public static string Difficulty;

        // Populated after scene loads
        public static CaseDetails        Case;
        public static Suspect[]          Suspects;
        public static Witness[]          Witnesses;
        public static Evidence[]         DiscoveredEvidence = new Evidence[0];
        public static TimelineEvent[]    Timeline;
        public static Match              Match;

        // Investigation state
        public static int   TimeRemaining;
        public static string CurrentPhase = "investigation";
        public static int   HintsRemaining = 3;

        public static void Reset()
        {
            MatchId           = null;
            CaseId            = null;
            RoomId            = null;
            Difficulty        = null;
            Case              = null;
            Suspects          = null;
            Witnesses         = null;
            DiscoveredEvidence= new Evidence[0];
            Timeline          = null;
            Match             = null;
            TimeRemaining     = 0;
            CurrentPhase      = "investigation";
            HintsRemaining    = 3;
        }
    }
}
