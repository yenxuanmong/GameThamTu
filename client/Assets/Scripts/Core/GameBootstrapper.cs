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
            EnsureManager<Authentication.AuthAPI>("AuthAPI");

            // Multiplayer
            EnsureManager<Multiplayer.NetworkManager>("NetworkManager");
            EnsureManager<Multiplayer.RoomManager>("RoomManager");
            EnsureManager<Multiplayer.ReconnectManager>("ReconnectManager");

            // Audio
            EnsureManager<AudioManager>("AudioManager");
            EnsureManager<SceneAudioController>("SceneAudioController");

            // UI globals
            EnsureManager<UI.NotificationToast>("NotificationToast");
            EnsureManager<UI.LoadingScreen>("LoadingScreen");
            EnsureManager<UI.FadeTransition>("FadeTransition");
            EnsureManager<UI.ConfirmDialog>("ConfirmDialog");
            EnsureManager<UI.PasswordInputDialog>("PasswordInputDialog");
            EnsureManager<UI.TooltipSystem>("TooltipSystem");

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
