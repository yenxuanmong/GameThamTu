// ============================================
// FadeTransition — black fade in/out for scene transitions
// Place on a fullscreen black Image panel in every scene
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveRoyale.UI
{
    public class FadeTransition : MonoBehaviour
    {
        public static FadeTransition Instance { get; private set; }

        [SerializeField] private Image  _fadeImage;
        [SerializeField] private float  _fadeDuration = 0.4f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Start faded in, then fade out
            if (_fadeImage)
            {
                _fadeImage.color = Color.black;
                StartCoroutine(FadeOut());
            }
        }

        // ---- Fade out (black → clear) ----
        public IEnumerator FadeOut(float duration = 0f)
        {
            float d = duration > 0 ? duration : _fadeDuration;
            yield return Fade(1f, 0f, d);
        }

        // ---- Fade in (clear → black) then invoke callback ----
        public IEnumerator FadeIn(System.Action onComplete = null, float duration = 0f)
        {
            float d = duration > 0 ? duration : _fadeDuration;
            yield return Fade(0f, 1f, d);
            onComplete?.Invoke();
        }

        // ---- Fade in → load scene → fade out ----
        public void TransitionToScene(string sceneName)
        {
            StartCoroutine(DoTransition(sceneName));
        }

        private IEnumerator DoTransition(string sceneName)
        {
            yield return StartCoroutine(FadeIn());
            Core.SceneLoader.Instance?.LoadScene(sceneName);
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(FadeOut());
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (_fadeImage == null) yield break;
            float t = 0f;
            Color c = _fadeImage.color;
            while (t < duration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, t / duration);
                _fadeImage.color = c;
                yield return null;
            }
            c.a = to;
            _fadeImage.color = c;
        }
    }
}
