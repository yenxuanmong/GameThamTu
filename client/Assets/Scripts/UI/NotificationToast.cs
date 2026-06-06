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
        private static NotificationToast _instance;

        [SerializeField] private GameObject  _toastPrefab;
        [SerializeField] private Transform   _container;
        [SerializeField] private float       _defaultDuration = 3f;

        void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ---- Static API ----

        public static void Show(string message, string type = "info", float duration = 0f)
        {
            if (_instance == null)
            {
                Debug.LogWarning($"[Toast] {type}: {message}");
                return;
            }
            _instance.StartCoroutine(_instance.ShowCoroutine(message, type,
                duration > 0 ? duration : _instance._defaultDuration));
        }

        private IEnumerator ShowCoroutine(string message, string type, float duration)
        {
            if (_toastPrefab == null || _container == null) yield break;

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

            // Fade in
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            yield return FadeCanvas(cg, 0f, 1f, 0.2f);
            yield return new WaitForSeconds(duration);
            // Fade out
            yield return FadeCanvas(cg, 1f, 0f, 0.3f);
            Destroy(go);
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
