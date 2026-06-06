// ============================================
// NPCInteractionZone — proximity trigger for witness interaction
// Shows "Press E to talk" prompt when player enters range
// ============================================
using UnityEngine;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    public class NPCInteractionZone : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float     _interactRadius = 2.5f;
        [SerializeField] private string    _witnessId;
        [SerializeField] private KeyCode   _interactKey = KeyCode.E;

        [Header("Prompt UI")]
        [SerializeField] private GameObject _promptPanel;
        [SerializeField] private TMP_Text   _promptText;
        [SerializeField] private string     _promptMessage = "Press E to talk";

        private bool     _playerInRange;
        private Witness  _witnessData;
        private Transform _playerTransform;

        void Start()
        {
            _promptPanel?.SetActive(false);

            if (GameSession.Witnesses != null)
                foreach (var w in GameSession.Witnesses)
                    if (w.id == _witnessId) { _witnessData = w; break; }

            if (_promptText) _promptText.text = _promptMessage;
        }

        void Update()
        {
            CheckProximity();

            if (_playerInRange && Input.GetKeyDown(_interactKey))
                Interact();
        }

        private void CheckProximity()
        {
            if (_playerTransform == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) _playerTransform = pc.transform;
            }

            if (_playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = dist <= _interactRadius;

            if (inRange != _playerInRange)
            {
                _playerInRange = inRange;
                _promptPanel?.SetActive(inRange);
            }
        }

        private void Interact()
        {
            if (_witnessData != null)
                NPCManager.Instance?.OpenWitness(_witnessData);
        }

        // 3D scene click fallback
        void OnMouseDown() => Interact();

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
