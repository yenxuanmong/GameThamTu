// ============================================
// MainMenuUI — main menu screen controller
// Attach to the MainMenu scene root
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;
using DetectiveRoyale.Authentication;

namespace DetectiveRoyale.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _profilePanel;
        [SerializeField] private GameObject _creditsPanel;

        [Header("Profile preview (top bar)")]
        [SerializeField] private TMP_Text  _usernameText;
        [SerializeField] private TMP_Text  _tierText;
        [SerializeField] private TMP_Text  _pointsText;
        [SerializeField] private Image     _avatarImage;

        [Header("Buttons")]
        [SerializeField] private Button    _playBtn;
        [SerializeField] private Button    _profileBtn;
        [SerializeField] private Button    _settingsBtn;
        [SerializeField] private Button    _logoutBtn;

        [Header("Version")]
        [SerializeField] private TMP_Text  _versionText;

        void Start()
        {
            // Wire CoreManagers if not already in scene
            EnsureCoreManagers();

            if (_versionText) _versionText.text = $"v{Application.version}";

            ShowMain();
            RefreshProfileBar();
        }

        // ============================================
        // Navigation
        // ============================================

        private void ShowMain()
        {
            _mainPanel?.SetActive(true);
            _settingsPanel?.SetActive(false);
            _profilePanel?.SetActive(false);
            _creditsPanel?.SetActive(false);
        }

        public void OnClickPlay()
        {
            if (!AuthState.Instance.IsLoggedIn)
            { SceneLoader.Instance.LoadScene(SceneLoader.SCENE_MAIN_MENU); return; }

            SceneLoader.Instance.LoadScene(SceneLoader.SCENE_LOBBY);
        }

        public void OnClickProfile()
        {
            _mainPanel?.SetActive(false);
            _profilePanel?.SetActive(true);
            // Refresh from server
            StartCoroutine(AuthAPI.Instance.GetProfile(
                _ => RefreshProfileBar(),
                _ => { }));
        }

        public void OnClickSettings()
        {
            _mainPanel?.SetActive(false);
            _settingsPanel?.SetActive(true);
        }

        public void OnClickCredits()
        {
            _mainPanel?.SetActive(false);
            _creditsPanel?.SetActive(true);
        }

        public void OnClickBack() => ShowMain();

        public void OnClickLogout()
        {
            StartCoroutine(AuthAPI.Instance.Logout(() =>
                SceneLoader.Instance.LoadScene(SceneLoader.SCENE_MAIN_MENU)));
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ============================================
        // Profile bar
        // ============================================

        private void RefreshProfileBar()
        {
            var p = AuthState.Instance.Player;
            if (p == null) return;
            if (_usernameText) _usernameText.text = p.username;
            if (_tierText)     _tierText.text     = $"{p.rank?.tier ?? "rookie"}";
            if (_pointsText)   _pointsText.text   = $"{p.rank?.points ?? 0} RP";

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
        // Ensure core manager GameObjects exist
        // ============================================

        private void EnsureCoreManagers()
        {
            if (AuthState.Instance == null)
                new GameObject("AuthState").AddComponent<AuthState>();
            if (ApiClient.Instance == null)
                new GameObject("ApiClient").AddComponent<ApiClient>();
            if (SocketManager.Instance == null)
                new GameObject("SocketManager").AddComponent<SocketManager>();
            if (SceneLoader.Instance == null)
                new GameObject("SceneLoader").AddComponent<SceneLoader>();
            if (UnityMainThreadDispatcher.Instance == null)
                new GameObject("Dispatcher").AddComponent<UnityMainThreadDispatcher>();
            if (AuthAPI.Instance == null)
                new GameObject("AuthAPI").AddComponent<AuthAPI>();
        }
    }
}
