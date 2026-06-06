// ============================================
// NetworkManager — boots Socket.IO on login,
// handles global error/notification events
// ============================================
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            AuthState.Instance.OnLogin.AddListener(OnLogin);
            AuthState.Instance.OnLogout.AddListener(OnLogout);

            // Connect if already logged in (e.g. after scene reload)
            if (AuthState.Instance.IsLoggedIn)
                SocketManager.Instance.Connect();

            // Global socket error → toast
            SocketManager.Instance.OnSocketError.AddListener(OnSocketError);
            SocketManager.Instance.OnNotification.AddListener(OnNotification);
            SocketManager.Instance.OnDisconnected.AddListener(OnDisconnected);
        }

        void OnDestroy()
        {
            if (AuthState.Instance   != null) AuthState.Instance.OnLogin.RemoveListener(OnLogin);
            if (AuthState.Instance   != null) AuthState.Instance.OnLogout.RemoveListener(OnLogout);
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.OnSocketError.RemoveListener(OnSocketError);
                SocketManager.Instance.OnNotification.RemoveListener(OnNotification);
                SocketManager.Instance.OnDisconnected.RemoveListener(OnDisconnected);
            }
        }

        private void OnLogin()  => SocketManager.Instance.Connect();
        private void OnLogout() => SocketManager.Instance.Disconnect();

        private void OnSocketError(SocketErrorPayload p) =>
            NotificationToast.Show(p.message, "error");

        private void OnNotification(NotificationPayload p) =>
            NotificationToast.Show(p.message, p.type);

        private void OnDisconnected()
        {
            NotificationToast.Show("Connection lost — reconnecting...", "warning");
            Invoke(nameof(TryReconnect), GameConfig.Instance.SocketReconnectDelay);
        }

        private void TryReconnect() => SocketManager.Instance.Connect();
    }
}
