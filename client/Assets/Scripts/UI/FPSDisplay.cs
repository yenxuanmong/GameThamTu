// ============================================
// FPSDisplay — lightweight FPS counter overlay
// Only visible in development builds
// ============================================
using UnityEngine;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float    _updateInterval = 0.5f;
        [SerializeField] private Color    _goodColor  = Color.green;
        [SerializeField] private Color    _medColor   = Color.yellow;
        [SerializeField] private Color    _badColor   = Color.red;
        [SerializeField] private int      _goodFps    = 55;
        [SerializeField] private int      _medFps     = 30;

        private float _timer;
        private int   _frames;
        private float _fps;

        void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }

        void Update()
        {
            _frames++;
            _timer += Time.unscaledDeltaTime;

            if (_timer < _updateInterval) return;

            _fps    = _frames / _timer;
            _frames = 0;
            _timer  = 0f;

            if (_text == null) return;
            _text.text  = $"{_fps:F0} FPS";
            _text.color = _fps >= _goodFps ? _goodColor
                        : _fps >= _medFps  ? _medColor
                        : _badColor;
        }
    }
}
