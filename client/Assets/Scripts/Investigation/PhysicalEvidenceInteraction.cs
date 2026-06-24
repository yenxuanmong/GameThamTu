// ============================================
// PhysicalEvidenceInteraction — handles collecting physical evidence
// Combines ClueCollector + InteractableHighlight + InspectObject
// ============================================
using UnityEngine;
using DetectiveRoyale.Core;
using DetectiveRoyale.UI;

namespace DetectiveRoyale.Investigation
{
    [RequireComponent(typeof(ClueCollector))]
    [RequireComponent(typeof(InteractableHighlight))]
    public class PhysicalEvidenceInteraction : MonoBehaviour
    {
        [Header("3D Inspect")]
        [SerializeField] private bool       _allowInspect3D = false;
        [SerializeField] private GameObject _inspectPrefab;      // 3D model for close-up

        [Header("Forensic View")]
        [SerializeField] private bool       _isForensicEvidence = false;

        [Header("Camera Footage")]
        [SerializeField] private bool       _isCameraEvidence = false;

        private ClueCollector           _collector;
        private InteractableHighlight   _highlight;
        private Evidence                _evidenceData;

        void Awake()
        {
            _collector = GetComponent<ClueCollector>();
            _highlight = GetComponent<InteractableHighlight>();
        }

        void Start()
        {
            // Hook into EvidenceSystem to know when THIS evidence was collected
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded += OnEvidenceAdded;
        }

        void OnDestroy()
        {
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded -= OnEvidenceAdded;
        }

        // Called when user clicks the object
        void OnMouseDown()
        {
            _collector?.Collect();
        }

        // Called by EvidenceSystem when this evidence is confirmed collected
        private void OnEvidenceAdded(Evidence ev)
        {
            // Match evidence id (set via ClueCollector._evidenceId via reflection or public accessor)
            _evidenceData = ev;
            _highlight?.MarkCollected();

            // Open appropriate viewer
            if (_isForensicEvidence)
                ForensicSystem.Instance?.Open(ev);   // Not static — find instance
            else if (_isCameraEvidence)
                CameraViewer.Instance?.Open(ev);      // Not static — find instance
            else if (_allowInspect3D && _inspectPrefab != null)
                InspectObject.Instance?.Inspect(_inspectPrefab, ev.name, ev.description);
            else
                EvidenceScanner.Instance?.Open(ev);
        }
    }

    // Add static Instance accessor on CameraViewer via extension pattern
    public static class CameraViewerExtensions
    {
        private static CameraViewer _cached;
        public static CameraViewer Instance
        {
            get
            {
                if (_cached == null)
                    _cached = Object.FindFirstObjectByType<CameraViewer>();
                return _cached;
            }
        }
    }
}
