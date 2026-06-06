// ============================================
// GlobalStatsUI — displays global game statistics
// Uses /api/analytics/global
// ============================================
using System.Collections;
using UnityEngine;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Analytics
{
    [System.Serializable] class GlobalStatsResponse { public GlobalStats stats; }

    public class GlobalStatsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Stats")]
        [SerializeField] private TMP_Text _totalMatchesText;
        [SerializeField] private TMP_Text _activePlayersText;
        [SerializeField] private TMP_Text _solveRateText;
        [SerializeField] private TMP_Text _avgDurationText;

        void Start()
        {
            _panel?.SetActive(false);
        }

        public void Open()
        {
            _panel?.SetActive(true);
            StartCoroutine(Load());
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator Load()
        {
            yield return ApiClient.Instance.Get<GlobalStatsResponse>(
                Api.Analytics.Global,
                resp => Populate(resp.stats),
                err  => Debug.LogWarning($"[GlobalStats] {err}"));
        }

        private void Populate(GlobalStats s)
        {
            if (s == null) return;
            if (_totalMatchesText)  _totalMatchesText.text  = $"{s.totalMatches:N0}";
            if (_activePlayersText) _activePlayersText.text = $"{s.activePlayers:N0}";
            if (_solveRateText)     _solveRateText.text     = $"{s.avgSolveRate * 100:F0}%";
            if (_avgDurationText)
            {
                int m = (int)s.avgMatchDuration / 60;
                _avgDurationText.text = $"{m}m avg";
            }
        }
    }
}
