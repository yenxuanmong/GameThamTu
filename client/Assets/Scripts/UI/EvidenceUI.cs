// ============================================
// EvidenceUI — Evidence inventory panel in the investigation scene
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;
using DetectiveRoyale.Investigation;

namespace DetectiveRoyale.UI
{
    public class EvidenceUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;

        [Header("List")]
        [SerializeField] private Transform   _listContent;
        [SerializeField] private GameObject  _evidenceItemPrefab;

        [Header("Filter")]
        [SerializeField] private TMP_Dropdown _typeFilter;

        [Header("Counter")]
        [SerializeField] private TMP_Text    _countText;

        private List<Evidence> _items = new();

        void Start()
        {
            _panel?.SetActive(false);
            // Listen for new evidence
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded += OnEvidenceAdded;
        }

        void OnDestroy()
        {
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded -= OnEvidenceAdded;
        }

        public void Toggle()
        {
            bool on = !(_panel?.activeSelf ?? false);
            _panel?.SetActive(on);
            if (on) Refresh();
        }

        private void Refresh()
        {
            _items.Clear();
            if (EvidenceSystem.Instance != null)
                _items.AddRange(EvidenceSystem.Instance.DiscoveredEvidence);

            ClearList();
            foreach (var ev in _items)
                SpawnItem(ev);

            if (_countText) _countText.text = $"Evidence: {_items.Count}";
        }

        private void OnEvidenceAdded(Evidence ev)
        {
            _items.Add(ev);
            if (_panel?.activeSelf ?? false) SpawnItem(ev);
            if (_countText) _countText.text = $"Evidence: {_items.Count}";
        }

        private void SpawnItem(Evidence ev)
        {
            if (_evidenceItemPrefab == null || _listContent == null) return;
            var go = Instantiate(_evidenceItemPrefab, _listContent);
            var item = go.GetComponent<EvidenceListItem>();
            item?.Setup(ev, () => EvidenceScanner.Instance?.Open(ev));
        }

        private void ClearList()
        {
            foreach (Transform t in _listContent) Destroy(t.gameObject);
        }
    }

    // ---- Evidence list row ----
    public class EvidenceListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text  _nameText;
        [SerializeField] private TMP_Text  _typeText;
        [SerializeField] private Button    _viewBtn;
        private System.Action _onView;

        public void Setup(Evidence ev, System.Action onView)
        {
            _onView = onView;
            if (_nameText) _nameText.text = ev.name;
            if (_typeText) _typeText.text = ev.type.ToUpper();
        }

        public void OnClickView() => _onView?.Invoke();
    }
}
