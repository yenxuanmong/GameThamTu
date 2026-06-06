// ============================================
// CaseBrowserUI — Browse available cases from /api/cases
// Shown in Lobby for hosts to pick a specific case
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    [System.Serializable] class CaseListResponse  { public CaseListItem[] cases; public PaginationInfo pagination; }
    [System.Serializable] class CaseDetailResponse { public CaseDetails caseData; }

    public class CaseBrowserUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;
        [SerializeField] private GameObject  _loadingOverlay;

        [Header("Filter")]
        [SerializeField] private TMP_Dropdown  _difficultyDropdown;
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private Button         _searchBtn;

        [Header("List")]
        [SerializeField] private Transform   _listContent;
        [SerializeField] private GameObject  _caseItemPrefab;

        [Header("Detail Panel")]
        [SerializeField] private GameObject  _detailPanel;
        [SerializeField] private TMP_Text    _detailTitle;
        [SerializeField] private TMP_Text    _detailDifficulty;
        [SerializeField] private TMP_Text    _detailDescription;
        [SerializeField] private TMP_Text    _detailVictim;
        [SerializeField] private TMP_Text    _detailStats;
        [SerializeField] private Button      _selectCaseBtn;
        [SerializeField] private Button      _closeDetailBtn;

        [Header("Pagination")]
        [SerializeField] private Button  _prevBtn;
        [SerializeField] private Button  _nextBtn;
        [SerializeField] private TMP_Text _pageText;

        private int    _page = 1;
        private bool   _hasMore;
        private string _selectedCaseId;
        private string _selectedCaseTitle;

        private static readonly string[] DiffValues = { "", "easy", "medium", "hard", "expert", "nightmare" };

        public event System.Action<string, string> OnCaseSelected; // caseId, caseTitle

        void Start()
        {
            _panel?.SetActive(false);
            _detailPanel?.SetActive(false);
        }

        public void Open()
        {
            _panel?.SetActive(true);
            _page = 1;
            StartCoroutine(LoadCases());
        }

        public void Close()
        {
            _panel?.SetActive(false);
            _detailPanel?.SetActive(false);
        }

        // ============================================
        // Load cases
        // ============================================

        public void OnClickSearch() { _page = 1; StartCoroutine(LoadCases()); }

        private IEnumerator LoadCases()
        {
            _loadingOverlay?.SetActive(true);
            ClearList();

            int    diffIdx = _difficultyDropdown ? _difficultyDropdown.value : 0;
            string diff    = (diffIdx < DiffValues.Length) ? DiffValues[diffIdx] : "";
            string search  = _searchInput ? _searchInput.text.Trim() : "";
            string path    = $"{Api.Cases.List}?page={_page}&pageSize=20";
            if (!string.IsNullOrEmpty(diff))   path += $"&difficulty={diff}";
            if (!string.IsNullOrEmpty(search)) path += $"&search={UnityEngine.Networking.UnityWebRequest.EscapeURL(search)}";

            yield return ApiClient.Instance.Get<CaseListResponse>(path,
                resp =>
                {
                    foreach (var c in resp.cases) SpawnCaseItem(c);
                    _hasMore = resp.pagination != null && _page < resp.pagination.totalPages;
                    UpdatePagination();
                },
                err => Debug.LogWarning($"[CaseBrowser] {err}"));

            _loadingOverlay?.SetActive(false);
        }

        private void SpawnCaseItem(CaseListItem c)
        {
            if (_caseItemPrefab == null || _listContent == null) return;
            var go = Instantiate(_caseItemPrefab, _listContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 1) texts[0].text = c.title;
            if (texts.Length >= 2) texts[1].text = c.difficulty.ToUpper();
            if (texts.Length >= 3) texts[2].text = $"{c.matchCount} plays";

            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var captured = c;
                btn.onClick.AddListener(() => OpenDetail(captured.id));
            }
        }

        // ============================================
        // Detail
        // ============================================

        private void OpenDetail(string caseId)
        {
            StartCoroutine(LoadDetail(caseId));
        }

        private IEnumerator LoadDetail(string caseId)
        {
            _detailPanel?.SetActive(true);

            yield return ApiClient.Instance.Get<CaseDetailResponse>(
                Api.Cases.ById(caseId),
                resp =>
                {
                    var c = resp.caseData;
                    _selectedCaseId    = c.id;
                    _selectedCaseTitle = c.title;

                    if (_detailTitle)       _detailTitle.text       = c.title;
                    if (_detailDifficulty)  _detailDifficulty.text  = c.difficulty.ToUpper();
                    if (_detailDescription) _detailDescription.text = c.description;
                    if (_detailVictim)      _detailVictim.text      = $"Victim: {c.victimName}";
                    if (_detailStats)
                        _detailStats.text = $"{c.suspectCount} suspects  •  {c.witnessCount} witnesses  •  {c.evidenceCount} evidence";
                },
                err => Debug.LogWarning($"[CaseBrowser] Detail: {err}"));
        }

        public void OnClickSelectCase()
        {
            if (string.IsNullOrEmpty(_selectedCaseId)) return;
            OnCaseSelected?.Invoke(_selectedCaseId, _selectedCaseTitle);
            Close();
        }

        public void OnClickCloseDetail() => _detailPanel?.SetActive(false);

        // ============================================
        // Pagination
        // ============================================

        public void OnClickPrev() { if (_page > 1) { _page--; StartCoroutine(LoadCases()); } }
        public void OnClickNext() { if (_hasMore)  { _page++; StartCoroutine(LoadCases()); } }

        private void UpdatePagination()
        {
            if (_pageText) _pageText.text = $"Page {_page}";
            if (_prevBtn)  _prevBtn.interactable = _page > 1;
            if (_nextBtn)  _nextBtn.interactable = _hasMore;
        }

        private void ClearList()
        {
            if (_listContent == null) return;
            foreach (Transform t in _listContent) Destroy(t.gameObject);
        }
    }
}
