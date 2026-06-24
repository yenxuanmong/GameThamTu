// ============================================
// ReconnectManager — auto-reconnect + session restore
// ============================================
using System.Collections;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    public class ReconnectManager : MonoBehaviour
    {
        private int   _attempts;
        private bool  _reconnecting;

        void Start()
        {
            SocketManager.Instance.OnDisconnected.AddListener(OnDisconnected);
            SocketManager.Instance.OnConnected.AddListener(OnConnected);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnDisconnected.RemoveListener(OnDisconnected);
            SocketManager.Instance.OnConnected.RemoveListener(OnConnected);
        }

        private void OnDisconnected()
        {
            if (_reconnecting) return;
            _attempts = 0;
            _reconnecting = true;
            StartCoroutine(ReconnectLoop());
        }

        private void OnConnected()
        {
            _reconnecting = false;
            _attempts     = 0;

            // If we were mid-match, rejoin match room
            if (!string.IsNullOrEmpty(GameSession.MatchId))
            {
                Debug.Log("[Reconnect] Rejoining match room");
                SocketManager.Instance.JoinRoom(GameSession.RoomId);
            }
        }

        private IEnumerator ReconnectLoop()
        {
            int maxAttempts = GameConfig.Instance.SocketMaxReconnects;
            float delay     = GameConfig.Instance.SocketReconnectDelay;

            while (_reconnecting && _attempts < maxAttempts)
            {
                _attempts++;
                Debug.Log($"[Reconnect] Attempt {_attempts}/{maxAttempts}");

                // Refresh token first
                bool tokenOk = false;
                yield return StartCoroutine(AuthState.Instance.TryRefreshToken(ok => tokenOk = ok));

                if (!tokenOk)
                {
                    Debug.LogWarning("[Reconnect] Token refresh failed — giving up");
                    _reconnecting = false;
                    NotificationToast.Show("Session expired. Please log in again.", "error");
                    SceneLoader.Instance.LoadScene(SceneLoader.SCENE_MAIN_MENU);
                    yield break;
                }

                SocketManager.Instance.Connect();
                yield return new WaitForSeconds(delay * _attempts); // back-off
            }

            if (_reconnecting)
            {
                _reconnecting = false;
                NotificationToast.Show("Could not reconnect. Please check your internet.", "error");
            }
        }
    }
}
