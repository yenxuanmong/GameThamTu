// ============================================
// NotificationToast — static helper for showing toast messages
// Requires a NotificationToast prefab in the scene with this component
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class NotificationToast : MonoBehaviour
    {
        // Use a persistent instance reference that survives scene reloads
        private static NotificationToast _instance;

        [SerializeField] private GameObject  _toastPrefab;
        [SerializeField] private Transform   _container;
        [SerializeField] private float       _defaultDuration = 3f;
        [SerializeField] private int         _maxVisible      = 5;

        // Queue so rapid Show() calls stack gracefully instead of overlapping
        private readonly Queue<(string message, string type, float duration)> _queue = new();
        private int _activeCount;

        void Awake()
        {
            // Singleton guard: destroy duplicates that arrive after scene reload
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            // Clear the static ref only if we are the current instance
            if (_instance == this)
                _instance = null;
        }

        // ---- Static API ----

        public static void Show(string message, string type = "info", float duration = 0f)
        {
            if (_instance == null)
            {
                Debug.LogWarning($"[Toast] {type}: {message}");
                return;
            }
            float d = duration > 0 ? duration : _instance._defaultDuration;
            _instance.Enqueue(message, type, d);
        }

        // Called via SendMessage from Core scripts to avoid circular asmdef
        public void ShowToast(string message) => Show(message);

        // ---- Internal queuing ----

        private void Enqueue(string message, string type, float duration)
        {
            if (_activeCount >= _maxVisible)
            {
                _queue.Enqueue((message, type, duration));
                return;
            }
            StartCoroutine(ShowCoroutine(message, type, duration));
        }

        private void OnToastFinished()
        {
            _activeCount--;
            if (_queue.Count > 0)
            {
                var (msg, type, dur) = _queue.Dequeue();
                StartCoroutine(ShowCoroutine(msg, type, dur));
            }
        }

        private IEnumerator ShowCoroutine(string message, string type, float duration)
        {
            if (_toastPrefab == null || _container == null) yield break;

            _activeCount++;
            var go  = Instantiate(_toastPrefab, _container);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = message;

            // Colour by type
            var img = go.GetComponent<Image>();
            if (img)
            {
                img.color = type switch
                {
                    "error"   => new Color(0.9f, 0.2f, 0.2f, 0.9f),
                    "warning" => new Color(1.0f, 0.7f, 0.0f, 0.9f),
                    "success" => new Color(0.2f, 0.8f, 0.3f, 0.9f),
                    "hint"    => new Color(0.4f, 0.6f, 1.0f, 0.9f),
                    _         => new Color(0.2f, 0.2f, 0.2f, 0.85f),
                };
            }

            // Ensure CanvasGroup exists on the toast prefab root
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            // Fade in
            yield return FadeCanvas(cg, 0f, 1f, 0.2f);
            yield return new WaitForSeconds(duration);
            // Fade out
            yield return FadeCanvas(cg, 1f, 0f, 0.3f);
            Destroy(go);
            OnToastFinished();
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
