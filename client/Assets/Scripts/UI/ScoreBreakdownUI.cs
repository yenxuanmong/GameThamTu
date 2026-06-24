// ============================================
// ScoreBreakdownUI — animated per-category score bars
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class ScoreBreakdownUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Bars — max scores: killer=300, motive=200, weapon=150, location=100, timeline=150, narrative=100")]
        [SerializeField] private Slider   _killerBar;
        [SerializeField] private Slider   _motiveBar;
        [SerializeField] private Slider   _weaponBar;
        [SerializeField] private Slider   _locationBar;
        [SerializeField] private Slider   _timelineBar;
        [SerializeField] private Slider   _narrativeBar;

        [Header("Score Labels")]
        [SerializeField] private TMP_Text _killerLabel;
        [SerializeField] private TMP_Text _motiveLabel;
        [SerializeField] private TMP_Text _weaponLabel;
        [SerializeField] private TMP_Text _locationLabel;
        [SerializeField] private TMP_Text _timelineLabel;
        [SerializeField] private TMP_Text _narrativeLabel;

        [Header("Totals")]
        [SerializeField] private TMP_Text _totalText;
        [SerializeField] private TMP_Text _timeBonusText;
        [SerializeField] private TMP_Text _correctText;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration  = 0.3f;
        [SerializeField] private float _barsDuration  = 1.4f;

        // Max scores per category (from backend scoring logic)
        private const int MaxKiller   = 300;
        private const int MaxMotive   = 200;
        private const int MaxWeapon   = 150;
        private const int MaxLocation = 100;
        private const int MaxTimeline = 150;
        private const int MaxNarrative= 100;

        void Awake() => _panel?.SetActive(false);

        // ============================================
        // Public API
        // ============================================

        public void Show(MatchScore score)
        {
            _panel?.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateShow(score));
        }

        public void Hide()
        {
            StopAllCoroutines();
            _panel?.SetActive(false);
        }

        // ============================================
        // Animation
        // ============================================

        private IEnumerator AnimateShow(MatchScore score)
        {
            // Fade in panel
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                yield return FadeCanvas(_canvasGroup, 0f, 1f, _fadeDuration);
            }

            // Animate bars filling up
            yield return AnimateBars(score.breakdown, _barsDuration);

            // Show totals
            if (_totalText)
                _totalText.text = $"Total: {score.totalScore} / 1000";

            if (_timeBonusText)
                _timeBonusText.text = score.timeBonus > 0
                    ? $"+{score.timeBonus} time bonus"
                    : "";

            if (_correctText)
            {
                _correctText.text  = score.isCorrect ? "\u2714 Correct!" : "\u2718 Incorrect";
                _correctText.color = score.isCorrect ? Color.green : Color.red;
            }
        }

        private IEnumerator AnimateBars(ScoreBreakdown bd, float duration)
        {
            float elapsed = 0f;

            // Zero bars at start
            SetBar(_killerBar,    _killerLabel,    0, 0, MaxKiller);
            SetBar(_motiveBar,    _motiveLabel,    0, 0, MaxMotive);
            SetBar(_weaponBar,    _weaponLabel,    0, 0, MaxWeapon);
            SetBar(_locationBar,  _locationLabel,  0, 0, MaxLocation);
            SetBar(_timelineBar,  _timelineLabel,  0, 0, MaxTimeline);
            SetBar(_narrativeBar, _narrativeLabel, 0, 0, MaxNarrative);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                float e  = Mathf.SmoothStep(0f, 1f, t);

                SetBar(_killerBar,    _killerLabel,    Mathf.RoundToInt(bd.killer    * e), bd.killer,    MaxKiller);
                SetBar(_motiveBar,    _motiveLabel,    Mathf.RoundToInt(bd.motive    * e), bd.motive,    MaxMotive);
                SetBar(_weaponBar,    _weaponLabel,    Mathf.RoundToInt(bd.weapon    * e), bd.weapon,    MaxWeapon);
                SetBar(_locationBar,  _locationLabel,  Mathf.RoundToInt(bd.location  * e), bd.location,  MaxLocation);
                SetBar(_timelineBar,  _timelineLabel,  Mathf.RoundToInt(bd.timeline  * e), bd.timeline,  MaxTimeline);
                SetBar(_narrativeBar, _narrativeLabel, Mathf.RoundToInt(bd.narrative * e), bd.narrative, MaxNarrative);

                yield return null;
            }

            // Final exact values
            SetBar(_killerBar,    _killerLabel,    bd.killer,    bd.killer,    MaxKiller);
            SetBar(_motiveBar,    _motiveLabel,    bd.motive,    bd.motive,    MaxMotive);
            SetBar(_weaponBar,    _weaponLabel,    bd.weapon,    bd.weapon,    MaxWeapon);
            SetBar(_locationBar,  _locationLabel,  bd.location,  bd.location,  MaxLocation);
            SetBar(_timelineBar,  _timelineLabel,  bd.timeline,  bd.timeline,  MaxTimeline);
            SetBar(_narrativeBar, _narrativeLabel, bd.narrative, bd.narrative, MaxNarrative);
        }

        // ---- Helpers ----

        private static void SetBar(Slider bar, TMP_Text label, int current, int target, int max)
        {
            if (bar   != null) bar.value   = max > 0 ? (float)current / max : 0f;
            if (label != null) label.text  = $"{current}/{target}";
        }

        private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / time);
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
