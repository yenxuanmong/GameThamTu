// ============================================
// LoginManager — drives the Login/Register UI
// Attach to a GameObject in the MainMenu scene
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Authentication
{
    public class LoginManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private GameObject _registerPanel;
        [SerializeField] private GameObject _forgotPanel;
        [SerializeField] private GameObject _loadingOverlay;

        [Header("Login Fields")]
        [SerializeField] private TMP_InputField _loginEmail;
        [SerializeField] private TMP_InputField _loginPassword;
        [SerializeField] private TMP_Text        _loginError;
        [SerializeField] private Button          _loginBtn;

        [Header("Register Fields")]
        [SerializeField] private TMP_InputField _regUsername;
        [SerializeField] private TMP_InputField _regEmail;
        [SerializeField] private TMP_InputField _regPassword;
        [SerializeField] private TMP_InputField _regPasswordConfirm;
        [SerializeField] private TMP_Text        _regError;
        [SerializeField] private Button          _registerBtn;

        [Header("Forgot Password Fields")]
        [SerializeField] private TMP_InputField _forgotEmail;
        [SerializeField] private TMP_Text        _forgotMessage;
        [SerializeField] private Button          _forgotSendBtn;

        void Start()
        {
            // Auto-login if token is present
            if (AuthState.Instance.IsLoggedIn)
            {
                StartCoroutine(AutoLogin());
                return;
            }
            ShowLogin();
        }

        // ============================================
        // Auto-login
        // ============================================

        private IEnumerator AutoLogin()
        {
            SetLoading(true);
            yield return StartCoroutine(AuthAPI.Instance.GetProfile(
                _ => SceneLoader.Instance.LoadScene(SceneLoader.SCENE_LOBBY),
                _ =>
                {
                    // Token expired — try refresh
                    StartCoroutine(AuthState.Instance.TryRefreshToken(ok =>
                    {
                        if (ok) SceneLoader.Instance.LoadScene(SceneLoader.SCENE_LOBBY);
                        else    { SetLoading(false); ShowLogin(); }
                    }));
                }
            ));
        }

        // ============================================
        // Panel navigation
        // ============================================

        public void ShowLogin()    { _loginPanel?.SetActive(true);  _registerPanel?.SetActive(false); _forgotPanel?.SetActive(false); }
        public void ShowRegister() { _loginPanel?.SetActive(false); _registerPanel?.SetActive(true);  _forgotPanel?.SetActive(false); }
        public void ShowForgot()   { _loginPanel?.SetActive(false); _registerPanel?.SetActive(false); _forgotPanel?.SetActive(true); }

        // ============================================
        // Login
        // ============================================

        public void OnClickLogin()
        {
            string email = _loginEmail?.text.Trim();
            string pass  = _loginPassword?.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            { SetLoginError("Please enter your email and password."); return; }

            SetLoading(true);
            StartCoroutine(AuthAPI.Instance.Login(email, pass,
                _ =>
                {
                    SetLoading(false);
                    SceneLoader.Instance.LoadScene(SceneLoader.SCENE_LOBBY);
                },
                err =>
                {
                    SetLoading(false);
                    SetLoginError(err);
                }
            ));
        }

        // ============================================
        // Register
        // ============================================

        public void OnClickRegister()
        {
            string user  = _regUsername?.text.Trim();
            string email = _regEmail?.text.Trim();
            string pass  = _regPassword?.text;
            string conf  = _regPasswordConfirm?.text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            { SetRegError("All fields are required."); return; }

            if (pass != conf)
            { SetRegError("Passwords do not match."); return; }

            if (pass.Length < 8)
            { SetRegError("Password must be at least 8 characters."); return; }

            SetLoading(true);
            StartCoroutine(AuthAPI.Instance.Register(user, email, pass,
                _ =>
                {
                    SetLoading(false);
                    SceneLoader.Instance.LoadScene(SceneLoader.SCENE_LOBBY);
                },
                err =>
                {
                    SetLoading(false);
                    SetRegError(err);
                }
            ));
        }

        // ============================================
        // Forgot password
        // ============================================

        public void OnClickForgotSend()
        {
            string email = _forgotEmail?.text.Trim();
            if (string.IsNullOrEmpty(email))
            { if (_forgotMessage) _forgotMessage.text = "Please enter your email."; return; }

            SetLoading(true);
            StartCoroutine(AuthAPI.Instance.ForgotPassword(email,
                msg =>
                {
                    SetLoading(false);
                    if (_forgotMessage) _forgotMessage.text = msg;
                },
                err =>
                {
                    SetLoading(false);
                    if (_forgotMessage) _forgotMessage.text = err;
                }
            ));
        }

        // ============================================
        // Helpers
        // ============================================

        private void SetLoading(bool on)
        {
            _loadingOverlay?.SetActive(on);
            if (_loginBtn)    _loginBtn.interactable    = !on;
            if (_registerBtn) _registerBtn.interactable = !on;
            if (_forgotSendBtn) _forgotSendBtn.interactable = !on;
        }

        private void SetLoginError(string msg) { if (_loginError) _loginError.text = msg; }
        private void SetRegError(string msg)   { if (_regError)   _regError.text   = msg; }
    }
}
