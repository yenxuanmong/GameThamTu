// ============================================
// TabGroup — manages tabbed panel switching
// Generalised for InventoryUI, SettingsUI, ProfileUI etc.
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveRoyale.UI
{
    public class TabGroup : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public Button     button;
            public GameObject panel;
            public Image      indicator;  // underline or background
        }

        [SerializeField] private List<Tab> _tabs;
        [SerializeField] private Color     _activeColor   = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color     _inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private int       _defaultTab    = 0;

        private int _activeIndex = -1;

        void Start()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                int idx = i;
                _tabs[i].button?.onClick.AddListener(() => SelectTab(idx));
            }
            SelectTab(_defaultTab);
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (_activeIndex == index) return;

            _activeIndex = index;

            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = i == index;
                _tabs[i].panel?.SetActive(active);

                if (_tabs[i].indicator)
                    _tabs[i].indicator.color = active ? _activeColor : _inactiveColor;

                // Bold text for active tab
                var text = _tabs[i].button?.GetComponentInChildren<TMPro.TMP_Text>();
                if (text)
                    text.fontStyle = active
                        ? TMPro.FontStyles.Bold
                        : TMPro.FontStyles.Normal;
            }
        }

        public int ActiveIndex => _activeIndex;
    }
}
