// ============================================
// GameFlow — high-level game state transitions
// Single place for: match start → investigation → results
// ============================================
using System.Collections;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    /// <summary>
    /// Singleton that owns the top-level game flow transitions.
    /// Other systems fire events; GameFlow decides scene changes.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            SubscribeEvents();
        }

        void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ============================================
        // Event subscriptions
        // ============================================

        private void SubscribeEvents()
        {
            SocketManager.Instance.OnMatchStarted.AddListener(OnMatchStarted);
            SocketManager.Instance.OnMatchEnded.AddListener(OnMatchEnded);
            AuthState.Instance.OnLogout.AddListener(OnLoggedOut);
        }

        private void UnsubscribeEvents()
        {
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.OnMatchStarted.RemoveListener(OnMatchStarted);
                SocketManager.Instance.OnMatchEnded.RemoveListener(OnMatchEnded);
            }
            if (AuthState.Instance != null)
                AuthState.Instance.OnLogout.RemoveListener(OnLoggedOut);
        }

        // ============================================
        // Match start
        // ============================================

        private void OnMatchStarted(MatchStartedPayload p)
        {
            GameSession.MatchId = p.matchId;
            GameSession.CaseId  = p.caseId;
            Debug.Log($"[GameFlow] Match started → {p.matchId}");
            SceneTransitionManager.Instance?.GoToInvestigation(
                p.matchId, p.caseId, GameSession.RoomId);
        }

        // ============================================
        // Match end
        // ============================================

        private void OnMatchEnded(MatchEndedPayload p)
        {
            Debug.Log($"[GameFlow] Match ended. Winner: {p.winnerId}");
            StartCoroutine(GoToResultsDelayed(1.5f));
        }

        private IEnumerator GoToResultsDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneTransitionManager.Instance?.GoToResults();
        }

        // ============================================
        // Logout
        // ============================================

        private void OnLoggedOut()
        {
            GameSession.Reset();
            SceneTransitionManager.Instance?.GoToMainMenu();
        }

        // ============================================
        // Manual triggers (called from UI)
        // ============================================

        public void ReturnToLobby()
        {
            if (!string.IsNullOrEmpty(GameSession.RoomId))
                SocketManager.Instance?.LeaveRoom(GameSession.RoomId);
            GameSession.Reset();
            SceneTransitionManager.Instance?.GoToLobby();
        }

        public void ReturnToMainMenu()
        {
            if (!string.IsNullOrEmpty(GameSession.RoomId))
                SocketManager.Instance?.LeaveRoom(GameSession.RoomId);
            GameSession.Reset();
            SceneTransitionManager.Instance?.GoToMainMenu();
        }

        public void PlayAgain()
        {
            GameSession.Reset();
            SceneTransitionManager.Instance?.GoToLobby();
        }
    }
}
