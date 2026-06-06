// ============================================
// InspectObject — 3D object inspection (rotate & zoom)
// Renders object in a separate camera for close-up view
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.Investigation
{
    public class InspectObject : MonoBehaviour
    {
        public static InspectObject Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private RawImage   _renderTarget;

        [Header("Inspect Camera")]
        [SerializeField] private Camera     _inspectCamera;
        [SerializeField] private RenderTexture _renderTexture;

        [Header("Object Stage")]
        [SerializeField] private Transform  _objectStage;   // holds the displayed object
        [SerializeField] private float      _rotateSpeed = 120f;
        [SerializeField] private float      _zoomSpeed   = 2f;
        [SerializeField] private float      _minZoom     = 2f;
        [SerializeField] private float      _maxZoom     = 8f;

        [Header("Info")]
        [SerializeField] private TMP_Text   _nameText;
        [SerializeField] private TMP_Text   _descText;

        private GameObject _currentObject;
        private bool       _dragging;
        private Vector2    _lastMousePos;
        private float      _zoom = 4f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _panel?.SetActive(false);
        }

        void Update()
        {
            if (!(_panel?.activeSelf ?? false)) return;
            HandleRotation();
            HandleZoom();
        }

        // ============================================
        // Open with a prefab or existing object
        // ============================================

        public void Inspect(GameObject obj3D, string displayName, string description)
        {
            // Clear previous
            if (_currentObject != null)
                Destroy(_currentObject);

            _panel?.SetActive(true);
            _zoom = 4f;

            _currentObject = Instantiate(obj3D, _objectStage);
            _currentObject.transform.localPosition = Vector3.zero;

            if (_nameText) _nameText.text = displayName;
            if (_descText) _descText.text = description;

            ApplyZoom();
        }

        public void Close()
        {
            if (_currentObject != null)
                Destroy(_currentObject);
            _panel?.SetActive(false);
        }

        // ============================================
        // Input handling
        // ============================================

        private void HandleRotation()
        {
            if (Input.GetMouseButtonDown(0)) { _dragging = true;  _lastMousePos = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0))   { _dragging = false; }

            if (_dragging && _objectStage != null)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastMousePos;
                _objectStage.Rotate(Vector3.up,   -delta.x * _rotateSpeed * Time.deltaTime, Space.World);
                _objectStage.Rotate(Vector3.right,  delta.y * _rotateSpeed * Time.deltaTime, Space.World);
                _lastMousePos = Input.mousePosition;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _zoom = Mathf.Clamp(_zoom - scroll * _zoomSpeed, _minZoom, _maxZoom);
                ApplyZoom();
            }
        }

        private void ApplyZoom()
        {
            if (_inspectCamera)
                _inspectCamera.transform.localPosition = new Vector3(0f, 0f, -_zoom);
        }
    }
}
