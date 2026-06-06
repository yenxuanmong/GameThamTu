// ============================================
// NotebookUI — full notebook panel combining notes + evidence links
// Uses /api/matches/:matchId/evidence/notebook
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    [System.Serializable] class NotebookListResponse { public NotebookEntry[] notebook; }
    [System.Serializable] class DeleteNoteResponse   { public bool success; }

    public class NotebookUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject     _panel;
        [SerializeField] private ScrollToBottom _scrollHelper;

        [Header("Note list")]
        [SerializeField] private Transform      _noteListContent;
        [SerializeField] private GameObject     _noteItemPrefab;
        [SerializeField] private TMP_Text       _emptyText;

        [Header("Add note")]
        [SerializeField] private TMP_InputField _noteInput;
        [SerializeField] private Button         _addBtn;
        [SerializeField] private TMP_Text       _charCount;
        [SerializeField] private int            _maxChars = GameConstants.MAX_NOTE_LENGTH;

        [Header("Filter")]
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private TMP_Text       _countLabel;

        [Header("Status")]
        [SerializeField] private TMP_Text       _statusText;

        private readonly List<NotebookEntry> _entries = new();
        private bool _open;

        void Start()
        {
            _panel?.SetActive(false);
        }

        void Update()
        {
            if (_noteInput && _charCount)
                _charCount.text = $"{_noteInput.text.Length}/{_maxChars}";
        }

        // ============================================
        // Toggle
        // ============================================

        public void Toggle()
        {
            _open = !_open;
            _panel?.SetActive(_open);
            if (_open) StartCoroutine(LoadNotes());
        }

        public void Open()
        {
            _open = true;
            _panel?.SetActive(true);
            StartCoroutine(LoadNotes());
        }

        public void Close()
        {
            _open = false;
            _panel?.SetActive(false);
        }

        // ============================================
        // Load notes
        // ============================================

        private IEnumerator LoadNotes()
        {
            if (_statusText) _statusText.text = "Loading...";
            ClearList();

            yield return ApiClient.Instance.Get<NotebookListResponse>(
                Api.Evidence.Notebook(GameSession.MatchId),
                resp =>
                {
                    _entries.Clear();
                    if (resp.notebook != null) _entries.AddRange(resp.notebook);
                    RenderList(_entries);
                    if (_statusText) _statusText.text = "";
                },
                err =>
                {
                    if (_statusText) _statusText.text = $"Error: {err}";
                });
        }

        // ============================================
        // Add note
        // ============================================

        public void OnClickAddNote()
        {
            string content = _noteInput ? _noteInput.text.Trim() : "";
            if (string.IsNullOrEmpty(content) || content.Length > _maxChars) return;

            // Emit via socket (server saves it)
            SocketManager.Instance.AddNote(GameSession.MatchId, content);

            // Optimistic local update
            var entry = new NotebookEntry
            {
                id        = System.Guid.NewGuid().ToString(),
                content   = content,
                createdAt = System.DateTime.UtcNow.ToString("o")
            };
            _entries.Insert(0, entry);
            SpawnNoteItem(entry);

            if (_noteInput) _noteInput.text = "";
            if (_emptyText) _emptyText.gameObject.SetActive(false);
            if (_countLabel) _countLabel.text = $"{_entries.Count} notes";
            _scrollHelper?.ScrollNow();
        }

        // ---- Submit on Enter ----
        public void OnNoteSubmit(string _) => OnClickAddNote();

        // ============================================
        // Search
        // ============================================

        public void OnSearchChanged(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                RenderList(_entries);
                return;
            }
            var filtered = new List<NotebookEntry>();
            string q = query.ToLower();
            foreach (var e in _entries)
                if (e.content?.ToLower().Contains(q) ?? false)
                    filtered.Add(e);
            RenderList(filtered);
        }

        // ============================================
        // Render
        // ============================================

        private void RenderList(List<NotebookEntry> entries)
        {
            ClearList();
            bool empty = entries == null || entries.Count == 0;
            _emptyText?.gameObject.SetActive(empty);
            if (!empty)
                foreach (var e in entries)
                    SpawnNoteItem(e);
            if (_countLabel) _countLabel.text = $"{entries?.Count ?? 0} notes";
        }

        private void SpawnNoteItem(NotebookEntry entry)
        {
            if (_noteItemPrefab == null || _noteListContent == null) return;
            var go  = Instantiate(_noteItemPrefab, _noteListContent);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = entry.content;

            // Delete button
            string entryId = entry.id;
            var delBtn = go.transform.Find("DeleteBtn")?.GetComponent<Button>();
            if (delBtn != null)
                delBtn.onClick.AddListener(() => DeleteNote(entryId, go));

            // Timestamp
            var timeText = go.transform.Find("Timestamp")?.GetComponent<TMP_Text>();
            if (timeText != null && !string.IsNullOrEmpty(entry.createdAt))
            {
                if (System.DateTime.TryParse(entry.createdAt, out var dt))
                    timeText.text = dt.ToLocalTime().ToString("HH:mm");
            }
        }

        // ============================================
        // Delete note
        // ============================================

        private void DeleteNote(string entryId, GameObject row)
        {
            Destroy(row);
            _entries.RemoveAll(e => e.id == entryId);
            if (_countLabel) _countLabel.text = $"{_entries.Count} notes";
            if (_emptyText) _emptyText.gameObject.SetActive(_entries.Count == 0);

            StartCoroutine(ApiClient.Instance.Delete<DeleteNoteResponse>(
                Api.Evidence.DeleteNote(GameSession.MatchId, entryId),
                _ => { },
                err => Debug.LogWarning($"[Notebook] Delete failed: {err}")));
        }

        private void ClearList()
        {
            if (_noteListContent == null) return;
            foreach (Transform t in _noteListContent) Destroy(t.gameObject);
        }
    }
}
