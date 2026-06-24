// ============================================
// SeasonUI — Season info + history popup
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Ranking
{
    [System.Serializable] class SeasonResponse  { public SeasonInfoFull season; }
    [System.Serializable] class SeasonsResponse { public SeasonInfoFull[] seasons; }

    [System.Serializable]
    public class SeasonInfoFull
    {
        public string id;
        public int    number;
        public string name;
        public string startDate;
        public string endDate;
        public bool   isActive;
        public int    daysRemaining;
        public int    playerCount;
    }

    public class SeasonUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;

        [Header("Current season")]
        [SerializeField] private TMP_Text     _currentName;
        [SerializeField] private TMP_Text     _currentPeriod;
        [SerializeField] private TMP_Text     _daysRemaining;
        [SerializeField] private TMP_Text     _totalPlayers;

        [Header("Season history")]
        [SerializeField] private Transform    _historyContent;
        [SerializeField] private GameObject   _historyItemPrefab;

        void Start() => _panel?.SetActive(false);

        public void Open()
        {
            _panel?.SetActive(true);
            StartCoroutine(LoadCurrentSeason());
            StartCoroutine(LoadAllSeasons());
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator LoadCurrentSeason()
        {
            yield return ApiClient.Instance.Get<SeasonResponse>("/ranking/seasons/current",
                resp =>
                {
                    var s = resp.season;
                    if (_currentName)   _currentName.text   = s.name;
                    if (_currentPeriod) _currentPeriod.text = $"{s.startDate[..10]} → {s.endDate[..10]}";
                    if (_daysRemaining) _daysRemaining.text = $"{s.daysRemaining} days left";
                    if (_totalPlayers)  _totalPlayers.text  = $"{s.playerCount} players";
                },
                err => Debug.LogWarning($"[SeasonUI] Current season: {err}"));
        }

        private IEnumerator LoadAllSeasons()
        {
            yield return ApiClient.Instance.Get<SeasonsResponse>("/ranking/seasons",
                resp =>
                {
                    foreach (Transform t in _historyContent) Destroy(t.gameObject);
                    foreach (var s in resp.seasons)
                    {
                        if (_historyItemPrefab == null) break;
                        var go = Instantiate(_historyItemPrefab, _historyContent);
                        var texts = go.GetComponentsInChildren<TMP_Text>();
                        if (texts.Length >= 2)
                        {
                            texts[0].text = $"Season {s.number}: {s.name}";
                            texts[1].text = s.isActive ? "ACTIVE" : s.endDate[..10];
                        }
                    }
                },
                err => Debug.LogWarning($"[SeasonUI] All seasons: {err}"));
        }
    }
}
