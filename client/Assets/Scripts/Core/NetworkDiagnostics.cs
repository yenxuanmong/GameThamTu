// ============================================
// NetworkDiagnostics — logs connection quality, reconnect attempts
// Exposed via debug overlay in development builds
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class NetworkDiagnostics : MonoBehaviour
    {
        public static NetworkDiagnostics Instance { get; private set; }

        [Header("Debug overlay")]
        [SerializeField] private bool  _showOverlay = false;
        [SerializeField] private Color _overlayColor = new Color(0f, 1f, 0.5f, 0.9f);

        private readonly List<string> _log = new(32);
        private int    _reconnectCount;
        private float  _lastPingMs;
        private bool   _connected;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            SocketManager.Instance.OnConnected.AddListener(() => { _connected = true;  Log("Connected"); });
            SocketManager.Instance.OnDisconnected.AddListener(() => { _connected = false; Log("Disconnected"); });
            SocketManager.Instance.OnSocketError.AddListener(p => Log($"Error: {p.code} — {p.message}"));
        }

        public void RecordReconnectAttempt()
        {
            _reconnectCount++;
            Log($"Reconnect attempt #{_reconnectCount}");
        }

        public void RecordPing(float ms)
        {
            _lastPingMs = ms;
        }

        public void Log(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _log.Add(entry);
            if (_log.Count > 100) _log.RemoveAt(0);
            Debug.Log($"[Net] {entry}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!_showOverlay) return;

            GUI.color = _overlayColor;
            int y = 10;
            GUI.Label(new Rect(10, y, 300, 20), $"Socket: {(_connected ? "✓ Connected" : "✗ Disconnected")}"); y += 18;
            GUI.Label(new Rect(10, y, 300, 20), $"Ping: {_lastPingMs:F0}ms"); y += 18;
            GUI.Label(new Rect(10, y, 300, 20), $"Reconnects: {_reconnectCount}"); y += 18;
            GUI.Label(new Rect(10, y, 300, 20), $"MatchId: {GameSession.MatchId ?? "—"}"); y += 18;
            GUI.Label(new Rect(10, y, 300, 20), $"Phase: {GameSession.CurrentPhase}"); y += 18;
            GUI.Label(new Rect(10, y, 300, 20), $"Timer: {GameSession.TimeRemaining}s"); y += 24;

            // Last 5 log entries
            int start = Mathf.Max(0, _log.Count - 5);
            for (int i = start; i < _log.Count; i++)
            {
                GUI.Label(new Rect(10, y, 400, 18), _log[i]);
                y += 16;
            }
        }
#endif
    }
}
