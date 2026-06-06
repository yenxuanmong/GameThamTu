// ============================================
// MatchSummaryUI — post-match detailed analytics panel
// Shows per-match summary from /api/analytics/matches/:matchId
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Analytics
{
    [System.Serializable] class MatchSummaryResponse { public MatchSummary summary; }

    public class MatchSummaryUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _loadingOverlay;

        [Header("Summary")]
        [SerializeField] private TMP_Text   _caseTitleText;
        [SerializeField] private TMP_Text   _difficultyText;
        [SerializeField] private TMP_Text   _durationText;
        [SerializeField] private TMP_Text   _solveCountText;
        [SerializeField] private TMP_Text   _avgScoreText;
        [SerializeField] private TMP_Text   _totalPlayersText;

        void Start() => _panel?.SetActive(false);

        public void Open(string matchId)
        {
            _panel?.SetActive(true);
            StartCoroutine(Load(matchId));
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator Load(string matchId)
        {
            _loadingOverlay?.SetActive(true);

            yield return ApiClient.Instance.Get<MatchSummaryResponse>(
                Api.Analytics.MatchSummary(matchId),
                resp => Populate(resp.summary),
                err  => Debug.LogWarning($"[MatchSummaryUI] {err}"));

            _loadingOverlay?.SetActive(false);
        }

        private void Populate(MatchSummary s)
        {
            if (s == null) return;
            if (_caseTitleText)    _caseTitleText.text    = s.caseTitle;
            if (_difficultyText)   _difficultyText.text   = s.difficulty.ToUpper();
            if (_durationText)     _durationText.text     = FormatDuration(s.durationSeconds);
            if (_solveCountText)   _solveCountText.text   = $"{s.solveCount}/{s.totalPlayers} solved";
            if (_avgScoreText)     _avgScoreText.text     = $"{s.avgScore:F0} avg score";
            if (_totalPlayersText) _totalPlayersText.text = $"{s.totalPlayers} players";
        }

        private static string FormatDuration(int secs)
        {
            int m = secs / 60, s = secs % 60;
            return $"{m}m {s:D2}s";
        }
    }
}
