// ============================================
// CaseBriefingUI — shown at start of Investigation scene
// Displays case title, victim info, and initial details
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class CaseBriefingUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Case info")]
        [SerializeField] private TMP_Text    _caseTitleText;
        [SerializeField] private TMP_Text    _victimNameText;
        [SerializeField] private TMP_Text    _victimAgeText;
        [SerializeField] private TMP_Text    _victimOccText;
        [SerializeField] private TMP_Text    _causeOfDeathText;
        [SerializeField] private TMP_Text    _timeOfDeathText;
        [SerializeField] private TMP_Text    _locationFoundText;
        [SerializeField] private TMP_Text    _difficultyText;
        [SerializeField] private TMP_Text    _descriptionText;

        [Header("Objectives")]
        [SerializeField] private TMP_Text    _objectivesText;

        [Header("Buttons")]
        [SerializeField] private Button      _startBtn;
        [SerializeField] private TMP_Text    _countdownText;

        [Header("Timing")]
        [SerializeField] private float       _autoHideDelay = 30f;

        private Coroutine _autoHideCoroutine;

        // ============================================
        // Open
        // ============================================

        public void Show()
        {
            _panel?.SetActive(true);
            if (_canvasGroup) _canvasGroup.alpha = 1f;

            PopulateFromSession();
            _autoHideCoroutine = StartCoroutine(AutoHideAfter(_autoHideDelay));
        }

        public void Hide()
        {
            if (_autoHideCoroutine != null)
                StopCoroutine(_autoHideCoroutine);
            StartCoroutine(FadeAndHide());
        }

        // ============================================
        // Populate
        // ============================================

        private void PopulateFromSession()
        {
            var c = GameSession.Case;
            if (c == null) return;

            if (_caseTitleText)    _caseTitleText.text    = c.title;
            if (_descriptionText)  _descriptionText.text  = c.description;
            if (_victimNameText)   _victimNameText.text   = c.victimName;
            if (_victimAgeText)    _victimAgeText.text    = $"Age: {c.victimAge}";
            if (_victimOccText)    _victimOccText.text    = c.victimOccupation;
            if (_causeOfDeathText) _causeOfDeathText.text = $"Cause: {c.causeOfDeath}";
            if (_timeOfDeathText)  _timeOfDeathText.text  = $"Time of death: {c.timeOfDeath}";
            if (_locationFoundText)_locationFoundText.text= $"Found at: {c.locationFound}";
            if (_difficultyText)   _difficultyText.text   = c.difficulty.ToUpper();

            if (_objectivesText)
            {
                _objectivesText.text =
                    $"• Identify {c.suspectCount} suspects\n" +
                    $"• Interview {c.witnessCount} witnesses\n" +
                    $"• Collect {c.evidenceCount} pieces of evidence\n" +
                    $"• Submit your conclusion before time expires";
            }
        }

        // ============================================
        // Buttons
        // ============================================

        public void OnClickStart() => Hide();

        // ============================================
        // Helpers
        // ============================================

        private IEnumerator AutoHideAfter(float delay)
        {
            float remaining = delay;
            while (remaining > 0f)
            {
                if (_countdownText)
                    _countdownText.text = $"Auto-closing in {Mathf.CeilToInt(remaining)}s...";
                remaining -= Time.deltaTime;
                yield return null;
            }
            Hide();
        }

        private IEnumerator FadeAndHide()
        {
            if (_canvasGroup != null)
            {
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }
            _panel?.SetActive(false);
        }
    }
}
