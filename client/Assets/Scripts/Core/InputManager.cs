// ============================================
// InputManager — global keyboard shortcuts for investigation
// ============================================
using UnityEngine;

namespace DetectiveRoyale.Core
{
    /// <summary>
    /// Attach to a persistent GameObject.
    /// Handles global keyboard shortcuts during the investigation scene.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Refs (Investigation scene only)")]
        [SerializeField] private DetectiveRoyale.UI.InventoryUI  _inventoryUI;
        [SerializeField] private DetectiveRoyale.UI.DeductionUI  _deductionUI;
        [SerializeField] private DetectiveRoyale.UI.HUD          _hud;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            HandleInvestigationShortcuts();
        }

        private void HandleInvestigationShortcuts()
        {
            // Only active during investigation scene
            if (GameSession.MatchId == null) return;

            // I — toggle inventory
            if (Input.GetKeyDown(KeyCode.I))
                _inventoryUI?.Toggle();

            // D — open deduction board
            if (Input.GetKeyDown(KeyCode.D))
                _deductionUI?.OnClickOpenDeduction();

            // H — request hint
            if (Input.GetKeyDown(KeyCode.H))
                DetectiveRoyale.Investigation.InvestigationManager.Instance?.OnClickHint();

            // Escape — close any open panel (handled per-panel via close buttons)
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseTopPanel();
        }

        private void CloseTopPanel()
        {
            // Priority: deduction > inventory > scanner
            if (_deductionUI != null)
            {
                _deductionUI.OnClickCloseDeduction();
                return;
            }
            if (_inventoryUI != null)
            {
                _inventoryUI.Toggle(); // close if open
            }
        }
    }
}
