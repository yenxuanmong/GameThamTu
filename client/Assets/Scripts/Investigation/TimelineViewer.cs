// ============================================
// TimelineViewer — visual timeline of case events
// Displays chronological events on a horizontal scroll
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class TimelineViewer : MonoBehaviour
    {
        public static TimelineViewer Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject   _panel;
        [SerializeField] private ScrollRect   _scrollRect;

        [Header("Timeline")]
        [SerializeField] private Transform    _timelineContent;   // horizontal layout
        [SerializeField] private GameObject   _eventNodePrefab;
        [SerializeField] private GameObject   _keyEventNodePrefab; // highlighted

        [Header("Selected event detail")]
        [SerializeField] private GameObject   _detailPanel;
        [SerializeField] private TMP_Text     _detailTimestamp;
        [SerializeField] private TMP_Text     _detailDescription;
        [SerializeField] private TMP_Text     _detailLocation;
        [SerializeField] private Image        _keyEventBadge;

        [Header("Close")]
        [SerializeField] private Button       _closeBtn;

        private TimelineEvent _selectedEvent;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _panel?.SetActive(false);
            _detailPanel?.SetActive(false);
        }

        // ============================================
        // Open / Close
        // ============================================

        public void Open()
        {
            _panel?.SetActive(true);
            PopulateTimeline();
        }

        public void Close()
        {
            _panel?.SetActive(false);
            _detailPanel?.SetActive(false);
        }

        // ============================================
        // Populate
        // ============================================

        private void PopulateTimeline()
        {
            ClearContent();
            if (GameSession.Timeline == null) return;

            foreach (var ev in GameSession.Timeline)
                SpawnNode(ev);
        }

        private void SpawnNode(TimelineEvent ev)
        {
            GameObject prefab = ev.isKeyEvent ? _keyEventNodePrefab : _eventNodePrefab;
            if (prefab == null || _timelineContent == null) return;

            var go    = Instantiate(prefab, _timelineContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 2)
            {
                texts[0].text = FormatTimestamp(ev.timestamp);
                texts[1].text = ev.description.Length > 30
                    ? ev.description[..27] + "..."
                    : ev.description;
            }

            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var captured = ev;
                btn.onClick.AddListener(() => ShowDetail(captured));
            }
        }

        // ============================================
        // Detail panel
        // ============================================

        public void ShowDetail(TimelineEvent ev)
        {
            _selectedEvent = ev;
            _detailPanel?.SetActive(true);

            if (_detailTimestamp)   _detailTimestamp.text   = FormatTimestamp(ev.timestamp);
            if (_detailDescription) _detailDescription.text = ev.description;
            if (_detailLocation)    _detailLocation.text    = ev.location;
            if (_keyEventBadge)     _keyEventBadge.gameObject.SetActive(ev.isKeyEvent);
        }

        public void CloseDetail() => _detailPanel?.SetActive(false);

        // ============================================
        // Helpers
        // ============================================

        private static string FormatTimestamp(string ts)
        {
            if (string.IsNullOrEmpty(ts)) return "Unknown";
            // Try to parse ISO and return short form
            if (System.DateTime.TryParse(ts, out var dt))
                return dt.ToString("MM/dd HH:mm");
            return ts;
        }

        private void ClearContent()
        {
            if (_timelineContent == null) return;
            foreach (Transform t in _timelineContent)
                Destroy(t.gameObject);
        }
    }
}
