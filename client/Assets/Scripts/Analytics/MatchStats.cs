// ============================================
// MatchStats — fetches and displays analytics
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Analytics
{
    [System.Serializable] class ReportResponse   { public PlayerReportData  report;    }
    [System.Serializable] class AccuracyResponse { public AccuracyData      breakdown; }
    [System.Serializable] class TrendResponse    { public TrendPoint[]      trend;     }

    [System.Serializable]
    public class PlayerReportData
    {
        public string playerId;
        public string username;
        public int    totalMatches;
        public int    totalWins;
        public float  winRate;
        public int    avgScore;
        public int    highScore;
        public int    perfectSolves;
        public string recentTrend;
    }

    [System.Serializable]
    public class AccuracyData
    {
        public int   totalSubmissions;
        public float killerAccuracy;
        public float motiveAccuracy;
        public float weaponAccuracy;
        public float locationAccuracy;
        public float fullCorrectRate;
        public int   avgScore;
    }

    [System.Serializable]
    public class TrendPoint
    {
        public string matchId;
        public int    score;
        public bool   isCorrect;
        public string endedAt;
    }

    public class MatchStats : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;
        [SerializeField] private GameObject   _loadingOverlay;

        [Header("Overview")]
        [SerializeField] private TMP_Text     _totalMatchesText;
        [SerializeField] private TMP_Text     _winRateText;
        [SerializeField] private TMP_Text     _avgScoreText;
        [SerializeField] private TMP_Text     _highScoreText;
        [SerializeField] private TMP_Text     _perfectSolvesText;
        [SerializeField] private TMP_Text     _trendText;

        [Header("Accuracy bars")]
        [SerializeField] private Slider       _killerBar;
        [SerializeField] private Slider       _motiveBar;
        [SerializeField] private Slider       _weaponBar;
        [SerializeField] private Slider       _locationBar;
        [SerializeField] private TMP_Text     _killerPct;
        [SerializeField] private TMP_Text     _motivePct;
        [SerializeField] private TMP_Text     _weaponPct;
        [SerializeField] private TMP_Text     _locationPct;

        [Header("Trend graph (simple dots)")]
        [SerializeField] private Transform    _trendContent;
        [SerializeField] private GameObject   _trendDotPrefab;

        void Start() => _panel?.SetActive(false);

        public void Open()
        {
            _panel?.SetActive(true);
            StartCoroutine(LoadStats());
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator LoadStats()
        {
            _loadingOverlay?.SetActive(true);

            bool d1 = false, d2 = false, d3 = false;

            StartCoroutine(ApiClient.Instance.Get<ReportResponse>("/analytics/me",
                resp =>
                {
                    DisplayOverview(resp.report);
                    d1 = true;
                },
                _ => d1 = true));

            StartCoroutine(ApiClient.Instance.Get<AccuracyResponse>("/analytics/me/accuracy",
                resp =>
                {
                    DisplayAccuracy(resp.breakdown);
                    d2 = true;
                },
                _ => d2 = true));

            StartCoroutine(ApiClient.Instance.Get<TrendResponse>("/analytics/me/trend?limit=20",
                resp =>
                {
                    DisplayTrend(resp.trend);
                    d3 = true;
                },
                _ => d3 = true));

            yield return new WaitUntil(() => d1 && d2 && d3);
            _loadingOverlay?.SetActive(false);
        }

        // ---- Display helpers ----

        private void DisplayOverview(PlayerReportData r)
        {
            if (_totalMatchesText) _totalMatchesText.text = r.totalMatches.ToString();
            if (_winRateText)      _winRateText.text       = $"{r.winRate * 100:F0}%";
            if (_avgScoreText)     _avgScoreText.text      = r.avgScore.ToString();
            if (_highScoreText)    _highScoreText.text     = r.highScore.ToString();
            if (_perfectSolvesText)_perfectSolvesText.text = r.perfectSolves.ToString();
            if (_trendText)
            {
                _trendText.text  = r.recentTrend;
                _trendText.color = r.recentTrend == "improving" ? Color.green
                                 : r.recentTrend == "declining" ? Color.red
                                 : Color.white;
            }
        }

        private void DisplayAccuracy(AccuracyData a)
        {
            SetBar(_killerBar,   _killerPct,   a.killerAccuracy);
            SetBar(_motiveBar,   _motivePct,   a.motiveAccuracy);
            SetBar(_weaponBar,   _weaponPct,   a.weaponAccuracy);
            SetBar(_locationBar, _locationPct, a.locationAccuracy);
        }

        private static void SetBar(Slider bar, TMP_Text label, float val)
        {
            if (bar)   bar.value    = val;
            if (label) label.text   = $"{val * 100:F0}%";
        }

        private void DisplayTrend(TrendPoint[] points)
        {
            if (_trendContent == null || _trendDotPrefab == null || points == null) return;
            foreach (Transform t in _trendContent) Destroy(t.gameObject);

            // Normalise scores 0-1000 → 0-1
            foreach (var pt in points)
            {
                var go  = Instantiate(_trendDotPrefab, _trendContent);
                var img = go.GetComponent<Image>();
                if (img) img.color = pt.isCorrect ? Color.green : Color.red;
                // Scale dot vertically by score (Layout element height)
                var le  = go.GetComponent<LayoutElement>();
                if (le) le.preferredHeight = Mathf.Lerp(4f, 80f, pt.score / 1000f);
            }
        }
    }
}
