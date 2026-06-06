// ============================================
// SettingsUI — audio, graphics, and game preferences
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Audio")]
        [SerializeField] private Slider   _masterVolumeSlider;
        [SerializeField] private Slider   _musicVolumeSlider;
        [SerializeField] private Slider   _sfxVolumeSlider;
        [SerializeField] private TMP_Text _masterLabel;
        [SerializeField] private TMP_Text _musicLabel;
        [SerializeField] private TMP_Text _sfxLabel;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown _qualityDropdown;
        [SerializeField] private Toggle       _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;

        [Header("Game")]
        [SerializeField] private Toggle   _showTimerToggle;
        [SerializeField] private Toggle   _autoSaveNotesToggle;
        [SerializeField] private Slider   _textSizeSlider;
        [SerializeField] private TMP_Text _textSizeLabel;

        // PlayerPrefs keys
        private const string K_MASTER = "vol_master";
        private const string K_MUSIC  = "vol_music";
        private const string K_SFX    = "vol_sfx";
        private const string K_QUALITY= "graphics_quality";
        private const string K_TIMER  = "show_timer";
        private const string K_AUTONOTE = "auto_notes";
        private const string K_TEXTSIZE = "text_size";

        void Start()
        {
            _panel?.SetActive(false);
            PopulateQualityDropdown();
            PopulateResolutionDropdown();
        }

        // ============================================
        // Open / Close
        // ============================================

        public void Open()
        {
            _panel?.SetActive(true);
            LoadSettings();
        }

        public void Close() => _panel?.SetActive(false);

        // ============================================
        // Load saved settings
        // ============================================

        private void LoadSettings()
        {
            float master = PlayerPrefs.GetFloat(K_MASTER, 1f);
            float music  = PlayerPrefs.GetFloat(K_MUSIC,  0.7f);
            float sfx    = PlayerPrefs.GetFloat(K_SFX,    1f);

            if (_masterVolumeSlider) { _masterVolumeSlider.value = master; UpdateLabel(_masterLabel, master); }
            if (_musicVolumeSlider)  { _musicVolumeSlider.value  = music;  UpdateLabel(_musicLabel, music);   }
            if (_sfxVolumeSlider)    { _sfxVolumeSlider.value    = sfx;    UpdateLabel(_sfxLabel, sfx);       }

            if (_qualityDropdown)    _qualityDropdown.value    = PlayerPrefs.GetInt(K_QUALITY, 2);
            if (_fullscreenToggle)   _fullscreenToggle.isOn    = Screen.fullScreen;
            if (_showTimerToggle)    _showTimerToggle.isOn     = PlayerPrefs.GetInt(K_TIMER, 1) == 1;
            if (_autoSaveNotesToggle)_autoSaveNotesToggle.isOn = PlayerPrefs.GetInt(K_AUTONOTE, 1) == 1;

            float textSize = PlayerPrefs.GetFloat(K_TEXTSIZE, 1f);
            if (_textSizeSlider) { _textSizeSlider.value = textSize; UpdateLabel(_textSizeLabel, textSize, "x"); }
        }

        // ============================================
        // Sliders (called by OnValueChanged events)
        // ============================================

        public void OnMasterVolumeChanged(float val)
        {
            UpdateLabel(_masterLabel, val);
            AudioListener.volume = val;
            PlayerPrefs.SetFloat(K_MASTER, val);
        }

        public void OnMusicVolumeChanged(float val)
        {
            UpdateLabel(_musicLabel, val);
            // Set music AudioMixer param if available
            PlayerPrefs.SetFloat(K_MUSIC, val);
        }

        public void OnSfxVolumeChanged(float val)
        {
            UpdateLabel(_sfxLabel, val);
            PlayerPrefs.SetFloat(K_SFX, val);
        }

        public void OnTextSizeChanged(float val)
        {
            UpdateLabel(_textSizeLabel, val, "x");
            PlayerPrefs.SetFloat(K_TEXTSIZE, val);
        }

        // ============================================
        // Toggles & dropdowns
        // ============================================

        public void OnQualityChanged(int idx)
        {
            QualitySettings.SetQualityLevel(idx);
            PlayerPrefs.SetInt(K_QUALITY, idx);
        }

        public void OnFullscreenToggled(bool on)
        {
            Screen.fullScreen = on;
        }

        public void OnResolutionChanged(int idx)
        {
            var res = Screen.resolutions;
            if (idx >= 0 && idx < res.Length)
                Screen.SetResolution(res[idx].width, res[idx].height, Screen.fullScreen);
        }

        public void OnShowTimerToggled(bool on)  => PlayerPrefs.SetInt(K_TIMER,   on ? 1 : 0);
        public void OnAutoNotesToggled(bool on)  => PlayerPrefs.SetInt(K_AUTONOTE, on ? 1 : 0);

        // ============================================
        // Save & Reset
        // ============================================

        public void OnClickSave()
        {
            PlayerPrefs.Save();
            NotificationToast.Show("Settings saved.", "success");
            Close();
        }

        public void OnClickReset()
        {
            PlayerPrefs.DeleteKey(K_MASTER);
            PlayerPrefs.DeleteKey(K_MUSIC);
            PlayerPrefs.DeleteKey(K_SFX);
            PlayerPrefs.DeleteKey(K_QUALITY);
            PlayerPrefs.DeleteKey(K_TIMER);
            PlayerPrefs.DeleteKey(K_AUTONOTE);
            PlayerPrefs.DeleteKey(K_TEXTSIZE);
            PlayerPrefs.Save();
            LoadSettings();
            NotificationToast.Show("Settings reset to defaults.", "info");
        }

        // ============================================
        // Helpers
        // ============================================

        private void PopulateQualityDropdown()
        {
            if (_qualityDropdown == null) return;
            _qualityDropdown.ClearOptions();
            _qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(
                QualitySettings.names));
        }

        private void PopulateResolutionDropdown()
        {
            if (_resolutionDropdown == null) return;
            _resolutionDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string>();
            foreach (var r in Screen.resolutions)
                opts.Add($"{r.width}×{r.height} @ {r.refreshRateRatio.numerator}Hz");
            _resolutionDropdown.AddOptions(opts);
        }

        private static void UpdateLabel(TMP_Text label, float val, string suffix = "")
        {
            if (label) label.text = suffix.Length > 0
                ? $"{val:F1}{suffix}"
                : $"{Mathf.RoundToInt(val * 100)}%";
        }
    }
}
