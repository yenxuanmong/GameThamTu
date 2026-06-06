// ============================================
// SplashScreen — shown before MainMenu loads
// Plays intro animation then transitions to MainMenu
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class SplashScreen : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private CanvasGroup _logoGroup;
        [SerializeField] private CanvasGroup _taglineGroup;
        [SerializeField] private TMP_Text    _taglineText;
        [SerializeField] private Image       _background;

        [Header("Timing")]
        [SerializeField] private float _fadeInTime   = 1.0f;
        [SerializeField] private float _holdTime     = 1.5f;
        [SerializeField] private float _fadeOutTime  = 0.8f;
        [SerializeField] private float _taglineDelay = 0.5f;

        [Header("Skip")]
        [SerializeField] private Button _skipBtn;

        private bool _skipped;

        void Start()
        {
            if (_logoGroup)    _logoGroup.alpha    = 0f;
            if (_taglineGroup) _taglineGroup.alpha = 0f;
            StartCoroutine(PlaySplash());
        }

        void Update()
        {
            // Skip on any key/tap
            if (Input.anyKeyDown && !_skipped)
                _skipped = true;
        }

        private IEnumerator PlaySplash()
        {
            yield return StartCoroutine(Fade(_logoGroup, 0f, 1f, _fadeInTime));
            yield return new WaitForSeconds(_taglineDelay);
            yield return StartCoroutine(Fade(_taglineGroup, 0f, 1f, _fadeInTime * 0.5f));

            float elapsed = 0f;
            while (elapsed < _holdTime && !_skipped)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return StartCoroutine(Fade(_logoGroup,    1f, 0f, _fadeOutTime));
            yield return StartCoroutine(Fade(_taglineGroup, 1f, 0f, _fadeOutTime * 0.5f));

            SceneLoader.Instance?.LoadScene(SceneLoader.SCENE_MAIN_MENU);
        }

        public void OnClickSkip()
        {
            _skipped = true;
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t          += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
