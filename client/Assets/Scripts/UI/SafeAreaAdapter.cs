// ============================================
// SafeAreaAdapter — adjusts UI RectTransform for device safe area
// Required for iPhone notch, Android cutouts etc.
// Attach to root Canvas RectTransform
// ============================================
using UnityEngine;

namespace DetectiveRoyale.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAdapter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect          _lastSafeArea;
        private Vector2       _lastScreenSize;

        void Awake()
        {
            _rectTransform  = GetComponent<RectTransform>();
            _lastSafeArea   = new Rect(0, 0, 0, 0);
            _lastScreenSize = Vector2.zero;
        }

        void Update()
        {
            Rect  safe       = Screen.safeArea;
            Vector2 screenSz = new Vector2(Screen.width, Screen.height);

            if (safe == _lastSafeArea && screenSz == _lastScreenSize) return;
            _lastSafeArea   = safe;
            _lastScreenSize = screenSz;
            Apply(safe, screenSz);
        }

        private void Apply(Rect safe, Vector2 screen)
        {
            if (screen.x == 0 || screen.y == 0) return;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;

            anchorMin.x /= screen.x;
            anchorMin.y /= screen.y;
            anchorMax.x /= screen.x;
            anchorMax.y /= screen.y;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
