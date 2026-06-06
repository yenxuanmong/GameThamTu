// ============================================
// CreateRoomUI — standalone Create Room form
// Works with LobbyManager to create rooms via REST + Socket
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    [System.Serializable]
    class CreateRoomPayload
    {
        public string name;
        public string difficulty;
        public int    maxPlayers;
        public string visibility;
        public string password;
    }

    [System.Serializable]
    class CreateRoomResponse { public Room room; }

    public class CreateRoomUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject     _panel;
        [SerializeField] private GameObject     _loadingOverlay;

        [Header("Form")]
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private DifficultySelector _difficultySelector;
        [SerializeField] private Slider         _maxPlayersSlider;
        [SerializeField] private TMP_Text       _maxPlayersLabel;
        [SerializeField] private Toggle         _privateToggle;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private GameObject     _passwordRow;
        [SerializeField] private Toggle         _voiceChatToggle;
        [SerializeField] private Toggle         _autoStartToggle;

        [Header("Case selection (optional)")]
        [SerializeField] private TMP_Text       _selectedCaseLabel;
        [SerializeField] private Button         _pickCaseBtn;
        [SerializeField] private CaseBrowserUI  _caseBrowser;

        [Header("Buttons")]
        [SerializeField] private Button   _createBtn;
        [SerializeField] private Button   _cancelBtn;
        [SerializeField] private TMP_Text _errorText;

        private string _selectedCaseId;
        private string _selectedCaseTitle;

        void Start()
        {
            _panel?.SetActive(false);
            _passwordRow?.SetActive(false);

            if (_caseBrowser != null)
                _caseBrowser.OnCaseSelected += OnCaseSelected;
        }

        void OnDestroy()
        {
            if (_caseBrowser != null)
                _caseBrowser.OnCaseSelected -= OnCaseSelected;
        }

        // ============================================
        // Open / Close
        // ============================================

        public void Open()
        {
            _panel?.SetActive(true);
            ClearForm();
        }

        public void Close() => _panel?.SetActive(false);

        // ============================================
        // Form interactions
        // ============================================

        public void OnMaxPlayersChanged(float val)
        {
            if (_maxPlayersLabel)
                _maxPlayersLabel.text = $"{Mathf.RoundToInt(val)} players";
        }

        public void OnPrivateToggleChanged(bool isPrivate)
        {
            _passwordRow?.SetActive(isPrivate);
        }

        public void OnClickPickCase()
        {
            _caseBrowser?.Open();
        }

        private void OnCaseSelected(string caseId, string caseTitle)
        {
            _selectedCaseId    = caseId;
            _selectedCaseTitle = caseTitle;
            if (_selectedCaseLabel)
                _selectedCaseLabel.text = caseTitle;
        }

        // ============================================
        // Create
        // ============================================

        public void OnClickCreate()
        {
            if (_errorText) _errorText.text = "";

            string name = _nameInput ? _nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(name))
            {
                SetError("Please enter a room name.");
                return;
            }
            if (name.Length > GameConstants.MAX_ROOM_NAME_LENGTH)
            {
                SetError($"Room name too long (max {GameConstants.MAX_ROOM_NAME_LENGTH} chars).");
                return;
            }

            bool   isPrivate = _privateToggle && _privateToggle.isOn;
            string password  = (isPrivate && _passwordInput) ? _passwordInput.text : null;
            if (isPrivate && string.IsNullOrEmpty(password))
            {
                SetError("Enter a password for the private room.");
                return;
            }

            string difficulty  = _difficultySelector?.SelectedValue ?? "medium";
            int    maxPlayers  = _maxPlayersSlider ? Mathf.RoundToInt(_maxPlayersSlider.value) : 4;

            StartCoroutine(DoCreate(name, difficulty, maxPlayers,
                isPrivate ? "private" : "public", password));
        }

        private IEnumerator DoCreate(string name, string difficulty,
            int maxPlayers, string visibility, string password)
        {
            SetLoading(true);

            var body = new CreateRoomPayload
            {
                name       = name,
                difficulty = difficulty,
                maxPlayers = maxPlayers,
                visibility = visibility,
                password   = password,
            };

            yield return ApiClient.Instance.Post<CreateRoomResponse>(
                Api.Rooms.List, body,
                resp =>
                {
                    SetLoading(false);
                    SocketManager.Instance.JoinRoom(resp.room.id);
                    Close();
                    UI.NotificationToast.Show($"Room '{name}' created!", "success");
                },
                err =>
                {
                    SetLoading(false);
                    SetError(err);
                });
        }

        // ============================================
        // Helpers
        // ============================================

        private void SetLoading(bool on)
        {
            _loadingOverlay?.SetActive(on);
            if (_createBtn) _createBtn.interactable = !on;
        }

        private void SetError(string msg)
        {
            if (_errorText) _errorText.text = msg;
        }

        private void ClearForm()
        {
            if (_nameInput)    _nameInput.text = "";
            if (_errorText)    _errorText.text = "";
            if (_passwordInput)_passwordInput.text = "";
            _passwordRow?.SetActive(false);
            _selectedCaseId    = null;
            _selectedCaseTitle = null;
            if (_selectedCaseLabel) _selectedCaseLabel.text = "Any case";
        }
    }
}
