// ============================================
// MatchHistoryUI — shows player's recent matches
// Uses /api/ranking/me/history and /api/matches/me
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Ranking
{
    [System.Serializable] class MatchHistoryResponse { public MatchHistoryEntry[] history; }
    [System.Serializable] class RecentMatchesResponse { public RecentMatch[] matches; }

    public class MatchHistoryUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;
        [SerializeField] private GameObject  _loadingOverlay;

        [Header("List")]
        [SerializeField] private Transform   _listContent;
        [SerializeField] private GameObject  _historyItemPrefab;

        [Header("Empty state")]
        [SerializeField] private TMP_Text    _emptyText;

        void Start() => _panel?.SetActive(false);

        public void Open()
        {
            _panel?.SetActive(true);
            StartCoroutine(Load());
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator Load()
        {
            _loadingOverlay?.SetActive(true);
            ClearList();

            yield return ApiClient.Instance.Get<RecentMatchesResponse>(
                Api.Matches.MyRecent,
                resp =>
                {
                    if (resp.matches == null || resp.matches.Length == 0)
                    {
                        if (_emptyText) _emptyText.gameObject.SetActive(true);
                        return;
                    }
                    if (_emptyText) _emptyText.gameObject.SetActive(false);
                    foreach (var m in resp.matches) SpawnItem(m);
                },
                err => Debug.LogWarning($"[MatchHistory] {err}"));

            _loadingOverlay?.SetActive(false);
        }

        private void SpawnItem(RecentMatch m)
        {
            if (_historyItemPrefab == null || _listContent == null) return;
            var go    = Instantiate(_historyItemPrefab, _listContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 1) texts[0].text = m.caseTitle ?? m.matchId[..8];
            if (texts.Length >= 2) texts[1].text = m.difficulty?.ToUpper() ?? "";
            if (texts.Length >= 3) texts[2].text = m.score.ToString();
            if (texts.Length >= 4)
            {
                texts[3].text  = m.rpChange >= 0 ? $"+{m.rpChange}" : m.rpChange.ToString();
                texts[3].color = m.rpChange >= 0 ? Color.green : Color.red;
            }
            if (texts.Length >= 5) texts[4].text = m.isCorrect ? "✓ Solved" : "✗ Failed";

            // Background tint for win/loss
            var img = go.GetComponent<Image>();
            if (img)
                img.color = m.isCorrect
                    ? new Color(0.1f, 0.4f, 0.1f, 0.3f)
                    : new Color(0.4f, 0.1f, 0.1f, 0.3f);
        }

        private void ClearList()
        {
            if (_listContent == null) return;
            foreach (Transform t in _listContent) Destroy(t.gameObject);
        }
    }
}
