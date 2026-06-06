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
        [SerializeField] private float _animDuration = 0.8f;
        [SerializeField] private bool  _animateOnEnable = false;

        [Header("Colors")]
        [SerializeField] private Gradient _colorGradient;

        private float _targetValue;
        private Coroutine _anim;

        void OnEnable()
        {
            if (_animateOnEnable)
                AnimateTo(_targetValue);
        }

        // ---- Public API ----

        public void Setup(string title, float value, float max = 1f, string format = null)
        {
            if (_titleLabel) _titleLabel.text = title;
            if (_bar) _bar.maxValue = max;
            SetValue(value, max, format);
        }

        public void SetValue(float value, float max = 1f, string format = null)
        {
            _targetValue = max > 0 ? value / max : 0f;
            if (_bar) _bar.maxValue = 1f;

            if (_valueLabel)
            {
                _valueLabel.text = format != null
                    ? string.Format(format, value)
                    : FormatAuto(value, max);
            }

            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateCoroutine(_bar ? _bar.value : 0f, _targetValue));
            UpdateColor(_targetValue);
        }

        public void SetImmediate(float normalizedValue)
        {
            _targetValue = Mathf.Clamp01(normalizedValue);
            if (_bar) _bar.value = _targetValue;
            UpdateColor(_targetValue);
        }

        // ---- Animation ----

        public void AnimateTo(float normalizedValue)
        {
            _targetValue = Mathf.Clamp01(normalizedValue);
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateCoroutine(_bar ? _bar.value : 0f, _targetValue));
        }

        private IEnumerator AnimateCoroutine(float from, float to)
        {
            float t = 0f;
            while (t < _animDuration)
            {
                t += Time.deltaTime;
                float current = Mathf.SmoothStep(from, to, t / _animDuration);
                if (_bar) _bar.value = current;
                UpdateColor(current);
                yield return null;
            }
            if (_bar) _bar.value = to;
            UpdateColor(to);
        }

        private void UpdateColor(float normalizedValue)
        {
            if (_fillImage == null) return;
            if (_colorGradient != null && _colorGradient.colorKeys.Length > 0)
                _fillImage.color = _colorGradient.Evaluate(normalizedValue);
        }

        private static string FormatAuto(float value, float max)
        {
            if (max <= 1f) return $"{value * 100:F0}%";
            return $"{value:F0}/{max:F0}";
        }
    }
}
