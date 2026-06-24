// ============================================
// AppStateManager — tracks global application state
// Handles app focus/pause, background handling, deep links
// ============================================
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DetectiveRoyale.Core
{
    public class AppStateManager : MonoBehaviour
    {
        public static AppStateManager Instance { get; private set; }

        public enum AppState { Loading, MainMenu, Lobby, InMatch, Results }
        public AppState CurrentState { get; private set; } = AppState.Loading;

        public static event Action<AppState> OnStateChanged;
        public static event Action<bool>     OnAppFocusChanged;   // true = gained focus
        public static event Action<bool>     OnAppPauseChanged;   // true = paused

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            OnAppFocusChanged?.Invoke(hasFocus);
            if (hasFocus && CurrentState == AppState.InMatch)
                SocketManager.Instance?.Connect();
        }

        void OnApplicationPause(bool paused)
        {
            OnAppPauseChanged?.Invoke(paused);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AppState newState = scene.name switch
            {
                SceneLoader.SCENE_MAIN_MENU    => AppState.MainMenu,
                SceneLoader.SCENE_LOBBY        => AppState.Lobby,
                SceneLoader.SCENE_INVESTIGATION=> AppState.InMatch,
                SceneLoader.SCENE_RESULTS      => AppState.Results,
                _                              => AppState.Loading
            };

            if (newState == CurrentState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[AppState] → {newState}");
        }

        public bool IsInMatch   => CurrentState == AppState.InMatch;
        public bool IsInLobby   => CurrentState == AppState.Lobby;
        public bool IsInMainMenu=> CurrentState == AppState.MainMenu;
    }
}
