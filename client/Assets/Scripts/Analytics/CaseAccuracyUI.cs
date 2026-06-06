// ============================================
// CaseAccuracyUI — shows accuracy statistics for a specific case
// Uses /api/analytics/cases/:caseId
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Analytics
{
    [System.Serializable] class CaseAccuracyResponse { public CaseAccuracyData stats; }

    [System.Serializable]
    public class CaseAccuracyData
    {
        public string caseId;
        public int    totalMatches;
        public float  solveRate;
        public float  avgScore;
        public float  killerAccuracy;
        public float  motiveAccuracy;
        public float  weaponAccuracy;
        public float  locationAccuracy;
    }

    public class CaseAccuracyUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _loadingOverlay;

        [Header("Header")]
        [SerializeField] private TMP_Text  _caseTitleText;
        [SerializeField] private TMP_Text  _totalMatchesText;
        [SerializeField] private TMP_Text  _solveRateText;
        [SerializeField] private TMP_Text  _avgScoreText;

        [Header("Accuracy Bars")]
        [SerializeField] private StatsBarUI _killerBar;
        [SerializeField] private StatsBarUI _motiveBar;
        [SerializeField] private StatsBarUI _weaponBar;
        [SerializeField] private StatsBarUI _locationBar;

        void Start() => _panel?.SetActive(false);

        public void Open(string caseId, string caseTitle = null)
        {
            _panel?.SetActive(true);
            if (_caseTitleText && caseTitle != null) _caseTitleText.text = caseTitle;
            StartCoroutine(Load(caseId));
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator Load(string caseId)
        {
            _loadingOverlay?.SetActive(true);

            yield return ApiClient.Instance.Get<CaseAccuracyResponse>(
                Api.Analytics.CaseStats(caseId),
                resp => Populate(resp.stats),
                err  => Debug.LogWarning($"[CaseAccuracy] {err}"));

            _loadingOverlay?.SetActive(false);
        }

        private void Populate(CaseAccuracyData d)
        {
            if (d == null) return;
            if (_totalMatchesText) _totalMatchesText.text = $"{d.totalMatches} matches";
            if (_solveRateText)    _solveRateText.text    = $"{d.solveRate * 100:F0}% solve rate";
            if (_avgScoreText)     _avgScoreText.text     = $"{d.avgScore:F0} avg score";

            _killerBar?.Setup("Killer",   d.killerAccuracy);
            _motiveBar?.Setup("Motive",   d.motiveAccuracy);
            _weaponBar?.Setup("Weapon",   d.weaponAccuracy);
            _locationBar?.Setup("Location", d.locationAccuracy);
        }
    }
}
