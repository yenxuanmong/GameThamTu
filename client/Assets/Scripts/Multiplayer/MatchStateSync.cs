// ============================================
// MatchStateSync — syncs authoritative match state from server
// Reconciles local timer/phase with server on reconnect
// ============================================
using System.Collections;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    [System.Serializable] class MatchStateResponse { public Match match; }

    public class MatchStateSync : MonoBehaviour
    {
        [SerializeField] private float _syncIntervalSeconds = 30f;

        private Coroutine _syncCoroutine;

        void Start()
        {
            SocketManager.Instance.OnConnected.AddListener(OnReconnected);
            _syncCoroutine = StartCoroutine(PeriodicSync());
        }

        void OnDestroy()
        {
            SocketManager.Instance?.OnConnected.RemoveListener(OnReconnected);
            if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);
        }

        // ---- Sync after reconnect ----
        private void OnReconnected()
        {
            if (!string.IsNullOrEmpty(GameSession.MatchId))
                StartCoroutine(SyncOnce());
        }

        // ---- Periodic sync ----
        private IEnumerator PeriodicSync()
        {
            while (true)
            {
                yield return new WaitForSeconds(_syncIntervalSeconds);
                if (!string.IsNullOrEmpty(GameSession.MatchId))
                    yield return SyncOnce();
            }
        }

        private IEnumerator SyncOnce()
        {
            yield return ApiClient.Instance.Get<MatchStateResponse>(
                Api.Matches.ById(GameSession.MatchId),
                resp =>
                {
                    if (resp.match == null) return;
                    // Reconcile — server is authoritative
                    int serverTime = resp.match.timeRemainingSeconds;
                    int localTime  = GameSession.TimeRemaining;
                    int drift      = Mathf.Abs(serverTime - localTime);

                    if (drift > 5)
                    {
                        Debug.Log($"[MatchSync] Timer drift {drift}s — correcting {localTime}→{serverTime}");
                        GameSession.TimeRemaining = serverTime;
                    }

                    if (resp.match.phase != GameSession.CurrentPhase)
                    {
                        Debug.Log($"[MatchSync] Phase mismatch — correcting to {resp.match.phase}");
                        GameSession.CurrentPhase = resp.match.phase;
                    }
                },
                err => Debug.LogWarning($"[MatchSync] {err}"));
        }
    }
}
