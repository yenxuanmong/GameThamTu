// ============================================
// GameConstants — all magic numbers and shared constants
// ============================================
namespace DetectiveRoyale.Core
{
    public static class GameConstants
    {
        // ---- Match ----
        public const int   MAX_PLAYERS           = 4;
        public const int   MIN_PLAYERS           = 2;
        public const int   DEFAULT_MATCH_DURATION = 1800;  // 30 min in seconds
        public const int   FINAL_MINUTES_THRESHOLD = 300;  // 5 min remaining
        public const int   MAX_HINTS             = 3;
        public const int   HINT_COOLDOWN_SECONDS = 120;    // 2 min between hints
        public const int   MAX_NOTE_LENGTH       = 300;
        public const int   MAX_MESSAGE_LENGTH    = 500;
        public const int   MAX_ROOM_NAME_LENGTH  = 32;

        // ---- Scoring ----
        public const int   MAX_SCORE_KILLER      = 200;
        public const int   MAX_SCORE_MOTIVE      = 150;
        public const int   MAX_SCORE_WEAPON      = 150;
        public const int   MAX_SCORE_LOCATION    = 150;
        public const int   MAX_SCORE_TIMELINE    = 150;
        public const int   MAX_SCORE_NARRATIVE   = 200;
        public const int   MAX_TIME_BONUS        = 300;
        public const int   PERFECT_SOLVE_BONUS   = 100;

        // ---- Ranking ----
        public const int   RP_WIN_BASE           = 25;
        public const int   RP_LOSS_BASE          = -15;
        public const int   RP_PERFECT_BONUS      = 10;
        public const int   TIER_ROOKIE_MAX       = 999;
        public const int   TIER_DETECTIVE_MIN    = 1000;
        public const int   TIER_DETECTIVE_MAX    = 1999;
        public const int   TIER_INSPECTOR_MIN    = 2000;
        public const int   TIER_INSPECTOR_MAX    = 2999;
        public const int   TIER_SERGEANT_MIN     = 3000;
        public const int   TIER_SERGEANT_MAX     = 3999;
        public const int   TIER_LIEUTENANT_MIN   = 4000;
        public const int   TIER_LIEUTENANT_MAX   = 4999;
        public const int   TIER_CAPTAIN_MIN      = 5000;
        public const int   TIER_CAPTAIN_MAX      = 5999;
        public const int   TIER_COMMISSIONER_MIN = 6000;

        // ---- UI ----
        public const float TOAST_DEFAULT_DURATION   = 3f;
        public const float TOAST_HINT_DURATION      = 6f;
        public const float TOAST_ERROR_DURATION     = 5f;
        public const float FADE_TRANSITION_DURATION = 0.35f;
        public const float LOADING_MIN_DISPLAY_TIME = 0.5f;

        // ---- Networking ----
        public const float HTTP_TIMEOUT_SECONDS   = 15f;
        public const float SOCKET_RECONNECT_DELAY = 3f;
        public const int   SOCKET_MAX_RECONNECTS  = 5;
        public const float PING_INTERVAL_SECONDS  = 10f;

        // ---- NPC ----
        public const int   MAX_WITNESS_MESSAGES     = 50;
        public const float NPC_RESPONSE_TIMEOUT     = 30f;

        // ---- Difficulties (must match backend) ----
        public static readonly string[] DIFFICULTIES =
            { "easy", "medium", "hard", "expert", "nightmare" };

        // ---- Evidence types (must match backend) ----
        public static readonly string[] EVIDENCE_TYPES =
            { "physical", "digital", "document", "forensic", "testimonial", "environmental" };

        // ---- Motives (must match backend) ----
        public static readonly string[] MOTIVES =
        {
            "jealousy", "greed", "revenge", "love", "fear",
            "power", "blackmail", "inheritance", "rivalry",
            "self_defense", "ideology", "other"
        };

        // ---- Weapons (must match backend) ----
        public static readonly string[] WEAPONS =
        {
            "knife", "gun", "poison", "blunt_object", "strangulation",
            "drowning", "fire", "explosion", "fall", "other"
        };

        // ---- Locations (must match backend) ----
        public static readonly string[] LOCATIONS =
        {
            "living_room", "kitchen", "bedroom", "bathroom", "garden",
            "garage", "basement", "attic", "office", "library",
            "dining_room", "hallway", "cellar", "rooftop", "other"
        };

        // ---- Scene names ----
        public static class Scenes
        {
            public const string SPLASH       = "Splash";
            public const string MAIN_MENU    = "MainMenu";
            public const string LOBBY        = "Lobby";
            public const string INVESTIGATION= "Investigation";
            public const string RESULTS      = "Results";
        }

        // ---- PlayerPrefs keys ----
        public static class Prefs
        {
            public const string ACCESS_TOKEN  = "dr_access_token";
            public const string REFRESH_TOKEN = "dr_refresh_token";
            public const string PLAYER_JSON   = "dr_player_json";
            public const string LANGUAGE      = "dr_language";
            public const string VOL_MASTER    = "vol_master";
            public const string VOL_MUSIC     = "vol_music";
            public const string VOL_SFX       = "vol_sfx";
            public const string QUALITY       = "graphics_quality";
            public const string SHOW_TIMER    = "show_timer";
            public const string AUTO_NOTES    = "auto_notes";
            public const string TEXT_SIZE     = "text_size";
            public const string TUTORIAL_DONE = "tutorial_done";
            public const string UNREAD_NOTIFS = "unread_notifs";
        }

        // ---- Tier helper ----
        public static string GetTierForPoints(int points) => points switch
        {
            < 1000 => "rookie",
            < 2000 => "detective",
            < 3000 => "inspector",
            < 4000 => "sergeant",
            < 5000 => "lieutenant",
            < 6000 => "captain",
            _      => "commissioner"
        };
    }
}
