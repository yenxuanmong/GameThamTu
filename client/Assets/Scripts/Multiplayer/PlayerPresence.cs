// ============================================
// PlayerPresence — tracks online/in-match status of players
// Updates profile status via REST, shows presence indicators in UI
// ============================================
using System.Collections;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    [System.Serializable] class PresenceBody { public string status; }
    [System.Serializable] class PresenceResp { public bool success; }

    public class PlayerPresence : MonoBehaviour
    {
        public static PlayerPresence Instance { get; private set; }

        [SerializeField] private float _heartbeatInterval = 60f;  // update status every minute

        public enum Status { Online, InMatch, Away, Offline }

        public Status CurrentStatus { get; private set; } = Status.Offline;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            AuthState.Instance.OnLogin.AddListener(() => SetStatus(Status.Online));
            AuthState.Instance.OnLogout.AddListener(() => SetStatus(Status.Offline));
            SocketManager.Instance.OnMatchStarted.AddListener(_ => SetStatus(Status.InMatch));
            SocketManager.Instance.OnMatchEnded.AddListener(_ => SetStatus(Status.Online));

            if (AuthState.Instance.IsLoggedIn)
            {
                SetStatus(Status.Online);
                StartCoroutine(HeartbeatLoop());
            }
        }

        void OnDestroy()
        {
            if (AuthState.Instance != null)
            {
                AuthState.Instance.OnLogin.RemoveListener(() => SetStatus(Status.Online));
                AuthState.Instance.OnLogout.RemoveListener(() => SetStatus(Status.Offline));
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (AuthState.Instance?.IsLoggedIn ?? false)
                SetStatus(paused ? Status.Away : Status.Online);
        }

        void OnApplicationQuit()
        {
            if (AuthState.Instance?.IsLoggedIn ?? false)
                SetStatusSync(Status.Offline);
        }

        // ============================================
        // Set status
        // ============================================

        public void SetStatus(Status status)
        {
            if (CurrentStatus == status) return;
            CurrentStatus = status;
            StartCoroutine(UpdateStatusAPI(StatusToString(status)));
        }

        private void SetStatusSync(Status status)
        {
            CurrentStatus = status;
            // Fire-and-forget (best effort on quit)
            StartCoroutine(UpdateStatusAPI(StatusToString(status)));
        }

        private IEnumerator UpdateStatusAPI(string statusStr)
        {
            if (!AuthState.Instance.IsLoggedIn) yield break;

            yield return ApiClient.Instance.Patch<PresenceResp>(
                Api.Auth.Me,
                new { status = statusStr },
                _ => { },
                _ => { });
        }

        // ============================================
        // Heartbeat to keep online status fresh
        // ============================================

        private IEnumerator HeartbeatLoop()
        {
            while (AuthState.Instance.IsLoggedIn)
            {
                yield return new WaitForSeconds(_heartbeatInterval);
                if (CurrentStatus != Status.Offline)
                    yield return UpdateStatusAPI(StatusToString(CurrentStatus));
            }
        }

        // ============================================
        // Helpers
        // ============================================

        private static string StatusToString(Status s) => s switch
        {
            Status.Online  => "online",
            Status.InMatch => "in_match",
            Status.Away    => "away",
            _              => "offline"
        };

        public static Color StatusColor(string status) => status switch
        {
            "online"   => Color.green,
            "in_match" => new Color(1f, 0.65f, 0f),
            "away"     => Color.yellow,
            _          => Color.grey
        };
    }
}
