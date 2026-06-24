// ============================================
// NotebookManager — in-scene notebook panel
// Shows and manages notes during investigation
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    [Serializable] class NotebookResp { public NoteItem[] notebook; }
    [Serializable] class NoteItem     { public string id; public string content; public string createdAt; }

    public class NotebookManager : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject     _panel;

        [Header("Note list")]
        [SerializeField] private Transform      _noteListContent;
        [SerializeField] private GameObject     _noteItemPrefab;

        [Header("Add note")]
        [SerializeField] private TMP_InputField _noteInput;
        [SerializeField] private Button         _addBtn;
        [SerializeField] private TMP_Text       _charCount;
        [SerializeField] private int            _maxChars = 300;

        [Header("Status")]
        [SerializeField] private TMP_Text       _statusText;

        private readonly List<NoteItem> _notes = new();
        private bool _open;

        void Start() => _panel?.SetActive(false);

        void Update()
        {
            if (_noteInput && _charCount)
                _charCount.text = $"{_noteInput.text.Length}/{_maxChars}";
        }

        public void Toggle()
        {
            _open = !_open;
            _panel?.SetActive(_open);
            if (_open) StartCoroutine(LoadNotes());
        }

        // ============================================
        // Load notebook from server
        // ============================================

        private IEnumerator LoadNotes()
        {
            if (_statusText) _statusText.text = "Loading...";
            ClearList();

            yield return ApiClient.Instance.Get<NotebookResp>(
                $"/matches/{GameSession.MatchId}/evidence/notebook",
                resp =>
                {
                    _notes.Clear();
                    if (resp.notebook != null) _notes.AddRange(resp.notebook);
                    foreach (var n in _notes) SpawnNote(n);
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

            SocketManager.Instance.AddNote(GameSession.MatchId, content);

            // Optimistic update
            var local = new NoteItem { id = System.Guid.NewGuid().ToString(), content = content, createdAt = System.DateTime.UtcNow.ToString("o") };
            _notes.Add(local);
            SpawnNote(local);

            if (_noteInput) _noteInput.text = "";
        }

        // ============================================
        // Spawn note row
        // ============================================

        private void SpawnNote(NoteItem note)
        {
            if (_noteItemPrefab == null || _noteListContent == null) return;
            var go  = Instantiate(_noteItemPrefab, _noteListContent);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = note.content;

            // Delete button
            var delBtn = go.transform.Find("DeleteBtn")?.GetComponent<Button>();
            if (delBtn != null)
            {
                string nid = note.id;
                delBtn.onClick.AddListener(() =>
                {
                    Destroy(go);
                    _notes.RemoveAll(n => n.id == nid);
                });
            }
        }

        private void ClearList()
        {
            foreach (Transform t in _noteListContent) Destroy(t.gameObject);
        }
    }
}
