// ============================================
// ScrollToBottom — auto-scrolls ScrollRect to bottom on content change
// Attach to a ScrollRect that displays chat or log messages
// ============================================
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveRoyale.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollToBottom : MonoBehaviour
    {
        [SerializeField] private bool _autoScroll = true;
        [SerializeField] private float _threshold  = 50f;  // px from bottom to keep auto-scroll active

        private ScrollRect   _scroll;
        private RectTransform _content;
        private bool          _userScrolledUp;

        void Awake()
        {
            _scroll  = GetComponent<ScrollRect>();
            _content = _scroll.content;
        }

        void Start()
        {
            _scroll.onValueChanged.AddListener(OnScrolled);
        }

        void OnDestroy()
        {
            _scroll.onValueChanged.RemoveListener(OnScrolled);
        }

        // ---- Call after adding new items ----
        public void ScrollNow()
        {
            if (!_autoScroll || _userScrolledUp) return;
            StartCoroutine(DoScroll());
        }

        public void ForceScrollNow()
        {
            _userScrolledUp = false;
            StartCoroutine(DoScroll());
        }

        private System.Collections.IEnumerator DoScroll()
        {
            yield return null;                         // wait one frame for layout rebuild
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        private void OnScrolled(Vector2 pos)
        {
            // If user scrolls up significantly, pause auto-scroll
            float distFromBottom = _content != null
                ? (1f - pos.y) * _content.rect.height
                : 0f;
            _userScrolledUp = distFromBottom > _threshold;
        }
    }
}
