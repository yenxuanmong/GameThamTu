// ============================================
// RoomSettingsUI — in-room settings panel for the host
// Allows changing difficulty, max players, voice chat, auto-start
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    public class RoomSettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;

        [Header("Settings controls")]
        [SerializeField] private TMP_Dropdown _difficultyDropdown;
        [SerializeField] private Slider       _maxPlayersSlider;
        [SerializeField] private TMP_Text     _maxPlayersLabel;
        [SerializeField] private Toggle       _voiceChatToggle;
        [SerializeField] private Toggle       _autoStartToggle;
        [SerializeField] private Slider       _countdownSlider;
        [SerializeField] private TMP_Text     _countdownLabel;
        [SerializeField] private Toggle       _spectatorToggle;

        [Header("Buttons")]
        [SerializeField] private Button   _applyBtn;
        [SerializeField] private Button   _closeBtn;
        [SerializeField] private TMP_Text _statusText;

        [Header("Host only")]
        [SerializeField] private GameObject _hostOnlySection;

        private static readonly string[] DiffValues =
            { "easy", "medium", "hard", "expert", "nightmare" };

        private bool _isHost;

        void Start() => _panel?.SetActive(false);

        public void Open(Room room, bool isHost)
        {
            _isHost = isHost;
            _panel?.SetActive(true);
            _hostOnlySection?.SetActive(isHost);
            _applyBtn?.gameObject.SetActive(isHost);

            // Populate from current room settings
            if (room?.settings != null)
                LoadSettings(room.settings);
        }

        public void Close() => _panel?.SetActive(false);

        private void LoadSettings(RoomSettings s)
        {
            if (_difficultyDropdown != null)
            {
                int idx = System.Array.IndexOf(DiffValues, s.difficulty);
                _difficultyDropdown.value = Mathf.Max(0, idx);
            }
            if (_maxPlayersSlider != null)
            {
                _maxPlayersSlider.value = s.maxPlayers;
                if (_maxPlayersLabel) _maxPlayersLabel.text = s.maxPlayers.ToString();
            }
            if (_voiceChatToggle)   _voiceChatToggle.isOn   = s.enableVoiceChat;
            if (_autoStartToggle)   _autoStartToggle.isOn   = s.autoStart;
            if (_spectatorToggle)   _spectatorToggle.isOn   = s.allowSpectators;
            if (_countdownSlider != null)
            {
                _countdownSlider.value = s.startCountdownSeconds;
                if (_countdownLabel) _countdownLabel.text = $"{s.startCountdownSeconds}s";
            }
        }

        // ---- UI callbacks ----

        public void OnMaxPlayersChanged(float val)
        {
            if (_maxPlayersLabel) _maxPlayersLabel.text = Mathf.RoundToInt(val).ToString();
        }

        public void OnCountdownChanged(float val)
        {
            if (_countdownLabel) _countdownLabel.text = $"{Mathf.RoundToInt(val)}s";
        }

        public void OnClickApply()
        {
            if (!_isHost) return;
            if (_statusText) _statusText.text = "Settings applied.";
            // Room settings update is sent via REST PATCH /rooms/:id
            // TODO: wire to ApiClient.Patch when backend supports it
            Close();
        }
    }
}
