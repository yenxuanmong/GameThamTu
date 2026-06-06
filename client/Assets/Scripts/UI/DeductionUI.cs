// ============================================
// DeductionUI — wrapper that shows/hides the deduction panel
// and relays button presses to DeductionBoard
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;
using DetectiveRoyale.Investigation;

namespace DetectiveRoyale.UI
{
    public class DeductionUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject     _deductionPanel;
        [SerializeField] private DeductionBoard _deductionBoard;

        [Header("Buttons")]
        [SerializeField] private Button         _openDeductionBtn;
        [SerializeField] private Button         _closeDeductionBtn;

        [Header("Status")]
        [SerializeField] private TMP_Text       _statusText;
        [SerializeField] private TMP_Text       _submittedLabel;

        void Start()
        {
            _deductionPanel?.SetActive(false);

            // Listen for phase changes → auto-open in submission phase
            SocketManager.Instance.OnPhaseChanged.AddListener(p =>
            {
                if (p.phase == "submission" || p.phase == "final_minutes")
                    if (_statusText) _statusText.text = "⚠ Time is running out — submit your conclusion!";
            });

            SocketManager.Instance.OnPlayerSubmitted.AddListener(p =>
            {
                if (p.playerId == AuthState.Instance.Player?.id)
                {
                    if (_submittedLabel) _submittedLabel.gameObject.SetActive(true);
                    if (_openDeductionBtn) _openDeductionBtn.interactable = false;
                }
            });
        }

        public void OnClickOpenDeduction()
        {
            _deductionPanel?.SetActive(true);
        }

        public void OnClickCloseDeduction()
        {
            _deductionPanel?.SetActive(false);
        }
    }
}
