// ============================================
// LanguageSelector — UI dropdown to switch language
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class LanguageSelector : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;

        void Start()
        {
            PopulateDropdown();
        }

        private void PopulateDropdown()
        {
            if (_dropdown == null) return;
            _dropdown.ClearOptions();

            var opts = new System.Collections.Generic.List<string>();
            int selected = 0;
            string current = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            for (int i = 0; i < LocalizationManager.SupportedLanguages.Length; i++)
            {
                var (code, label) = LocalizationManager.SupportedLanguages[i];
                opts.Add(label);
                if (code == current) selected = i;
            }

            _dropdown.AddOptions(opts);
            _dropdown.SetValueWithoutNotify(selected);
            _dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(int idx)
        {
            if (idx < 0 || idx >= LocalizationManager.SupportedLanguages.Length) return;
            string code = LocalizationManager.SupportedLanguages[idx].code;
            LocalizationManager.Instance?.LoadLanguage(code);
        }
    }
}
