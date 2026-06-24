// ============================================
// CameraController — isometric/top-down camera for Investigation scene
// Supports pan, zoom, and follow-player mode
// ============================================
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Follow")]
        [SerializeField] private Transform _target;
        [SerializeField] private bool      _followTarget = true;
        [SerializeField] private float     _followSmoothing = 5f;
        [SerializeField] private Vector3   _followOffset = new Vector3(0f, 8f, -6f);

        [Header("Pan")]
        [SerializeField] private bool  _allowPan    = true;
        [SerializeField] private float _panSpeed    = 15f;
        [SerializeField] private float _panBorder   = 20f;    // pixels from edge to pan
        [SerializeField] private float _panMinX     = -20f;
        [SerializeField] private float _panMaxX     =  20f;
        [SerializeField] private float _panMinZ     = -20f;
        [SerializeField] private float _panMaxZ     =  20f;

        [Header("Zoom")]
        [SerializeField] private bool  _allowZoom   = true;
        [SerializeField] private float _zoomSpeed   = 4f;
        [SerializeField] private float _minZoom     = 4f;
        [SerializeField] private float _maxZoom     = 14f;
        [SerializeField] private float _currentZoom = 8f;

        [Header("Rotation")]
        [SerializeField] private bool  _allowRotate = false;
        [SerializeField] private float _rotateSpeed = 80f;

        private Camera _cam;
        private bool   _panMode;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _cam = GetComponent<Camera>() ?? Camera.main;
        }

        void LateUpdate()
        {
            if (_followTarget && _target != null)
            {
                FollowTarget();
            }
            else if (_allowPan)
            {
                HandlePan();
            }

            if (_allowZoom) HandleZoom();
            if (_allowRotate) HandleRotation();
        }

        // ============================================
        // Follow
        // ============================================

        private void FollowTarget()
        {
            Vector3 desired = _target.position + _followOffset;
            transform.position = Vector3.Lerp(
                transform.position, desired, Time.deltaTime * _followSmoothing);
        }

        public void SetTarget(Transform target, bool follow = true)
        {
            _target       = target;
            _followTarget = follow;
        }

        public void SetFollowEnabled(bool enabled) => _followTarget = enabled;

        // ============================================
        // Pan (edge scroll + middle mouse drag)
        // ============================================

        private void HandlePan()
        {
            Vector3 pos = transform.position;

            // Middle mouse drag
            if (Input.GetMouseButton(2))
            {
                float h = -Input.GetAxis("Mouse X") * _panSpeed * Time.deltaTime * (_currentZoom / 8f);
                float v = -Input.GetAxis("Mouse Y") * _panSpeed * Time.deltaTime * (_currentZoom / 8f);
                pos += transform.right * h + Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * v;
            }
            // Edge scroll
            else
            {
                Vector2 mouse = Input.mousePosition;
                if (mouse.x < _panBorder) pos += Vector3.left   * _panSpeed * Time.deltaTime;
                if (mouse.x > Screen.width  - _panBorder) pos += Vector3.right  * _panSpeed * Time.deltaTime;
                if (mouse.y < _panBorder) pos += Vector3.back   * _panSpeed * Time.deltaTime;
                if (mouse.y > Screen.height - _panBorder) pos += Vector3.forward * _panSpeed * Time.deltaTime;
            }

            pos.x = Mathf.Clamp(pos.x, _panMinX, _panMaxX);
            pos.z = Mathf.Clamp(pos.z, _panMinZ, _panMaxZ);
            transform.position = pos;
        }

        // ============================================
        // Zoom
        // ============================================

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            _currentZoom = Mathf.Clamp(_currentZoom - scroll * _zoomSpeed * 10f, _minZoom, _maxZoom);

            if (_cam != null && _cam.orthographic)
                _cam.orthographicSize = _currentZoom;
            else
                transform.position += transform.forward * scroll * _zoomSpeed;
        }

        // ============================================
        // Rotation
        // ============================================

        private void HandleRotation()
        {
            if (Input.GetKey(KeyCode.Q))
                transform.Rotate(0f, -_rotateSpeed * Time.deltaTime, 0f, Space.World);
            if (Input.GetKey(KeyCode.E))
                transform.Rotate(0f,  _rotateSpeed * Time.deltaTime, 0f, Space.World);
        }

        // ============================================
        // Utility
        // ============================================

        public void FocusOn(Vector3 worldPos, bool instant = false)
        {
            Vector3 target = worldPos + _followOffset;
            if (instant) transform.position = target;
            else         StartCoroutine(SmoothMoveTo(target, 0.5f));
        }

        private System.Collections.IEnumerator SmoothMoveTo(Vector3 destination, float duration)
        {
            Vector3 start = transform.position;
            float   t     = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, destination, t / duration);
                yield return null;
            }
            transform.position = destination;
        }
    }
}
