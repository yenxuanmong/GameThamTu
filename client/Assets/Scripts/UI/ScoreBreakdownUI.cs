// ============================================
// ScoreBreakdownUI — animated score breakdown panel
// Shows per-category score after match ends
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
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Category bars")]
        [SerializeField] private Slider  _killerBar;
        [SerializeField] private Slider  _motiveBar;
        [SerializeField] private Slider  _weaponBar;
        [SerializeField] private Slider  _locationBar;
        [SerializeField] private Slider  _timelineBar;
        [SerializeField] private Slider  _narrativeBar;

        [Header("Category labels")]
        [SerializeField] private TMP_Text _killerLabel;
        [SerializeField] private TMP_Text _motiveLabel;
        [SerializeField] private TMP_Text _weaponLabel;
        [SerializeField] private TMP_Text _locationLabel;
        [SerializeField] private TMP_Text _timelineLabel;
        [SerializeField] private TMP_Text _narrativeLabel;

        [Header("Total")]
        [SerializeField] private TMP_Text _totalText;
        [SerializeField] private TMP_Text _timeBonusText;

        [Header("Animation")]
        [SerializeField] private float _animDuration = 1.2f;

        private const int MaxCategoryScore = 200;

        void Start() => _panel?.SetActive(false);

        public void Show(MatchScore score)
        {
            _panel?.SetActive(true);
            StartCoroutine(AnimateIn(score));
        }

        public void Hide() => _panel?.SetActive(false);

        private IEnumerator AnimateIn(MatchScore score)
        {
            // Fade in
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    _canvasGroup.alpha = t / 0.3f;
                    yield return null;
                }
                _canvasGroup.alpha = 1f;
            }

            // Animate bars
            var bd = score.breakdown;
            yield return StartCoroutine(AnimateBars(bd, _animDuration));

            // Set total
            if (_totalText)    _totalText.text    = $"{score.totalScore} pts";
            if (_timeBonusText && score.timeBonus > 0)
                _timeBonusText.text = $"+{score.timeBonus} time bonus";
        }

        private IEnumerator AnimateBars(ScoreBreakdown bd, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                float e  = Mathf.SmoothStep(0f, 1f, t);

                SetBar(_killerBar,   _killerLabel,   bd.killer,   e);
                SetBar(_motiveBar,   _motiveLabel,   bd.motive,   e);
                SetBar(_weaponBar,   _weaponLabel,   bd.weapon,   e);
                SetBar(_locationBar, _locationLabel, bd.location, e);
                SetBar(_timelineBar, _timelineLabel, bd.timeline, e);
                SetBar(_narrativeBar,_narrativeLabel,bd.narrative,e);

                yield return null;
            }

            SetBar(_killerBar,   _killerLabel,   bd.killer,   1f);
            SetBar(_motiveBar,   _motiveLabel,   bd.motive,   1f);
            SetBar(_weaponBar,   _weaponLabel,   bd.weapon,   1f);
            SetBar(_locationBar, _locationLabel, bd.location, 1f);
            SetBar(_timelineBar, _timelineLabel, bd.timeline, 1f);
            SetBar(_narrativeBar,_narrativeLabel,bd.narrative,1f);
        }

        private static void SetBar(Slider bar, TMP_Text label, int target, float progress)
        {
            int current = Mathf.RoundToInt(target * progress);
            if (bar)   bar.value    = (float)current / MaxCategoryScore;
            if (label) label.text   = current.ToString();
        }
    }
}
