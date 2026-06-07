// ============================================
// GameBootstrapper — creates ALL singleton managers at game start
// Attach to a single GameObject in the FIRST scene (Splash/MainMenu)
// ============================================
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private static bool _initialized;

        void Awake()
        {
            if (_initialized) { Destroy(gameObject); return; }
            _initialized = true;
            DontDestroyOnLoad(gameObject);

            // Core singletons
            EnsureManager<UnityMainThreadDispatcher>("Dispatcher");
            EnsureManager<CoroutineRunner>("CoroutineRunner");
            EnsureManager<AuthState>("AuthState");
            EnsureManager<ApiClient>("ApiClient");
            EnsureManager<SocketManager>("SocketManager");
            EnsureManager<SceneLoader>("SceneLoader");
            EnsureManager<LocalizationManager>("LocalizationManager");

            // Auth
            EnsureManager<AuthAPI>("AuthAPI");

            // Multiplayer
            EnsureManager<NetworkManager>("NetworkManager");
            EnsureManager<RoomManager>("RoomManager");
            EnsureManager<ReconnectManager>("ReconnectManager");

            // Audio
            EnsureManager<AudioManager>("AudioManager");
            EnsureManager<SceneAudioController>("SceneAudioController");

            // UI globals
            EnsureManager<NotificationToast>("NotificationToast");
            EnsureManager<LoadingScreen>("LoadingScreen");
            EnsureManager<FadeTransition>("FadeTransition");
            EnsureManager<ConfirmDialog>("ConfirmDialog");
            EnsureManager<PasswordInputDialog>("PasswordInputDialog");
            EnsureManager<TooltipSystem>("TooltipSystem");

            // Version check
            EnsureManager<VersionChecker>("VersionChecker");

            // Input
            EnsureManager<InputManager>("InputManager");

            Debug.Log("[Bootstrapper] All core managers initialised");
        }

        private static T EnsureManager<T>(string goName) where T : MonoBehaviour
        {
            T existing = FindFirstObjectByType<T>();
            if (existing != null) return existing;
            var go = new GameObject(goName);
            DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }
    }
}
