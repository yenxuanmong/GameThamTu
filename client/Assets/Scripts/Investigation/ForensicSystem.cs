// ============================================
// ForensicSystem — specialised display for forensic/physical evidence
// Provides magnification, annotation tools
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class ForensicSystem : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;

        [Header("Magnifier")]
        [SerializeField] private RawImage     _mainImage;
        [SerializeField] private RawImage     _zoomImage;
        [SerializeField] private Slider       _zoomSlider;
        [SerializeField] private float        _minZoom = 1f;
        [SerializeField] private float        _maxZoom = 4f;

        [Header("Info")]
        [SerializeField] private TMP_Text     _nameText;
        [SerializeField] private TMP_Text     _analysisText;

        private Evidence _current;
        private float    _zoom = 1f;

        public void Open(Evidence evidence)
        {
            _current = evidence;
            _panel?.SetActive(true);

            if (_nameText)     _nameText.text     = evidence.name;
            if (_analysisText) _analysisText.text = evidence.description;

            if (!string.IsNullOrEmpty(evidence.imageUrl))
                StartCoroutine(LoadForensicImage(evidence.imageUrl));

            if (_zoomSlider)
            {
                _zoomSlider.minValue = _minZoom;
                _zoomSlider.maxValue = _maxZoom;
                _zoomSlider.value    = 1f;
            }
        }

        public void Close() => _panel?.SetActive(false);

        public void OnZoomChanged(float value)
        {
            _zoom = value;
            if (_mainImage)
            {
                _mainImage.transform.localScale = Vector3.one * _zoom;
            }
        }

        private IEnumerator LoadForensicImage(string url)
        {
            using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
                if (_mainImage)  _mainImage.texture  = tex;
                if (_zoomImage)  _zoomImage.texture  = tex;
            }
        }
    }
}
