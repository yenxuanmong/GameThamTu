// ============================================
// TooltipSystem — hover tooltips for UI elements
// ============================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DetectiveRoyale.UI
{
    // ---- Attach to any UI element to give it a tooltip ----
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea(1, 3)]
        [SerializeField] private string _text;

        public void OnPointerEnter(PointerEventData _) => TooltipSystem.Show(_text);
        public void OnPointerExit(PointerEventData _)  => TooltipSystem.Hide();

        public void SetText(string text) => _text = text;
    }

    // ---- Singleton tooltip panel ----
    public class TooltipSystem : MonoBehaviour
    {
        private static TooltipSystem _instance;

        [SerializeField] private GameObject  _panel;
        [SerializeField] private TMP_Text    _text;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Vector2     _offset = new Vector2(15f, -15f);

        void Awake()
        {
            _instance = this;
            _panel?.SetActive(false);
        }

        void Update()
        {
            if (!(_panel?.activeSelf ?? false)) return;
            Vector2 pos = Input.mousePosition;
            if (_rectTransform)
                _rectTransform.position = pos + _offset;
        }

        public static void Show(string text)
        {
            if (_instance == null || string.IsNullOrEmpty(text)) return;
            if (_instance._text) _instance._text.text = text;
            _instance._panel?.SetActive(true);
        }

        public static void Hide()
        {
            _instance?._panel?.SetActive(false);
        }
    }
}
