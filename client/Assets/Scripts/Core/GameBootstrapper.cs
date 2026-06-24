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

            // Core singletons (all in DetectiveRoyale.Core assembly)
            EnsureManager<UnityMainThreadDispatcher>("Dispatcher");
            EnsureManager<CoroutineRunner>("CoroutineRunner");
            EnsureManager<AuthState>("AuthState");
            EnsureManager<ApiClient>("ApiClient");
            EnsureManager<SocketManager>("SocketManager");
            EnsureManager<SceneLoader>("SceneLoader");
            EnsureManager<SceneTransitionManager>("SceneTransitionManager");
            EnsureManager<LocalizationManager>("LocalizationManager");
            EnsureManager<AudioManager>("AudioManager");
            EnsureManager<SceneAudioController>("SceneAudioController");
            EnsureManager<VersionChecker>("VersionChecker");
            EnsureManager<InputManager>("InputManager");
            EnsureManager<ScreenshotManager>("ScreenshotManager");
            EnsureManager<ClientErrorHandler>("ClientErrorHandler");

            // Other managers (different assemblies) are initialised by their
            // own scene GameObjects — see SETUP.md for scene-by-scene setup.

            Debug.Log("[Bootstrapper] Core managers initialised");
        }

        private static T EnsureManager<T>(string goName) where T : MonoBehaviour
        {
            T existing = FindAnyObjectByType<T>();
            if (existing != null) return existing;
            var go = new GameObject(goName);
            DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }
    }
}
