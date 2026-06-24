// ============================================
// MatchTimerUI — dedicated match timer component
// Replaces inline timer code in InvestigationManager
// ============================================
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class MatchTimerUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text   _timerText;
        [SerializeField] private Image      _radialFill;
        [SerializeField] private Image      _linearFill;
        [SerializeField] private GameObject _warningPulse;

        [Header("Thresholds")]
        [SerializeField] private int   _warningSeconds  = 300;   // 5 min
        [SerializeField] private int   _criticalSeconds = 60;    // 1 min

        [Header("Colors")]
        [SerializeField] private Color _normalColor   = Color.white;
        [SerializeField] private Color _warningColor  = new Color(1f, 0.75f, 0.1f);
        [SerializeField] private Color _criticalColor = Color.red;

        public event Action OnTimerExpired;

        private int   _totalSeconds;
        private int   _remaining;
        private bool  _running;
        private bool  _pulsing;

        void Start()
        {
            SocketManager.Instance?.OnTimerTick.AddListener(OnServerTick);
            SocketManager.Instance?.OnPhaseChanged.AddListener(OnPhaseChanged);
            SocketManager.Instance?.OnMatchStarted.AddListener(_ => StartTimer());
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnTimerTick.RemoveListener(OnServerTick);
            SocketManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }

        void Update()
        {
            if (!_running) return;

            if (_pulsing && _warningPulse != null)
            {
                float alpha = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                var cg = _warningPulse.GetComponent<CanvasGroup>()
                      ?? _warningPulse.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
            }
        }

        // ============================================
        // Control
        // ============================================

        public void StartTimer()
        {
            _totalSeconds = GameSession.Match?.durationSeconds ?? 1800;
            _remaining    = GameSession.TimeRemaining;
            _running      = true;
            UpdateDisplay();
        }

        public void StopTimer()
        {
            _running = false;
            _pulsing = false;
            _warningPulse?.SetActive(false);
        }

        public void SetRemaining(int seconds)
        {
            _remaining = Mathf.Max(0, seconds);
            GameSession.TimeRemaining = _remaining;
            UpdateDisplay();

            if (_remaining == 0)
            {
                StopTimer();
                OnTimerExpired?.Invoke();
            }
        }

        // ============================================
        // Socket handlers
        // ============================================

        private void OnServerTick(TimerPayload p)   => SetRemaining(p.timeRemaining);
        private void OnPhaseChanged(PhasePayload p) => SetRemaining(p.timeRemaining);

        // ============================================
        // Display
        // ============================================

        private void UpdateDisplay()
        {
            int m = _remaining / 60;
            int s = _remaining % 60;

            Color col = _remaining <= _criticalSeconds ? _criticalColor
                      : _remaining <= _warningSeconds  ? _warningColor
                      : _normalColor;

            if (_timerText)
            {
                _timerText.text  = $"{m:D2}:{s:D2}";
                _timerText.color = col;
            }

            float ratio = _totalSeconds > 0 ? (float)_remaining / _totalSeconds : 0f;
            if (_radialFill) _radialFill.fillAmount = ratio;
            if (_linearFill) _linearFill.fillAmount = ratio;

            bool shouldPulse = _remaining <= _criticalSeconds;
            if (shouldPulse != _pulsing)
            {
                _pulsing = shouldPulse;
                _warningPulse?.SetActive(shouldPulse);
            }
        }
    }
}
