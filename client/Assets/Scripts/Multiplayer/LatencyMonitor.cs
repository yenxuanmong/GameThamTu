// ============================================
// LatencyMonitor — ping display and network quality indicator
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    public class LatencyMonitor : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text  _pingText;
        [SerializeField] private Image     _signalIcon;
        [SerializeField] private float     _updateInterval = 5f;

        [Header("Colors")]
        [SerializeField] private Color _goodColor   = Color.green;
        [SerializeField] private Color _medColor    = Color.yellow;
        [SerializeField] private Color _badColor    = Color.red;

        [Header("Thresholds (ms)")]
        [SerializeField] private int _goodThreshold = 80;
        [SerializeField] private int _badThreshold  = 200;

        private float _lastPingTime;
        private int   _latencyMs;

        void Start()
        {
            SocketManager.Instance.OnConnected.AddListener(StartPing);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance != null)
                SocketManager.Instance.OnConnected.RemoveListener(StartPing);
        }

        private void StartPing() => StartCoroutine(PingLoop());

        private IEnumerator PingLoop()
        {
            while (SocketManager.Instance?.IsConnected ?? false)
            {
                yield return new WaitForSeconds(_updateInterval);
                SendPing();
            }
        }

        private void SendPing()
        {
            _lastPingTime = Time.realtimeSinceStartup;
            SocketManager.Instance?.Emit("ping", new { timestamp = _lastPingTime });
        }

        // Called by SocketManager when "pong" event received
        public void OnPong()
        {
            _latencyMs = Mathf.RoundToInt((Time.realtimeSinceStartup - _lastPingTime) * 1000f);
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_pingText)
                _pingText.text = $"{_latencyMs}ms";

            Color col = _latencyMs < _goodThreshold ? _goodColor
                      : _latencyMs < _badThreshold  ? _medColor
                      : _badColor;

            if (_pingText)   _pingText.color   = col;
            if (_signalIcon) _signalIcon.color = col;
        }
    }
}
