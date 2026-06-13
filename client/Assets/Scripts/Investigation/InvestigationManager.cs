// ============================================
// InvestigationManager — boots the investigation scene,
// loads case data, wires socket events, drives the match timer
// ============================================
using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DetectiveRoyale.Core;
using DetectiveRoyale.NPC;

namespace DetectiveRoyale.Investigation
{
    [Serializable] class CaseResponse  { public CaseDetails caseData; }
    [Serializable] class SuspectsResp  { public Suspect[]   suspects; }
    [Serializable] class WitnessesResp { public Witness[]   witnesses; }
    [Serializable] class TimelineResp  { public TimelineEvent[] timeline; }
    [Serializable] class MatchResp     { public Match match; }

    public class InvestigationManager : MonoBehaviour
    {
        public static InvestigationManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _investigationRoot;
        [SerializeField] private GameObject _deductionPanel;
        [SerializeField] private GameObject _submittedOverlay;

        [Header("HUD")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _phaseText;
        [SerializeField] private TMP_Text _hintsText;
        [SerializeField] private TMP_Text _caseTitle;
        [SerializeField] private TMP_Text _victimName;

        [Header("Sub-systems")]
        [SerializeField] private EvidenceSystem _evidenceSystem;
        [SerializeField] private NPCManager     _npcManager;
        [SerializeField] private DeductionBoard _deductionBoard;

        private bool      _hasSubmitted;
        private Coroutine _localTimerCoroutine;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        IEnumerator Start()
        {
            _loadingPanel?.SetActive(true);
            _investigationRoot?.SetActive(false);

            yield return LoadCaseData();

            SetupSocketListeners();
            UpdateHUD();

            _loadingPanel?.SetActive(false);
            _investigationRoot?.SetActive(true);

            _localTimerCoroutine = StartCoroutine(LocalTimerTick());
        }

        void OnDestroy()
        {
            RemoveSocketListeners();
            if (_localTimerCoroutine != null)
                StopCoroutine(_localTimerCoroutine);
        }

        // ============================================
        // Load case data
        // ============================================

        private IEnumerator LoadCaseData()
        {
            string caseId = GameSession.CaseId;
            if (string.IsNullOrEmpty(caseId))
            {
                Debug.LogError("[InvManager] No CaseId in GameSession");
                yield break;
            }

            yield return ApiClient.Instance.Get<CaseResponse>($"/cases/{caseId}",
                r => GameSession.Case = r.caseData,
                e => Debug.LogError($"[InvManager] Case: {e}"));

            yield return ApiClient.Instance.Get<SuspectsResp>($"/cases/{caseId}/suspects",
                r => GameSession.Suspects = r.suspects,
                e => Debug.LogWarning($"[InvManager] Suspects: {e}"));

            yield return ApiClient.Instance.Get<WitnessesResp>($"/cases/{caseId}/witnesses",
                r => GameSession.Witnesses = r.witnesses,
                e => Debug.LogWarning($"[InvManager] Witnesses: {e}"));

            yield return ApiClient.Instance.Get<TimelineResp>($"/cases/{caseId}/timeline",
                r => GameSession.Timeline = r.timeline,
                e => Debug.LogWarning($"[InvManager] Timeline: {e}"));

            yield return ApiClient.Instance.Get<MatchResp>($"/matches/{GameSession.MatchId}",
                r =>
                {
                    GameSession.Match         = r.match;
                    GameSession.TimeRemaining = r.match.timeRemainingSeconds;
                    GameSession.CurrentPhase  = r.match.phase;
                },
                e => Debug.LogError($"[InvManager] Match: {e}"));
        }

        // ============================================
        // HUD
        // ============================================

        private void UpdateHUD()
        {
            if (_caseTitle  && GameSession.Case != null) _caseTitle.text  = GameSession.Case.title;
            if (_victimName && GameSession.Case != null) _victimName.text = $"Victim: {GameSession.Case.victimName}";
            if (_phaseText)                              _phaseText.text  = GameSession.CurrentPhase.Replace("_", " ").ToUpper();
            if (_hintsText)                              _hintsText.text  = $"Hints: {GameSession.HintsRemaining}";
            UpdateTimerDisplay();
        }

        private IEnumerator LocalTimerTick()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (GameSession.TimeRemaining > 0)
                {
                    GameSession.TimeRemaining--;
                    UpdateTimerDisplay();
                }
            }
        }

        private void UpdateTimerDisplay()
        {
            if (_timerText == null) return;
            int t = GameSession.TimeRemaining;
            _timerText.text  = $"{t / 60:D2}:{t % 60:D2}";
            _timerText.color = t <= 300 ? Color.red : Color.white;
        }

        // ============================================
        // Socket listeners
        // ============================================

        private void SetupSocketListeners()
        {
            var sm = SocketManager.Instance;
            sm.OnTimerTick.AddListener(OnTimer);
            sm.OnPhaseChanged.AddListener(OnPhaseChanged);
            sm.OnEvidenceFound.AddListener(OnEvidenceFound);
            sm.OnMatchEnded.AddListener(OnMatchEnded);
            sm.OnHintReceived.AddListener(OnHintReceived);
        }

        private void RemoveSocketListeners()
        {
            if (SocketManager.Instance == null) return;
            var sm = SocketManager.Instance;
            sm.OnTimerTick.RemoveListener(OnTimer);
            sm.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            sm.OnEvidenceFound.RemoveListener(OnEvidenceFound);
            sm.OnMatchEnded.RemoveListener(OnMatchEnded);
            sm.OnHintReceived.RemoveListener(OnHintReceived);
        }

        private void OnTimer(TimerPayload p)
        {
            GameSession.TimeRemaining = p.timeRemaining;
            UpdateTimerDisplay();
        }

        private void OnPhaseChanged(PhasePayload p)
        {
            GameSession.CurrentPhase  = p.phase;
            GameSession.TimeRemaining = p.timeRemaining;
            if (_phaseText) _phaseText.text = p.phase.Replace("_", " ").ToUpper();
            if (p.phase == "submission") ShowDeductionPanel();
        }

        private void OnEvidenceFound(EvidenceFoundPayload p)
        {
            if (p.playerId == AuthState.Instance.Player?.id)
                _evidenceSystem?.OnEvidenceDiscovered(p.evidenceId);
        }

        private void OnMatchEnded(MatchEndedPayload p)
        {
            GameSession.Match = GameSession.Match ?? new Match();
            SceneLoader.Instance.LoadScene(SceneLoader.SCENE_RESULTS);
        }

        private void OnHintReceived(HintPayload p)
        {
            GameSession.HintsRemaining = p.hintsRemaining;
            if (_hintsText) _hintsText.text = $"Hints: {p.hintsRemaining}";
            var toast = GameObject.Find("NotificationToast");
            if (toast != null)
                toast.SendMessage("ShowToast", p.hint, SendMessageOptions.DontRequireReceiver);
        }

        // ============================================
        // Public actions
        // ============================================

        public void ShowDeductionPanel() => _deductionPanel?.SetActive(true);

        public void OnClickHint()
        {
            if (GameSession.HintsRemaining <= 0)
            {
                var toast = GameObject.Find("NotificationToast");
                if (toast != null)
                    toast.SendMessage("ShowToast", "No hints remaining.", SendMessageOptions.DontRequireReceiver);
                return;
            }
            SocketManager.Instance.RequestHint(GameSession.MatchId);
        }

        public void OnConclusionSubmitted()
        {
            _hasSubmitted = true;
            if (_submittedOverlay) _submittedOverlay.SetActive(true);
            _deductionPanel?.SetActive(false);
        }
    }
}
