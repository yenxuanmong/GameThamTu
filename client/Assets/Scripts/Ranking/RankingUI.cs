// ============================================
// RankingUI — leaderboard panel
// Can be shown from MainMenu or Lobby
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Ranking
{
    [System.Serializable] class LeaderboardResponse { public LeaderboardEntry[] leaderboard; public SeasonInfo season; }
    [System.Serializable] class MyRankResponse      { public PlayerRankInfo rank; }

    [System.Serializable]
    public class PlayerRankInfo
    {
        public string playerId;
        public string username;
        public string tier;
        public int    points;
        public int    peakPoints;
        public int    wins;
        public int    losses;
        public float  winRate;
        public int    streak;
        public int    globalRank;
        public int    season;
    }

    public class RankingUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;

        [Header("Season info")]
        [SerializeField] private TMP_Text     _seasonName;
        [SerializeField] private TMP_Text     _seasonEnd;
        [SerializeField] private TMP_Text     _playerCount;

        [Header("Leaderboard")]
        [SerializeField] private Transform    _listContent;
        [SerializeField] private GameObject   _entryPrefab;
        [SerializeField] private TMP_Dropdown _pageDropdown;
        [SerializeField] private Button       _prevPageBtn;
        [SerializeField] private Button       _nextPageBtn;
        [SerializeField] private TMP_Text     _pageText;

        [Header("My rank")]
        [SerializeField] private TMP_Text     _myRankText;
        [SerializeField] private TMP_Text     _myTierText;
        [SerializeField] private TMP_Text     _myPointsText;
        [SerializeField] private TMP_Text     _myWinRateText;
        [SerializeField] private TMP_Text     _myStreakText;

        [Header("Loading")]
        [SerializeField] private GameObject   _loadingOverlay;

        private int _currentPage = 1;
        private const int PAGE_SIZE = 50;
        private bool _hasMore = true;

        void Start() => _panel?.SetActive(false);

        public void Open()
        {
            _panel?.SetActive(true);
            _currentPage = 1;
            StartCoroutine(LoadLeaderboard());
            StartCoroutine(LoadMyRank());
        }

        public void Close() => _panel?.SetActive(false);

        // ============================================
        // Load leaderboard
        // ============================================

        private IEnumerator LoadLeaderboard()
        {
            _loadingOverlay?.SetActive(true);
            ClearList();

            yield return ApiClient.Instance.Get<LeaderboardResponse>(
                $"/ranking/leaderboard?page={_currentPage}&pageSize={PAGE_SIZE}",
                resp =>
                {
                    foreach (var entry in resp.leaderboard)
                        SpawnEntry(entry);
                    _hasMore = resp.leaderboard.Length == PAGE_SIZE;
                    UpdatePagination();
                    DisplaySeason(resp.season);
                },
                err => Debug.LogWarning($"[Ranking] Load error: {err}"));

            _loadingOverlay?.SetActive(false);
        }

        private void SpawnEntry(LeaderboardEntry e)
        {
            if (_entryPrefab == null || _listContent == null) return;
            var go    = Instantiate(_entryPrefab, _listContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 5)
            {
                texts[0].text = e.rank.ToString();
                texts[1].text = e.username;
                texts[2].text = e.tier;
                texts[3].text = e.points.ToString();
                texts[4].text = e.wins.ToString();
            }
            // Highlight self
            bool isSelf = e.playerId == AuthState.Instance.Player?.id;
            var bg = go.GetComponent<Image>();
            if (bg && isSelf) bg.color = new Color(1f, 0.9f, 0.2f, 0.3f);
        }

        private void DisplaySeason(SeasonInfo s)
        {
            if (s == null) return;
            if (_seasonName)   _seasonName.text   = s.name;
            if (_seasonEnd)    _seasonEnd.text     = $"Ends in {s.daysRemaining} days";
            if (_playerCount)  _playerCount.text   = $"{s.playerCount} players";
        }

        // ============================================
        // My rank
        // ============================================

        private IEnumerator LoadMyRank()
        {
            yield return ApiClient.Instance.Get<MyRankResponse>("/ranking/me",
                resp =>
                {
                    var r = resp.rank;
                    if (_myRankText)    _myRankText.text    = $"#{r.globalRank}";
                    if (_myTierText)    _myTierText.text    = r.tier;
                    if (_myPointsText)  _myPointsText.text  = $"{r.points} RP";
                    if (_myWinRateText) _myWinRateText.text = $"{r.winRate * 100:F0}% WR";
                    if (_myStreakText)
                    {
                        string streak = r.streak >= 0 ? $"🔥{r.streak}W" : $"❄{-r.streak}L";
                        _myStreakText.text = streak;
                    }
                },
                err => Debug.LogWarning($"[Ranking] My rank error: {err}"));
        }

        // ============================================
        // Pagination
        // ============================================

        public void OnClickPrevPage()
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            StartCoroutine(LoadLeaderboard());
        }

        public void OnClickNextPage()
        {
            if (!_hasMore) return;
            _currentPage++;
            StartCoroutine(LoadLeaderboard());
        }

        private void UpdatePagination()
        {
            if (_pageText)    _pageText.text               = $"Page {_currentPage}";
            if (_prevPageBtn) _prevPageBtn.interactable    = _currentPage > 1;
            if (_nextPageBtn) _nextPageBtn.interactable    = _hasMore;
        }

        private void ClearList()
        {
            foreach (Transform t in _listContent) Destroy(t.gameObject);
        }
    }
}
