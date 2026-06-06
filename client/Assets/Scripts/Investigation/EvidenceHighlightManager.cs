// ============================================
// EvidenceHighlightManager — manages glow/outline on all
// collectable evidence objects in the scene
// ============================================
using System.Collections.Generic;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class EvidenceHighlightManager : MonoBehaviour
    {
        public static EvidenceHighlightManager Instance { get; private set; }

        [SerializeField] private bool  _highlightAll      = true;
        [SerializeField] private bool  _pulseUncollected  = true;
        [SerializeField] private Color _uncollectedColor  = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color _collectedColor    = new Color(0.4f, 1f, 0.4f, 0.4f);
        [SerializeField] private float _discoveryRadius   = 30f;

        private List<ClueCollector> _allClues = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            RefreshClueList();
            EvidenceSystem.Instance?.OnEvidenceAdded   += OnEvidenceAdded;
        }

        void OnDestroy()
        {
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded -= OnEvidenceAdded;
        }

        // ---- Rebuild list after scene setup ----
        public void RefreshClueList()
        {
            _allClues.Clear();
            _allClues.AddRange(FindObjectsByType<ClueCollector>(FindObjectsSortMode.None));

            if (_highlightAll)
                foreach (var c in _allClues)
                    SetHighlight(c, false);
        }

        // ---- Mark evidence as found ----
        private void OnEvidenceAdded(Evidence ev)
        {
            foreach (var c in _allClues)
            {
                var highlight = c.GetComponent<InteractableHighlight>();
                if (highlight != null)
                    highlight.MarkCollected();
            }
        }

        // ---- Reveal nearby evidence (hint integration) ----
        public void RevealNearby(Vector3 playerPos, float radius = -1f)
        {
            float r = radius < 0 ? _discoveryRadius : radius;
            foreach (var c in _allClues)
            {
                if (Vector3.Distance(c.transform.position, playerPos) <= r)
                {
                    var highlight = c.GetComponent<InteractableHighlight>();
                    if (highlight != null)
                    {
                        // Flash to draw attention
                        StartCoroutine(FlashHighlight(highlight));
                    }
                }
            }
        }

        private System.Collections.IEnumerator FlashHighlight(InteractableHighlight h)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void SetHighlight(ClueCollector c, bool collected)
        {
            var h = c.GetComponent<InteractableHighlight>();
            if (h == null) return;
            if (collected) h.MarkCollected();
        }
    }
}
