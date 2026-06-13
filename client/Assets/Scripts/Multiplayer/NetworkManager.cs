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

            if (AuthState.Instance.IsLoggedIn)
                SocketManager.Instance.Connect();

            SocketManager.Instance.OnSocketError.AddListener(OnSocketError);
            SocketManager.Instance.OnNotification.AddListener(OnNotification);
            SocketManager.Instance.OnDisconnected.AddListener(OnDisconnected);
        }

        void OnDestroy()
        {
            if (AuthState.Instance != null)
            {
                AuthState.Instance.OnLogin.RemoveListener(OnLogin);
                AuthState.Instance.OnLogout.RemoveListener(OnLogout);
            }
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.OnSocketError.RemoveListener(OnSocketError);
                SocketManager.Instance.OnNotification.RemoveListener(OnNotification);
                SocketManager.Instance.OnDisconnected.RemoveListener(OnDisconnected);
            }
        }

        private void OnLogin()  => SocketManager.Instance.Connect();
        private void OnLogout() => SocketManager.Instance.Disconnect();

        private void ShowToast(string msg)
        {
            var go = GameObject.Find("NotificationToast");
            if (go != null)
                go.SendMessage("ShowToast", msg, SendMessageOptions.DontRequireReceiver);
        }

        private void OnSocketError(SocketErrorPayload p)   => ShowToast(p.message);
        private void OnNotification(NotificationPayload p) => ShowToast(p.message);

        private void OnDisconnected()
        {
            ShowToast("Connection lost — reconnecting...");
            Invoke(nameof(TryReconnect), GameConfig.Instance.SocketReconnectDelay);
        }

        private void TryReconnect() => SocketManager.Instance.Connect();
    }
}
