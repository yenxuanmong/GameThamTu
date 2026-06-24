// ============================================
// CountdownTimer — visual countdown widget
// Can be used for match countdown or action cooldowns
// ============================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class CountdownTimer : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Image    _radialFill;      // optional circular fill
        [SerializeField] private Image    _barFill;         // optional linear fill

        [Header("Colors")]
        [SerializeField] private Color _normalColor  = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private int   _warningThreshold  = 300;  // 5 min
        [SerializeField] private int   _criticalThreshold = 60;   // 1 min

        [Header("Pulse on critical")]
        [SerializeField] private bool  _pulseOnCritical = true;
        [SerializeField] private float _pulseSpeed = 3f;

        private int      _total;
        private int      _remaining;
        private bool     _running;
        private Coroutine _coroutine;

        public event Action OnExpired;

        // ============================================
        // Control
        // ============================================

        public void StartCountdown(int totalSeconds)
        {
            _total     = totalSeconds;
            _remaining = totalSeconds;
            _running   = true;

            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(Tick());
        }

        public void SetRemaining(int seconds)
        {
            _remaining = Mathf.Max(0, seconds);
            UpdateDisplay();
        }

        public void Pause()  => _running = false;
        public void Resume() => _running = true;

        public void Stop()
        {
            _running = false;
            if (_coroutine != null) { StopCoroutine(_coroutine); _coroutine = null; }
        }

        // ============================================
        // Tick
        // ============================================

        private IEnumerator Tick()
        {
            while (_remaining > 0)
            {
                UpdateDisplay();
                yield return new WaitForSeconds(1f);
                if (_running) _remaining--;
            }
            _remaining = 0;
            UpdateDisplay();
            OnExpired?.Invoke();
        }

        // ============================================
        // Display
        // ============================================

        private void Update()
        {
            if (!_running || !_pulseOnCritical || _remaining > _criticalThreshold) return;
            float pulse = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            if (_timerText) _timerText.alpha = Mathf.Lerp(0.4f, 1f, pulse);
        }

        private void UpdateDisplay()
        {
            int m = _remaining / 60;
            int s = _remaining % 60;

            if (_timerText)
            {
                _timerText.text  = $"{m:D2}:{s:D2}";
                _timerText.color = _remaining <= _criticalThreshold ? _criticalColor
                                 : _remaining <= _warningThreshold  ? _warningColor
                                 : _normalColor;
            }

            float ratio = _total > 0 ? (float)_remaining / _total : 0f;
            if (_radialFill) _radialFill.fillAmount = ratio;
            if (_barFill)    _barFill.fillAmount    = ratio;
        }
    }
}
