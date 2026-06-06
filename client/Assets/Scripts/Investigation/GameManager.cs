// ============================================
// GameManager — investigation scene orchestrator
// Coordinates all sub-systems, handles win/lose conditions
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("HUD Buttons")]
        [SerializeField] private Button _hintBtn;
        [SerializeField] private Button _openInventoryBtn;
        [SerializeField] private Button _openDeductionBtn;
        [SerializeField] private Button _leaveMatchBtn;

        [Header("Phase Banners")]
        [SerializeField] private GameObject _finalMinutesBanner;
        [SerializeField] private TMP_Text   _finalMinutesText;
        [SerializeField] private GameObject _submittedBanner;

        [Header("Other Players Bar")]
        [SerializeField] private Transform  _otherPlayersBar;
        [SerializeField] private GameObject _otherPlayerIconPrefab;

        [Header("Sub-system refs")]
        [SerializeField] private UI.InventoryUI   _inventoryUI;
        [SerializeField] private UI.DeductionUI   _deductionUI;
        [SerializeField] private EvidenceSystem   _evidenceSystem;

        private bool _matchActive;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            SetupSocketListeners();
            BuildOtherPlayerBar();
            _matchActive = true;
        }

        void OnDestroy() => RemoveSocketListeners();

        // ============================================
        // HUD button handlers
        // ============================================

        public void OnClickHint()
        {
            if (!_matchActive) return;
            InvestigationManager.Instance?.OnClickHint();
        }

        public void OnClickInventory() => _inventoryUI?.Toggle();

        public void OnClickDeduction()
        {
            if (!_matchActive) return;
            _deductionUI?.OnClickOpenDeduction();
        }

        public void OnClickLeaveMatch()
        {
            // Warn player
            StartCoroutine(ConfirmLeave());
        }

        private IEnumerator ConfirmLeave()
        {
            // Simple approach: leave after 2s delay to allow reading warning
            UI.NotificationToast.Show("Leaving the match will count as a loss. You have 5 seconds to cancel.", "warning", 5f);
            yield return new WaitForSeconds(5f);

            if (!string.IsNullOrEmpty(GameSession.RoomId))
                SocketManager.Instance.LeaveRoom(GameSession.RoomId);

            GameSession.Reset();
            Core.SceneLoader.Instance.LoadScene(Core.SceneLoader.SCENE_LOBBY);
        }

        // ============================================
        // Other player bar
        // ============================================

        private void BuildOtherPlayerBar()
        {
            if (_otherPlayersBar == null || _otherPlayerIconPrefab == null) return;
            if (GameSession.Match == null) return;

            string myId = AuthState.Instance.Player?.id;
            foreach (var pid in GameSession.Match.playerIds)
            {
                if (pid == myId) continue;
                var go    = Instantiate(_otherPlayerIconPrefab, _otherPlayersBar);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 0) texts[0].text = pid[..Mathf.Min(6, pid.Length)];
                go.name = pid; // tag for update
            }
        }

        // ============================================
        // Socket listeners
        // ============================================

        private void SetupSocketListeners()
        {
            SocketManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
            SocketManager.Instance.OnPlayerSubmitted.AddListener(OnOtherPlayerSubmitted);
            SocketManager.Instance.OnMatchEnded.AddListener(OnMatchEnded);
        }

        private void RemoveSocketListeners()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            SocketManager.Instance.OnPlayerSubmitted.RemoveListener(OnOtherPlayerSubmitted);
            SocketManager.Instance.OnMatchEnded.RemoveListener(OnMatchEnded);
        }

        private void OnPhaseChanged(PhasePayload p)
        {
            if (p.phase == "final_minutes" && _finalMinutesBanner != null)
            {
                _finalMinutesBanner.SetActive(true);
                if (_finalMinutesText) _finalMinutesText.text = "⚠ Final 5 minutes!";
                StartCoroutine(HideBanner(_finalMinutesBanner, 4f));
            }
        }

        private void OnOtherPlayerSubmitted(SubmitPayload p)
        {
            // Grey out their icon on the bar
            if (_otherPlayersBar == null) return;
            Transform icon = _otherPlayersBar.Find(p.playerId);
            if (icon != null)
            {
                var img = icon.GetComponent<Image>();
                if (img) img.color = new Color(0.4f, 0.9f, 0.4f, 0.8f);
                var txt = icon.GetComponentInChildren<TMP_Text>();
                if (txt) txt.text += " ✓";
            }
        }

        private void OnMatchEnded(MatchEndedPayload p)
        {
            _matchActive = false;
            if (_submittedBanner) _submittedBanner.SetActive(true);
        }

        private IEnumerator HideBanner(GameObject banner, float delay)
        {
            yield return new WaitForSeconds(delay);
            banner?.SetActive(false);
        }
    }
}
