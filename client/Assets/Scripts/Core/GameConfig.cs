// ============================================
// GameConfig — centralised configuration
// ============================================
using UnityEngine;

namespace DetectiveRoyale.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Detective Royale/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Server")]
        public string ServerUrl = "http://localhost:3000";
        public string SocketUrl = "http://localhost:3000";

        [Header("API")]
        public string ApiBase => $"{ServerUrl}/api";

        [Header("Timeouts (seconds)")]
        public float HttpTimeoutSeconds = 15f;
        public float SocketReconnectDelay = 3f;
        public int   SocketMaxReconnects  = 5;

        [Header("Match")]
        public int MaxPlayers = 4;
        public int HintCooldownSeconds = 120;
        public int MaxHints = 3;

        // Singleton accessor — loaded from Resources/GameConfig
        private static GameConfig _instance;
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                {
                    // Fallback defaults if asset not found
                    _instance = CreateInstance<GameConfig>();
                    Debug.LogWarning("[GameConfig] GameConfig asset not found in Resources — using defaults.");
                }
                return _instance;
            }
        }
    }
}
