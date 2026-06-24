// ============================================
// InteractableHighlight — hover glow effect on interactable objects
// Attach to any object with Renderer component
// ============================================
using UnityEngine;

namespace DetectiveRoyale.Investigation
{
    [RequireComponent(typeof(Renderer))]
    public class InteractableHighlight : MonoBehaviour
    {
        [Header("Highlight")]
        [SerializeField] private Color  _normalColor    = Color.white;
        [SerializeField] private Color  _hoverColor     = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color  _collectedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private string _colorProperty  = "_BaseColor";

        [Header("Pulse")]
        [SerializeField] private bool   _pulsing  = true;
        [SerializeField] private float  _pulseSpeed = 2f;
        [SerializeField] private float  _pulseMin   = 0.7f;
        [SerializeField] private float  _pulseMax   = 1.0f;

        private Renderer  _renderer;
        private Material  _mat;
        private bool      _collected;
        private bool      _hovered;

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            // Create instance material to avoid shared material modification
            _mat = _renderer.material;
        }

        void Update()
        {
            if (_collected || !_pulsing || _hovered) return;

            float t     = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(_pulseMin, _pulseMax, t);
            Color c     = _normalColor;
            c.a         = alpha;
            _mat?.SetColor(_colorProperty, c);
        }

        void OnMouseEnter()
        {
            if (_collected) return;
            _hovered = true;
            _mat?.SetColor(_colorProperty, _hoverColor);
        }

        void OnMouseExit()
        {
            if (_collected) return;
            _hovered = false;
            _mat?.SetColor(_colorProperty, _normalColor);
        }

        public void MarkCollected()
        {
            _collected = true;
            _hovered   = false;
            _mat?.SetColor(_colorProperty, _collectedColor);
        }
    }
}
