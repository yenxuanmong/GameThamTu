// ============================================
// AnimatedButton — button with scale/color animation on hover/press
// Add to any Button alongside existing scripts
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DetectiveRoyale.UI
{
    [RequireComponent(typeof(Button))]
    public class AnimatedButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler,  IPointerUpHandler
    {
        [Header("Scale")]
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _pressScale = 0.95f;
        [SerializeField] private float _animSpeed  = 10f;

        [Header("Color tint")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor  = new Color(1f, 1f, 0.85f, 1f);
        [SerializeField] private Color _pressColor  = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Header("Audio")]
        [SerializeField] private bool _playClickSound = true;

        private Vector3  _targetScale;
        private Color    _targetColor;
        private Image    _image;
        private Button   _button;

        void Awake()
        {
            _image    = GetComponent<Image>();
            _button   = GetComponent<Button>();
            _targetScale = Vector3.one;
            _targetColor = _normalColor;
        }

        void Update()
        {
            transform.localScale =
                Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _animSpeed);

            if (_image)
                _image.color = Color.Lerp(_image.color, _targetColor, Time.deltaTime * _animSpeed);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_button.interactable) return;
            _targetScale = Vector3.one * _hoverScale;
            _targetColor = _hoverColor;
        }

        public void OnPointerExit(PointerEventData _)
        {
            _targetScale = Vector3.one;
            _targetColor = _normalColor;
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (!_button.interactable) return;
            _targetScale = Vector3.one * _pressScale;
            _targetColor = _pressColor;
            if (_playClickSound) Core.AudioManager.Instance?.PlayClick();
        }

        public void OnPointerUp(PointerEventData _)
        {
            _targetScale = Vector3.one * _hoverScale;
            _targetColor = _hoverColor;
        }
    }
}
