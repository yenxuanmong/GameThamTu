// ============================================
// PlayerController — top-down / point-and-click player movement
// Used in Investigation scene to navigate the crime scene
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace DetectiveRoyale.Core
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float    _moveSpeed        = 4f;
        [SerializeField] private float    _rotationSpeed    = 720f;
        [SerializeField] private LayerMask _walkableLayer;
        [SerializeField] private LayerMask _interactableLayer;

        [Header("Visual")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string   _walkParam  = "IsWalking";
        [SerializeField] private string   _speedParam = "Speed";

        [Header("Click Marker")]
        [SerializeField] private GameObject _clickMarkerPrefab;
        [SerializeField] private float      _markerLifetime = 0.5f;

        [Header("Interaction")]
        [SerializeField] private float _interactRange = 2f;

        private NavMeshAgent  _agent;
        private Camera        _camera;
        private bool          _movementEnabled = true;
        private GameObject    _currentMarker;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _agent  = GetComponent<NavMeshAgent>();
            _camera = Camera.main;

            _agent.speed        = _moveSpeed;
            _agent.angularSpeed = _rotationSpeed;
            _agent.acceleration = 16f;
        }

        void Update()
        {
            if (!_movementEnabled) return;

            HandleClickMovement();
            UpdateAnimation();
        }

        // ============================================
        // Click-to-move
        // ============================================

        private void HandleClickMovement()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current?.IsPointerOverGameObject() ?? false) return;

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

            // Check interactable first
            if (Physics.Raycast(ray, out RaycastHit interactHit, 50f, _interactableLayer))
            {
                // Handled by ClueCollector / AIChat components on the hit object
                return;
            }

            // Move to clicked point
            if (Physics.Raycast(ray, out RaycastHit walkHit, 50f, _walkableLayer))
            {
                MoveTo(walkHit.point);
                SpawnClickMarker(walkHit.point);
            }
        }

        public void MoveTo(Vector3 destination)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            _agent.SetDestination(destination);
        }

        public void StopMovement()
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled) StopMovement();
        }

        // ============================================
        // Animation
        // ============================================

        private void UpdateAnimation()
        {
            if (_animator == null || _agent == null) return;
            float speed = _agent.velocity.magnitude;
            _animator.SetBool(_walkParam, speed > 0.1f);
            _animator.SetFloat(_speedParam, speed / _moveSpeed);
        }

        // ============================================
        // Click marker VFX
        // ============================================

        private void SpawnClickMarker(Vector3 pos)
        {
            if (_clickMarkerPrefab == null) return;
            if (_currentMarker != null) Destroy(_currentMarker);
            _currentMarker = Instantiate(_clickMarkerPrefab, pos + Vector3.up * 0.01f, Quaternion.identity);
            Destroy(_currentMarker, _markerLifetime);
        }

        // ============================================
        // Interaction proximity check
        // ============================================

        public bool IsInRange(Vector3 target)
            => Vector3.Distance(transform.position, target) <= _interactRange;

        public IEnumerator MoveToAndInteract(Vector3 target, System.Action onArrived)
        {
            MoveTo(target);
            while (!IsInRange(target) && _agent.pathPending == false)
                yield return null;
            StopMovement();
            onArrived?.Invoke();
        }
    }
}
