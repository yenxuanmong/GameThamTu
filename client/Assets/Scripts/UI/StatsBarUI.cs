// ============================================
// StatsBarUI — animated progress bar for stats display
// Used in ProfileUI, ResultUI, and RankingUI
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class StatsBarUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Slider   _bar;
        [SerializeField] private TMP_Text _valueLabel;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private Image    _fillImage;

        [Header("Animation")]
        [SerializeField] private float _animDuration   = 0.8f;
        [SerializeField] private bool  _animateOnEnable = false;

        [Header("Colors")]
        [SerializeField] private Gradient _colorGradient;

        private float     _targetValue;
        private Coroutine _anim;

        void Awake()
        {
            // Guarantee the Slider's range is 0–1 from the start
            if (_bar != null)
            {
                _bar.minValue = 0f;
                _bar.maxValue = 1f;
            }
        }

        void OnEnable()
        {
            if (_animateOnEnable)
                AnimateTo(_targetValue);
        }

        // ---- Public API ----

        /// <summary>
        /// Full setup: title, raw value and its maximum.
        /// Handles max-normalisation internally so callers never need to
        /// pre-divide.  Optional format string is forwarded to the label
        /// (e.g. "{0:F0} pts").
        /// </summary>
        public void Setup(string title, float value, float max = 1f, string format = null)
        {
            if (_titleLabel) _titleLabel.text = title;
            // Don't call SetValue here to avoid double-normalising max
            SetValue(value, max, format);
        }

        /// <summary>
        /// Update the bar value.  Pass the raw value and its maximum;
        /// normalisation to 0–1 is done here.
        /// </summary>
        public void SetValue(float value, float max = 1f, string format = null)
        {
            _targetValue = (max > 0f) ? Mathf.Clamp01(value / max) : 0f;

            // Always keep the Slider in 0-1 space
            if (_bar != null)
                _bar.maxValue = 1f;

            if (_valueLabel)
            {
                _valueLabel.text = format != null
                    ? string.Format(format, value)
                    : FormatAuto(value, max);
            }

            if (_anim != null) StopCoroutine(_anim);

            float from = (_bar != null) ? _bar.value : 0f;
            _anim = StartCoroutine(AnimateCoroutine(from, _targetValue));
            UpdateColor(_targetValue);
        }

        /// <summary>Jump to a normalised value (0–1) without animation.</summary>
        public void SetImmediate(float normalizedValue)
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            _targetValue = Mathf.Clamp01(normalizedValue);
            if (_bar) _bar.value = _targetValue;
            UpdateColor(_targetValue);
        }

        // ---- Animation ----

        public void AnimateTo(float normalizedValue)
        {
            _targetValue = Mathf.Clamp01(normalizedValue);
            if (_anim != null) StopCoroutine(_anim);
            float from = (_bar != null) ? _bar.value : 0f;
            _anim = StartCoroutine(AnimateCoroutine(from, _targetValue));
        }

        private IEnumerator AnimateCoroutine(float from, float to)
        {
            // Guard: nothing to animate if there is no bar
            if (_bar == null) yield break;

            float t = 0f;
            while (t < _animDuration)
            {
                t += Time.deltaTime;
                float current = Mathf.SmoothStep(from, to, t / _animDuration);
                _bar.value = current;
                UpdateColor(current);
                yield return null;
            }
            _bar.value = to;
            UpdateColor(to);
            _anim = null;
        }

        private void UpdateColor(float normalizedValue)
        {
            if (_fillImage == null) return;
            if (_colorGradient != null && _colorGradient.colorKeys.Length > 0)
                _fillImage.color = _colorGradient.Evaluate(normalizedValue);
        }

        private static string FormatAuto(float value, float max)
        {
            // If max ≤ 1 treat the value as a fraction and show as percentage
            if (max <= 1f) return $"{value * 100:F0}%";
            return $"{value:F0}/{max:F0}";
        }
    }
}
