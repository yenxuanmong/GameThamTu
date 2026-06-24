// ============================================
// ProfileUI — view and edit player profile
// Attach to a Profile panel in any scene
// ============================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;
using DetectiveRoyale.Authentication;

namespace DetectiveRoyale.UI
{
    [Serializable] public class UpdateProfileBody { public string avatarUrl; }
    [Serializable] public class PreferencesBody   { public string preferredDifficulty; public bool enableVoiceChat; public bool enableNotifications; }
    [Serializable] public class ChangePassBody    { public string currentPassword; public string newPassword; }
    [Serializable] public class UpdateResponse    { public bool success; }

    public class ProfileUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Profile display")]
        [SerializeField] private TMP_Text   _usernameText;
        [SerializeField] private TMP_Text   _emailText;
        [SerializeField] private TMP_Text   _tierText;
        [SerializeField] private TMP_Text   _pointsText;
        [SerializeField] private TMP_Text   _winsText;
        [SerializeField] private TMP_Text   _winRateText;
        [SerializeField] private TMP_Text   _streakText;
        [SerializeField] private TMP_Text   _matchesText;
        [SerializeField] private TMP_Text   _perfectSolvesText;
        [SerializeField] private Image      _avatarImage;
        [SerializeField] private TMP_Text   _memberSinceText;

        [Header("Change password")]
        [SerializeField] private GameObject     _changePassPanel;
        [SerializeField] private TMP_InputField _currentPassInput;
        [SerializeField] private TMP_InputField _newPassInput;
        [SerializeField] private TMP_InputField _confirmPassInput;
        [SerializeField] private TMP_Text       _changePassError;
        [SerializeField] private TMP_Text       _changePassSuccess;
        [SerializeField] private Button         _savePassBtn;

        [Header("Preferences")]
        [SerializeField] private TMP_Dropdown   _prefDifficultyDropdown;
        [SerializeField] private Toggle         _voiceChatToggle;
        [SerializeField] private Toggle         _notificationsToggle;
        [SerializeField] private Button         _savePrefsBtn;
        [SerializeField] private TMP_Text       _prefsSavedText;

        [Header("Avatar upload")]
        [SerializeField] private Button         _uploadAvatarBtn;
        [SerializeField] private TMP_Text       _uploadStatusText;

        [Header("Loading")]
        [SerializeField] private GameObject     _loadingOverlay;

        private static readonly string[] DifficultyOptions =
            { "easy", "medium", "hard", "expert", "nightmare" };
        private static readonly string[] DifficultyLabels  =
            { "Easy", "Medium", "Hard", "Expert", "Nightmare" };

        void Start() => _panel?.SetActive(false);

        // ============================================
        // Open / Close
        // ============================================

        public void Open()
        {
            _panel?.SetActive(true);
            StartCoroutine(LoadProfile());
        }

        public void Close()
        {
            _panel?.SetActive(false);
            _changePassPanel?.SetActive(false);
        }

        // ============================================
        // Load profile
        // ============================================

        private IEnumerator LoadProfile()
        {
            _loadingOverlay?.SetActive(true);

            yield return AuthAPI.Instance.GetProfile(
                p => PopulateUI(p),
                err => Debug.LogWarning($"[ProfileUI] Load error: {err}"));

            _loadingOverlay?.SetActive(false);
        }

        private void PopulateUI(PlayerProfile p)
        {
            if (_usernameText)    _usernameText.text    = p.username;
            if (_emailText)       _emailText.text       = p.email;
            if (_tierText)        _tierText.text        = p.rank?.tier.ToTitleCase() ?? "Rookie";
            if (_pointsText)      _pointsText.text      = $"{p.rank?.points ?? 0} RP";
            if (_winsText)        _winsText.text        = $"{p.rank?.wins ?? 0} W / {p.rank?.losses ?? 0} L";
            if (_streakText)
            {
                int s = p.rank?.streak ?? 0;
                _streakText.text  = s >= 0 ? $"🔥 {s}-win streak" : $"❄ {-s}-loss streak";
                _streakText.color = s >= 0 ? Color.green : new Color(0.4f, 0.8f, 1f);
            }
            if (_matchesText)       _matchesText.text       = $"{p.stats?.totalMatches ?? 0} matches";
            if (_perfectSolvesText) _perfectSolvesText.text = $"{p.stats?.perfectSolves ?? 0} perfect solves";

            float wr = p.stats != null && p.stats.totalMatches > 0
                ? (float)p.stats.totalWins / p.stats.totalMatches : 0f;
            if (_winRateText) _winRateText.text = $"{wr * 100:F0}% win rate";

            // Preferences
            if (_prefDifficultyDropdown != null && p.preferences != null)
            {
                int idx = Array.IndexOf(DifficultyOptions, p.preferences.preferredDifficulty);
                _prefDifficultyDropdown.value = Mathf.Max(0, idx);
            }
            if (_voiceChatToggle     && p.preferences != null) _voiceChatToggle.isOn     = p.preferences.enableVoiceChat;
            if (_notificationsToggle && p.preferences != null) _notificationsToggle.isOn = p.preferences.enableNotifications;

            // Avatar
            if (!string.IsNullOrEmpty(p.avatarUrl))
                StartCoroutine(LoadAvatar(p.avatarUrl));
        }

        private IEnumerator LoadAvatar(string url)
        {
            using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success && _avatarImage)
            {
                var tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
                _avatarImage.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            }
        }

        // ============================================
        // Save preferences
        // ============================================

        public void OnClickSavePrefs()
        {
            int    diffIdx = _prefDifficultyDropdown ? _prefDifficultyDropdown.value : 1;
            string diff    = (diffIdx < DifficultyOptions.Length) ? DifficultyOptions[diffIdx] : "medium";
            bool   voice   = _voiceChatToggle     && _voiceChatToggle.isOn;
            bool   notifs  = _notificationsToggle && _notificationsToggle.isOn;

            if (_savePrefsBtn)   _savePrefsBtn.interactable = false;
            if (_prefsSavedText) _prefsSavedText.text = "";

            StartCoroutine(SavePreferences(diff, voice, notifs));
        }

        private IEnumerator SavePreferences(string difficulty, bool voice, bool notifs)
        {
            var body = new { preferences = new PreferencesBody
            {
                preferredDifficulty = difficulty,
                enableVoiceChat     = voice,
                enableNotifications = notifs
            }};

            yield return ApiClient.Instance.Patch<UpdateResponse>("/auth/me", body,
                _ =>
                {
                    if (_prefsSavedText) _prefsSavedText.text = "✓ Saved";
                    if (_savePrefsBtn)   _savePrefsBtn.interactable = true;
                    NotificationToast.Show("Preferences saved.", "success");
                },
                err =>
                {
                    if (_savePrefsBtn) _savePrefsBtn.interactable = true;
                    NotificationToast.Show($"Save failed: {err}", "error");
                });
        }

        // ============================================
        // Change password
        // ============================================

        public void OnClickShowChangePassword()
        {
            _changePassPanel?.SetActive(true);
            ClearChangePassFields();
        }

        public void OnClickCancelChangePassword()
        {
            _changePassPanel?.SetActive(false);
        }

        public void OnClickSavePassword()
        {
            string current = _currentPassInput ? _currentPassInput.text : "";
            string next    = _newPassInput     ? _newPassInput.text     : "";
            string confirm = _confirmPassInput ? _confirmPassInput.text : "";

            SetPassError("");
            SetPassSuccess("");

            if (string.IsNullOrEmpty(current)) { SetPassError("Enter your current password."); return; }
            if (next.Length < 8)               { SetPassError("New password must be 8+ characters."); return; }
            if (next != confirm)               { SetPassError("Passwords do not match."); return; }

            if (_savePassBtn) _savePassBtn.interactable = false;

            StartCoroutine(AuthAPI.Instance.ChangePassword(current, next,
                () =>
                {
                    SetPassSuccess("✓ Password updated.");
                    ClearChangePassFields();
                    if (_savePassBtn) _savePassBtn.interactable = true;
                },
                err =>
                {
                    SetPassError(err);
                    if (_savePassBtn) _savePassBtn.interactable = true;
                }));
        }

        private void ClearChangePassFields()
        {
            if (_currentPassInput) _currentPassInput.text = "";
            if (_newPassInput)     _newPassInput.text     = "";
            if (_confirmPassInput) _confirmPassInput.text = "";
            SetPassError(""); SetPassSuccess("");
        }

        private void SetPassError(string msg)   { if (_changePassError)   _changePassError.text   = msg; }
        private void SetPassSuccess(string msg) { if (_changePassSuccess) _changePassSuccess.text = msg; }

        // ============================================
        // Avatar upload (native file dialog — PC/Mac only)
        // ============================================

        public void OnClickUploadAvatar()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            StartCoroutine(PickAndUploadAvatar());
#else
            NotificationToast.Show("Avatar upload not supported on this platform.", "warning");
#endif
        }

        private IEnumerator PickAndUploadAvatar()
        {
            // Use NativeFilePicker or SimpleFileBrowser if available.
            // Fallback: log a message.
            if (_uploadStatusText) _uploadStatusText.text = "File picker not configured. Set up NativeFilePicker.";
            yield return null;

            /* --- Example with NativeFilePicker (uncomment when package is installed) ---
            string[] fileTypes = { "image/jpeg", "image/png", "image/webp" };
            NativeFilePicker.PickFile(path =>
            {
                if (!string.IsNullOrEmpty(path))
                    StartCoroutine(UploadAvatar(path));
            }, fileTypes);
            */
        }

        private IEnumerator UploadAvatar(string filePath)
        {
            if (_uploadStatusText) _uploadStatusText.text = "Uploading...";

            byte[] fileData = System.IO.File.ReadAllBytes(filePath);
            string ext      = System.IO.Path.GetExtension(filePath).ToLower();
            string mimeType = ext == ".png" ? "image/png" : "image/jpeg";

            var form = new UnityEngine.Networking.WWWForm();
            form.AddBinaryData("avatar", fileData, System.IO.Path.GetFileName(filePath), mimeType);

            string url = $"{GameConfig.Instance.ApiBase}/auth/me/avatar";
            using var req = UnityEngine.Networking.UnityWebRequest.Post(url, form);

            string token = AuthState.Instance.AccessToken;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                if (_uploadStatusText) _uploadStatusText.text = "✓ Avatar updated";
                NotificationToast.Show("Avatar updated.", "success");
                StartCoroutine(LoadProfile());
            }
            else
            {
                if (_uploadStatusText) _uploadStatusText.text = "Upload failed.";
                NotificationToast.Show("Avatar upload failed.", "error");
            }
        }
    }
}
