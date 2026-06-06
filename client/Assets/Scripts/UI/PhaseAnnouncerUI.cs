// ============================================
// PhaseAnnouncerUI — full-screen phase change announcer
// Shows animated banner when match phase changes
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class PhaseAnnouncerUI : MonoBehaviour
    {
        [SerializeField] private GameObject  _panel;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text    _phaseTitle;
        [SerializeField] private TMP_Text    _phaseSubtitle;
        [SerializeField] private Image       _backgroundFlash;
        [SerializeField] private float       _displayTime  = 2.5f;
        [SerializeField] private float       _fadeTime     = 0.4f;

        private static readonly System.Collections.Generic.Dictionary<string, (string title, string sub, Color color)>
            PhaseInfo = new()
            {
                ["investigation"]  = ("INVESTIGATION", "Find the clues. Question the witnesses.", new Color(0.2f, 0.6f, 1f)),
                ["final_minutes"]  = ("FINAL MINUTES", "Time is running out! Submit your conclusion.", new Color(1f, 0.6f, 0.1f)),
                ["submission"]     = ("SUBMISSION PHASE", "Lock in your answer. Who is the killer?", Color.green),
                ["reveal"]         = ("CASE REVEALED", "The truth comes to light...", new Color(0.9f, 0.8f, 0.1f)),
            };

        void Start()
        {
            _panel?.SetActive(false);
            SocketManager.Instance?.OnPhaseChanged.AddListener(OnPhaseChanged);
        }

        void OnDestroy()
        {
            SocketManager.Instance?.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }

        private void OnPhaseChanged(PhasePayload p) => Announce(p.phase);

        public void Announce(string phase)
        {
            if (!PhaseInfo.TryGetValue(phase, out var info)) return;
            StartCoroutine(ShowAnnouncement(info.title, info.sub, info.color));
        }

        private IEnumerator ShowAnnouncement(string title, string subtitle, Color color)
        {
            if (_phaseTitle)   _phaseTitle.text   = title;
            if (_phaseSubtitle)_phaseSubtitle.text = subtitle;
            if (_backgroundFlash) _backgroundFlash.color = new Color(color.r, color.g, color.b, 0.25f);

            _panel?.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // Fade in
            yield return Fade(0f, 1f, _fadeTime);
            yield return new WaitForSeconds(_displayTime);
            // Fade out
            yield return Fade(1f, 0f, _fadeTime);

            _panel?.SetActive(false);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
