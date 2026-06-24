// ============================================
// DeductionUI — shows/hides the deduction panel
// Attach to HUDCanvas in Investigation scene.
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class DeductionUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _deductionPanel;

        [Header("Buttons")]
        [SerializeField] private Button  _openBtn;
        [SerializeField] private Button  _closeBtn;

        [Header("Status")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _submittedLabel;
        [SerializeField] private Image    _openBtnHighlight;   // flashes red in submission phase

        private bool _hasSubmitted;

        // ============================================

        void Start()
        {
            _openBtn?.onClick.AddListener(OnClickOpenDeduction);
            _closeBtn?.onClick.AddListener(OnClickCloseDeduction);
            _deductionPanel?.SetActive(false);

            if (_submittedLabel)
                _submittedLabel.gameObject.SetActive(false);

            // Socket listeners
            SocketManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
            SocketManager.Instance.OnPlayerSubmitted.AddListener(OnPlayerSubmitted);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            SocketManager.Instance.OnPlayerSubmitted.RemoveListener(OnPlayerSubmitted);
        }

        // ============================================
        // Open / Close
        // ============================================

        public void OnClickOpenDeduction()
        {
            if (_hasSubmitted) return;
            _deductionPanel?.SetActive(true);
        }

        public void OnClickCloseDeduction()
        {
            _deductionPanel?.SetActive(false);
        }

        // ---- Called by InputManager shortcut (D key) ----
        public void Toggle()
        {
            if (_hasSubmitted) return;
            bool current = _deductionPanel != null && _deductionPanel.activeSelf;
            _deductionPanel?.SetActive(!current);
        }

        // ============================================
        // Socket handlers
        // ============================================

        private void OnPhaseChanged(PhasePayload p)
        {
            switch (p.phase)
            {
                case "final_minutes":
                    SetStatus("\u26a0\ufe0f Final minutes — submit your conclusion!");
                    FlashButton(true);
                    break;

                case "submission":
                    SetStatus("\u23f0 Time is up — submit now!");
                    FlashButton(true);
                    // Auto-open panel when submission phase begins
                    if (!_hasSubmitted)
                        _deductionPanel?.SetActive(true);
                    break;

                default:
                    SetStatus("");
                    FlashButton(false);
                    break;
            }
        }

        private void OnPlayerSubmitted(SubmitPayload p)
        {
            if (p.playerId != AuthState.Instance.Player?.id) return;

            _hasSubmitted = true;
            _deductionPanel?.SetActive(false);
            if (_submittedLabel) _submittedLabel.gameObject.SetActive(true);
            if (_openBtn)        _openBtn.interactable = false;
            FlashButton(false);
        }

        // ============================================
        // Helpers
        // ============================================

        private void SetStatus(string msg)
        {
            if (_statusText) _statusText.text = msg;
        }

        private void FlashButton(bool on)
        {
            if (_openBtnHighlight) _openBtnHighlight.color =
                on ? new Color(1f, 0.3f, 0.2f, 0.85f) : new Color(1f, 1f, 1f, 0f);
        }
    }
}
