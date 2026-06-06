// ============================================
// ConfirmDialog — reusable yes/no confirmation modal
// ============================================
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class ConfirmDialog : MonoBehaviour
    {
        public static ConfirmDialog Instance { get; private set; }

        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text   _titleText;
        [SerializeField] private TMP_Text   _bodyText;
        [SerializeField] private Button     _confirmBtn;
        [SerializeField] private TMP_Text   _confirmLabel;
        [SerializeField] private Button     _cancelBtn;
        [SerializeField] private TMP_Text   _cancelLabel;

        private Action _onConfirm;
        private Action _onCancel;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _panel?.SetActive(false);
        }

        // ---- Show ----

        public void Show(
            string title,
            string body,
            Action onConfirm,
            Action onCancel      = null,
            string confirmText   = "Confirm",
            string cancelText    = "Cancel")
        {
            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            if (_titleText)    _titleText.text    = title;
            if (_bodyText)     _bodyText.text      = body;
            if (_confirmLabel) _confirmLabel.text  = confirmText;
            if (_cancelLabel)  _cancelLabel.text   = cancelText;

            _panel?.SetActive(true);
        }

        // ---- Buttons ----

        public void OnClickConfirm()
        {
            _panel?.SetActive(false);
            _onConfirm?.Invoke();
        }

        public void OnClickCancel()
        {
            _panel?.SetActive(false);
            _onCancel?.Invoke();
        }

        // ---- Static shorthand ----

        public static void Ask(string title, string body, Action onYes, Action onNo = null)
            => Instance?.Show(title, body, onYes, onNo);
    }
}
