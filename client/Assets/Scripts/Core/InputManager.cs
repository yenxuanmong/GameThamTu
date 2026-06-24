// ============================================
// InputManager — global keyboard shortcuts for investigation
// ============================================
using UnityEngine;

namespace DetectiveRoyale.Core
{
    /// <summary>
    /// Attach to a persistent GameObject.
    /// Handles global keyboard shortcuts during the investigation scene.
    /// Uses lazy component lookup to avoid circular assembly references.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        // Lazy references — found at runtime to avoid circular asmdef dependencies
        private MonoBehaviour _inventoryUI;
        private MonoBehaviour _deductionUI;

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
                SendToggle(_inventoryUI, "Toggle");

            // D — open deduction board
            if (Input.GetKeyDown(KeyCode.D))
                SendToggle(_deductionUI, "OnClickOpenDeduction");

            // H — request hint (send to InvestigationManager via message)
            if (Input.GetKeyDown(KeyCode.H))
                SendHint();

            // Escape — close any open panel
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseTopPanel();
        }

        // Called by InvestigationManager on scene load to wire up refs
        public void SetInvestigationRefs(MonoBehaviour inventory, MonoBehaviour deduction)
        {
            _inventoryUI  = inventory;
            _deductionUI  = deduction;
        }

        private void SendToggle(MonoBehaviour target, string methodName)
        {
            if (target != null)
                target.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        }

        private void SendHint()
        {
            // Broadcast to any active InvestigationManager in scene
            var go = GameObject.Find("InvestigationManager");
            if (go != null)
                go.SendMessage("OnClickHint", SendMessageOptions.DontRequireReceiver);
        }

        private void CloseTopPanel()
        {
            if (_deductionUI != null)
            {
                _deductionUI.SendMessage("OnClickCloseDeduction", SendMessageOptions.DontRequireReceiver);
                return;
            }
            if (_inventoryUI != null)
            {
                _inventoryUI.SendMessage("Toggle", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
