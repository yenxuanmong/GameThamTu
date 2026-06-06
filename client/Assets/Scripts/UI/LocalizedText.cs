// ============================================
// LocalizedText — auto-updates TMP_Text when language changes
// Add this component alongside TMP_Text and set a localization key
// ============================================
using UnityEngine;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private string _fallback;

        private TMP_Text _text;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= Refresh;
        }

        public void SetKey(string key, string fallback = null)
        {
            _key      = key;
            _fallback = fallback;
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null || string.IsNullOrEmpty(_key)) return;
            _text.text = LocalizationManager.T(_key, _fallback ?? _key);
        }
    }
}
