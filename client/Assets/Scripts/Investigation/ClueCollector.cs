// ============================================
// ClueCollector — clickable evidence object in the scene
// Attach to any 3D/2D GameObject that represents a clue
// ============================================
using UnityEngine;
using UnityEngine.EventSystems;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class ClueCollector : MonoBehaviour, IPointerClickHandler
    {
        [Header("Evidence")]
        [SerializeField] private string _evidenceId;
        [SerializeField] private string _evidenceName;

        [Header("Highlight")]
        [SerializeField] private GameObject _glowEffect;
        [SerializeField] private bool       _oneTimeCollect = true;

        private bool _collected;

        void Start()
        {
            if (_glowEffect) _glowEffect.SetActive(true);
        }

        // ---- Mouse / touch click ----
        public void OnPointerClick(PointerEventData _)  => Collect();
        void OnMouseDown()                               => Collect();

        public void Collect()
        {
            if (_collected && _oneTimeCollect) return;

            EvidenceSystem.Instance?.ExamineObject(_evidenceId);

            if (_oneTimeCollect)
            {
                _collected = true;
                if (_glowEffect) _glowEffect.SetActive(false);
                NotificationToast.Show($"Evidence collected: {_evidenceName}", "info");
            }
        }
    }
}
