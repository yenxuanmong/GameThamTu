// ============================================
// PasswordInputDialog — modal dialog for password-protected rooms
// ============================================
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class PasswordInputDialog : MonoBehaviour
    {
        public static PasswordInputDialog Instance { get; private set; }

        [SerializeField] private GameObject     _panel;
        [SerializeField] private TMP_Text       _titleText;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Button         _confirmBtn;
        [SerializeField] private Button         _cancelBtn;
        [SerializeField] private TMP_Text       _errorText;

        private Action<string> _onConfirm;
        private Action         _onCancel;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _panel?.SetActive(false);
        }

        // ---- Open dialog ----

        public void Show(string title, Action<string> onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            if (_titleText)    _titleText.text    = title;
            if (_passwordInput) _passwordInput.text = "";
            if (_errorText)    _errorText.text    = "";

            _panel?.SetActive(true);
            _passwordInput?.Select();
        }

        public void ShowError(string msg)
        {
            if (_errorText) _errorText.text = msg;
        }

        // ---- Buttons ----

        public void OnClickConfirm()
        {
            string pwd = _passwordInput ? _passwordInput.text : "";
            if (string.IsNullOrEmpty(pwd))
            { if (_errorText) _errorText.text = "Please enter the room password."; return; }

            _panel?.SetActive(false);
            _onConfirm?.Invoke(pwd);
        }

        public void OnClickCancel()
        {
            _panel?.SetActive(false);
            _onCancel?.Invoke();
        }

        // ---- Submit on Enter ----
        public void OnPasswordSubmit(string _) => OnClickConfirm();
    }
}
