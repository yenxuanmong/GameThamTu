// ============================================
// UIAnimator — reusable panel animation helpers
// Slide in/out, scale bounce, shake effects
// ============================================
using System;
using System.Collections;
using UnityEngine;

namespace DetectiveRoyale.UI
{
    public class UIAnimator : MonoBehaviour
    {
        public enum AnimType { FadeIn, FadeOut, SlideInLeft, SlideInRight, SlideInUp, SlideInDown, ScaleBounce, Shake }

        [SerializeField] private AnimType  _openAnim  = AnimType.FadeIn;
        [SerializeField] private AnimType  _closeAnim = AnimType.FadeOut;
        [SerializeField] private float     _duration  = 0.25f;
        [SerializeField] private bool      _autoPlayOnEnable = true;

        private CanvasGroup    _canvasGroup;
        private RectTransform  _rectTransform;
        private Vector2        _originalPosition;
        private Vector3        _originalScale;

        void Awake()
        {
            _canvasGroup    = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _rectTransform  = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;
            _originalScale    = _rectTransform.localScale;
        }

        void OnEnable()
        {
            if (_autoPlayOnEnable)
                PlayOpen();
        }

        // ---- Public API ----

        public void PlayOpen(Action onDone = null)  => StartCoroutine(Animate(_openAnim,  true,  onDone));
        public void PlayClose(Action onDone = null) => StartCoroutine(Animate(_closeAnim, false, onDone));

        public static void PlayOn(GameObject go, AnimType type, float duration = 0.25f, Action onDone = null)
        {
            var anim = go.GetComponent<UIAnimator>() ?? go.AddComponent<UIAnimator>();
            go.SetActive(true);
            anim.StartCoroutine(anim.Animate(type, true, onDone, duration));
        }

        // ============================================
        // Core animate coroutine
        // ============================================

        private IEnumerator Animate(AnimType type, bool opening, Action onDone, float dur = -1f)
        {
            float d = dur > 0 ? dur : _duration;

            switch (type)
            {
                case AnimType.FadeIn:
                case AnimType.FadeOut:
                    yield return FadeCoroutine(opening ? 0f : 1f, opening ? 1f : 0f, d);
                    break;
                case AnimType.SlideInLeft:
                    yield return SlideCoroutine(opening ? new Vector2(-Screen.width, 0) : _originalPosition,
                                                opening ? _originalPosition : new Vector2(-Screen.width, 0), d);
                    break;
                case AnimType.SlideInRight:
                    yield return SlideCoroutine(opening ? new Vector2(Screen.width, 0) : _originalPosition,
                                                opening ? _originalPosition : new Vector2(Screen.width, 0), d);
                    break;
                case AnimType.SlideInUp:
                    yield return SlideCoroutine(opening ? new Vector2(0, -Screen.height) : _originalPosition,
                                                opening ? _originalPosition : new Vector2(0, -Screen.height), d);
                    break;
                case AnimType.SlideInDown:
                    yield return SlideCoroutine(opening ? new Vector2(0, Screen.height) : _originalPosition,
                                                opening ? _originalPosition : new Vector2(0, Screen.height), d);
                    break;
                case AnimType.ScaleBounce:
                    yield return ScaleBounceCoroutine(d);
                    break;
                case AnimType.Shake:
                    yield return ShakeCoroutine(d, 8f);
                    break;
            }

            onDone?.Invoke();
        }

        // ---- Fade ----
        private IEnumerator FadeCoroutine(float from, float to, float dur)
        {
            _canvasGroup.alpha = from;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur));
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        // ---- Slide ----
        private IEnumerator SlideCoroutine(Vector2 from, Vector2 to, float dur)
        {
            _rectTransform.anchoredPosition = from;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _rectTransform.anchoredPosition = Vector2.Lerp(from, to,
                    Mathf.SmoothStep(0, 1, t / dur));
                yield return null;
            }
            _rectTransform.anchoredPosition = to;
        }

        // ---- Scale bounce ----
        private IEnumerator ScaleBounceCoroutine(float dur)
        {
            _rectTransform.localScale = Vector3.zero;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                // Overshoot spring
                float s = 1f + 0.3f * Mathf.Sin(p * Mathf.PI * 2.5f) * (1f - p);
                _rectTransform.localScale = _originalScale * Mathf.Lerp(0f, s, p);
                yield return null;
            }
            _rectTransform.localScale = _originalScale;
        }

        // ---- Shake ----
        private IEnumerator ShakeCoroutine(float dur, float magnitude)
        {
            Vector2 origin = _rectTransform.anchoredPosition;
            float   t      = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float decay = 1f - (t / dur);
                _rectTransform.anchoredPosition = origin +
                    new Vector2(UnityEngine.Random.Range(-1f, 1f),
                                UnityEngine.Random.Range(-1f, 1f)) * magnitude * decay;
                yield return null;
            }
            _rectTransform.anchoredPosition = origin;
        }
    }
}
