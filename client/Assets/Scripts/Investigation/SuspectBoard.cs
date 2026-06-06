// ============================================
// SuspectBoard — visual cork board pinning suspects
// Lets player annotate and link suspects with string lines
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class SuspectBoard : MonoBehaviour
    {
        public static SuspectBoard Instance { get; private set; }

        [Header("Board")]
        [SerializeField] private GameObject  _panel;
        [SerializeField] private Transform   _boardContent;
        [SerializeField] private GameObject  _suspectCardPrefab;

        [Header("Detail")]
        [SerializeField] private GameObject  _detailPanel;
        [SerializeField] private TMP_Text    _detailName;
        [SerializeField] private TMP_Text    _detailAge;
        [SerializeField] private TMP_Text    _detailOccupation;
        [SerializeField] private TMP_Text    _detailPersonality;
        [SerializeField] private TMP_Text    _detailAlibi;
        [SerializeField] private TMP_Text    _detailBackstory;
        [SerializeField] private TMP_Text    _detailNotes;
        [SerializeField] private TMP_InputField _playerNotesInput;

        [Header("Connection lines")]
        [SerializeField] private bool        _showConnectionLines = true;

        private readonly Dictionary<string, Vector2> _cardPositions = new();
        private Suspect _selectedSuspect;

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
            PopulateBoard();
        }

        public void Close()
        {
            _panel?.SetActive(false);
            _detailPanel?.SetActive(false);
        }

        // ============================================
        // Populate
        // ============================================

        private void PopulateBoard()
        {
            ClearBoard();
            if (GameSession.Suspects == null) return;

            int count = GameSession.Suspects.Length;
            for (int i = 0; i < count; i++)
            {
                var s   = GameSession.Suspects[i];
                // Arrange in a circular layout
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                var   pos   = new Vector2(Mathf.Cos(angle) * 200f, Mathf.Sin(angle) * 140f);
                SpawnCard(s, pos);
                _cardPositions[s.id] = pos;
            }
        }

        private void SpawnCard(Suspect s, Vector2 position)
        {
            if (_suspectCardPrefab == null || _boardContent == null) return;
            var go  = Instantiate(_suspectCardPrefab, _boardContent);
            var rt  = go.GetComponent<RectTransform>();
            if (rt) rt.anchoredPosition = position;

            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 1) texts[0].text = s.name;
            if (texts.Length >= 2) texts[1].text = s.occupation;

            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var captured = s;
                btn.onClick.AddListener(() => ShowDetail(captured));
            }
        }

        // ============================================
        // Detail panel
        // ============================================

        public void ShowDetail(Suspect s)
        {
            _selectedSuspect = s;
            _detailPanel?.SetActive(true);

            if (_detailName)        _detailName.text        = s.name;
            if (_detailAge)         _detailAge.text         = $"Age: {s.age}";
            if (_detailOccupation)  _detailOccupation.text  = s.occupation;
            if (_detailPersonality) _detailPersonality.text = $"Personality: {s.personality}";
            if (_detailAlibi)       _detailAlibi.text       = $"Alibi: {s.alibi}";
            if (_detailBackstory)   _detailBackstory.text   = s.backstory;

            // Load saved player notes from local cache
            string notes = PlayerPrefs.GetString($"suspect_notes_{s.id}", "");
            if (_playerNotesInput) _playerNotesInput.text = notes;
        }

        public void CloseDetail() => _detailPanel?.SetActive(false);

        // ============================================
        // Player notes
        // ============================================

        public void OnClickSaveNotes()
        {
            if (_selectedSuspect == null) return;
            string notes = _playerNotesInput ? _playerNotesInput.text : "";
            PlayerPrefs.SetString($"suspect_notes_{_selectedSuspect.id}", notes);
            PlayerPrefs.Save();
            NotificationToast.Show("Notes saved.", "success");
        }

        // ============================================
        // Helpers
        // ============================================

        private void ClearBoard()
        {
            _cardPositions.Clear();
            if (_boardContent == null) return;
            foreach (Transform t in _boardContent) Destroy(t.gameObject);
        }
    }
}
