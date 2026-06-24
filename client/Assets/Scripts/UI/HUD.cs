// ============================================
// HUD — in-game Heads-Up Display for Investigation scene
// Central place for timer, phase, hints, notifications
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Timer")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Image    _timerFill;     // radial or horizontal fill

        [Header("Phase")]
        [SerializeField] private TMP_Text _phaseText;
        [SerializeField] private Image    _phaseIndicator;

        [Header("Hints")]
        [SerializeField] private TMP_Text _hintsText;
        [SerializeField] private Button   _hintBtn;

        [Header("Case info")]
        [SerializeField] private TMP_Text _caseTitleText;
        [SerializeField] private TMP_Text _victimText;

        [Header("Players submitted")]
        [SerializeField] private TMP_Text _submittedText;
        private int _submitted;

        [Header("Phase colours")]
        [SerializeField] private Color _investigationColor = Color.white;
        [SerializeField] private Color _finalMinutesColor  = new Color(1f, 0.7f, 0f);
        [SerializeField] private Color _submissionColor    = Color.green;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            SocketManager.Instance.OnTimerTick.AddListener(OnTimer);
            SocketManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
            SocketManager.Instance.OnPlayerSubmitted.AddListener(OnPlayerSubmitted);
            SocketManager.Instance.OnHintReceived.AddListener(OnHintReceived);
            PopulateCaseInfo();
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnTimerTick.RemoveListener(OnTimer);
            SocketManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            SocketManager.Instance.OnPlayerSubmitted.RemoveListener(OnPlayerSubmitted);
            SocketManager.Instance.OnHintReceived.RemoveListener(OnHintReceived);
        }

        // ============================================

        private void PopulateCaseInfo()
        {
            if (GameSession.Case == null) return;
            if (_caseTitleText) _caseTitleText.text = GameSession.Case.title;
            if (_victimText)    _victimText.text    = $"Victim: {GameSession.Case.victimName}";
        }

        // ============================================
        // Socket handlers
        // ============================================

        private void OnTimer(TimerPayload p)
        {
            int t = p.timeRemaining;
            if (_timerText)
            {
                _timerText.text  = $"{t / 60:D2}:{t % 60:D2}";
                _timerText.color = t <= 60 ? Color.red : t <= 300 ? _finalMinutesColor : Color.white;
            }
            if (_timerFill && GameSession.Match != null)
                _timerFill.fillAmount = (float)t / GameSession.Match.durationSeconds;
        }

        private void OnPhaseChanged(PhasePayload p)
        {
            UpdatePhaseDisplay(p.phase);
        }

        private void UpdatePhaseDisplay(string phase)
        {
            if (_phaseText) _phaseText.text = phase.Replace("_", " ").ToUpper();
            Color col = phase switch
            {
                "final_minutes" => _finalMinutesColor,
                "submission"    => _submissionColor,
                "reveal"        => _submissionColor,
                _               => _investigationColor
            };
            if (_phaseText)      _phaseText.color      = col;
            if (_phaseIndicator) _phaseIndicator.color = col;
        }

        private void OnPlayerSubmitted(SubmitPayload p)
        {
            _submitted++;
            int total = GameSession.Match?.playerIds?.Length ?? 0;
            if (_submittedText) _submittedText.text = $"{_submitted}/{total} submitted";
        }

        private void OnHintReceived(HintPayload p)
        {
            if (_hintsText) _hintsText.text = $"Hints: {p.hintsRemaining}/{GameConfig.Instance.MaxHints}";
            if (_hintBtn)   _hintBtn.interactable = p.hintsRemaining > 0;
        }

        // ============================================
        // Public setters called by InvestigationManager
        // ============================================

        public void SetHints(int remaining)
        {
            if (_hintsText) _hintsText.text = $"Hints: {remaining}/{GameConfig.Instance.MaxHints}";
            if (_hintBtn)   _hintBtn.interactable = remaining > 0;
        }
    }
}
