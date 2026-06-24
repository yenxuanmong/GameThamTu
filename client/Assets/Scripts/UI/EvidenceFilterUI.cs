// ============================================
// EvidenceFilterUI — filter/sort the evidence list by type, location
// ============================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class EvidenceFilterUI : MonoBehaviour
    {
        [Header("Filter controls")]
        [SerializeField] private TMP_Dropdown _typeFilter;
        [SerializeField] private TMP_Dropdown _sortDropdown;
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private Button         _clearBtn;

        [Header("Results")]
        [SerializeField] private TMP_Text _countText;

        public event System.Action<IEnumerable<Evidence>> OnFilterChanged;

        private static readonly string[] TypeOptions =
            { "All", "Physical", "Digital", "Document", "Forensic", "Testimonial", "Environmental" };

        private static readonly string[] SortOptions =
            { "Newest First", "Oldest First", "Name A-Z", "Type" };

        void Start()
        {
            PopulateDropdowns();

            _typeFilter?.onValueChanged.AddListener(_ => Apply());
            _sortDropdown?.onValueChanged.AddListener(_ => Apply());
            _searchInput?.onValueChanged.AddListener(_ => Apply());
            _clearBtn?.onClick.AddListener(Clear);
        }

        private void PopulateDropdowns()
        {
            if (_typeFilter != null)
            {
                _typeFilter.ClearOptions();
                _typeFilter.AddOptions(new List<string>(TypeOptions));
            }
            if (_sortDropdown != null)
            {
                _sortDropdown.ClearOptions();
                _sortDropdown.AddOptions(new List<string>(SortOptions));
            }
        }

        public void Apply()
        {
            var all = Investigation.EvidenceSystem.Instance?.DiscoveredEvidence;
            if (all == null) return;

            IEnumerable<Evidence> filtered = all;

            // Type filter
            int typeIdx = _typeFilter ? _typeFilter.value : 0;
            if (typeIdx > 0)
            {
                string typeKey = TypeOptions[typeIdx].ToLower();
                filtered = filtered.Where(e => e.type == typeKey);
            }

            // Search
            string search = _searchInput ? _searchInput.text.Trim().ToLower() : "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(e =>
                    (e.name?.ToLower().Contains(search) ?? false) ||
                    (e.description?.ToLower().Contains(search) ?? false));

            // Sort
            int sortIdx = _sortDropdown ? _sortDropdown.value : 0;
            filtered = sortIdx switch
            {
                1 => filtered.OrderBy(e => e.discoveredAt),
                2 => filtered.OrderBy(e => e.name),
                3 => filtered.OrderBy(e => e.type),
                _ => filtered.OrderByDescending(e => e.discoveredAt)
            };

            var result = filtered.ToList();
            if (_countText) _countText.text = $"{result.Count} items";
            OnFilterChanged?.Invoke(result);
        }

        public void Clear()
        {
            if (_typeFilter)   _typeFilter.value   = 0;
            if (_sortDropdown) _sortDropdown.value  = 0;
            if (_searchInput)  _searchInput.text    = "";
            Apply();
        }
    }
}
