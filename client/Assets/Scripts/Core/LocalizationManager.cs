// ============================================
// LocalizationManager — simple key-based localisation
// Loads JSON language files from Resources/Localization/
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        private Dictionary<string, string> _strings = new();

        public string CurrentLanguage { get; private set; } = "en";

        private const string KEY_LANG = "dr_language";

        public static event Action OnLanguageChanged;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            string saved = PlayerPrefs.GetString(KEY_LANG, "en");
            LoadLanguage(saved);
        }

        // ============================================
        // Load
        // ============================================

        public void LoadLanguage(string langCode)
        {
            var asset = Resources.Load<TextAsset>($"Localization/{langCode}");
            if (asset == null)
            {
                Debug.LogWarning($"[Localization] No file for language: {langCode}. Falling back to 'en'.");
                asset = Resources.Load<TextAsset>("Localization/en");
            }

            if (asset == null)
            {
                Debug.LogWarning("[Localization] Default language file not found. Localization disabled.");
                return;
            }

            _strings.Clear();
            CurrentLanguage = langCode;
            PlayerPrefs.SetString(KEY_LANG, langCode);

            // Parse simple flat JSON: { "key": "value" }
            try
            {
                var data = JsonUtility.FromJson<LocalizationData>(asset.text);
                if (data?.entries != null)
                    foreach (var e in data.entries)
                        _strings[e.key] = e.value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] Parse error: {ex.Message}");
            }

            OnLanguageChanged?.Invoke();
        }

        // ============================================
        // Get
        // ============================================

        public string Get(string key, string fallback = null)
        {
            return _strings.TryGetValue(key, out string value)
                ? value
                : fallback ?? key;
        }

        /// <summary>Shorthand: Loc.T("key")</summary>
        public static string T(string key, string fallback = null) =>
            Instance != null ? Instance.Get(key, fallback) : fallback ?? key;

        // ============================================
        // Supported languages
        // ============================================

        public static readonly (string code, string label)[] SupportedLanguages =
        {
            ("en", "English"),
            ("vi", "Tiếng Việt"),
            ("zh", "中文"),
            ("ja", "日本語"),
            ("ko", "한국어"),
            ("fr", "Français"),
            ("de", "Deutsch"),
            ("es", "Español"),
            ("pt", "Português"),
        };

        // ---- Serialisation helper ----
        [Serializable] private class LocalizationData   { public LocalizationEntry[] entries; }
        [Serializable] private class LocalizationEntry  { public string key; public string value; }
    }
}
