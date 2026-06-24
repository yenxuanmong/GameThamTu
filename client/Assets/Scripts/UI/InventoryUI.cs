// ============================================
// InventoryUI — tabbed panel combining evidence, notebook, suspects
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _panel;

        [Header("Tabs")]
        [SerializeField] private Button _tabEvidence;
        [SerializeField] private Button _tabSuspects;
        [SerializeField] private Button _tabNotebook;
        [SerializeField] private Button _tabTimeline;

        [Header("Content panels")]
        [SerializeField] private GameObject _evidenceContent;
        [SerializeField] private GameObject _suspectsContent;
        [SerializeField] private GameObject _notebookContent;
        [SerializeField] private GameObject _timelineContent;

        [Header("Suspects list")]
        [SerializeField] private Transform  _suspectListContent;
        [SerializeField] private GameObject _suspectItemPrefab;

        [Header("Notebook")]
        [SerializeField] private Transform  _noteListContent;
        [SerializeField] private GameObject _noteItemPrefab;
        [SerializeField] private TMP_InputField _newNoteInput;
        [SerializeField] private Button     _addNoteBtn;

        [Header("Timeline")]
        [SerializeField] private Transform  _timelineContent2;
        [SerializeField] private GameObject _timelineItemPrefab;

        void Start() => _panel?.SetActive(false);

        public void Toggle()
        {
            bool on = !(_panel?.activeSelf ?? false);
            _panel?.SetActive(on);
            if (on) ShowTab("evidence");
        }

        public void ShowTab(string tab)
        {
            _evidenceContent?.SetActive(tab == "evidence");
            _suspectsContent?.SetActive(tab == "suspects");
            _notebookContent?.SetActive(tab == "notebook");
            _timelineContent?.SetActive(tab == "timeline");

            if (tab == "suspects")  PopulateSuspects();
            if (tab == "notebook")  StartCoroutine(LoadNotebook());
            if (tab == "timeline")  PopulateTimeline();
        }

        public void OnClickTabEvidence() => ShowTab("evidence");
        public void OnClickTabSuspects() => ShowTab("suspects");
        public void OnClickTabNotebook() => ShowTab("notebook");
        public void OnClickTabTimeline() => ShowTab("timeline");

        // ---- Suspects ----

        private void PopulateSuspects()
        {
            ClearList(_suspectListContent);
            if (GameSession.Suspects == null) return;
            foreach (var s in GameSession.Suspects)
            {
                var go = Instantiate(_suspectItemPrefab, _suspectListContent);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2) { texts[0].text = s.name; texts[1].text = s.occupation; }
            }
        }

        // ---- Notebook ----

        private IEnumerator LoadNotebook()
        {
            ClearList(_noteListContent);
            yield return ApiClient.Instance.Get<NotebookResponse>(
                $"/matches/{GameSession.MatchId}/evidence/notebook",
                resp =>
                {
                    foreach (var note in resp.notebook)
                        SpawnNote(note);
                },
                err => Debug.LogWarning($"[Inventory] Notebook: {err}"));
        }

        private void SpawnNote(NotebookEntry note)
        {
            if (_noteItemPrefab == null) return;
            var go = Instantiate(_noteItemPrefab, _noteListContent);
            var t  = go.GetComponentInChildren<TMP_Text>();
            if (t) t.text = note.content;
        }

        public void OnClickAddNote()
        {
            string content = _newNoteInput ? _newNoteInput.text.Trim() : "";
            if (string.IsNullOrEmpty(content)) return;
            SocketManager.Instance.AddNote(GameSession.MatchId, content);
            if (_newNoteInput) _newNoteInput.text = "";
            StartCoroutine(LoadNotebook());
        }

        // ---- Timeline ----

        private void PopulateTimeline()
        {
            ClearList(_timelineContent2);
            if (GameSession.Timeline == null) return;
            foreach (var ev in GameSession.Timeline)
            {
                if (_timelineItemPrefab == null) continue;
                var go = Instantiate(_timelineItemPrefab, _timelineContent2);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2)
                {
                    texts[0].text = ev.timestamp;
                    texts[1].text = ev.description;
                }
            }
        }

        private void ClearList(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform t in parent) Destroy(t.gameObject);
        }

        // ---- Response types ----
        [System.Serializable] class NotebookResponse { public NotebookEntry[] notebook; }
        [System.Serializable] class NotebookEntry    { public string id; public string content; public string createdAt; }
    }
}
