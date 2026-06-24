// ============================================
// EvidenceNotepad — quick inline notes attached to evidence items
// Syncs with server via /api/matches/:matchId/evidence/:id/notes
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class EvidenceNotepad : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject     _panel;

        [Header("Evidence header")]
        [SerializeField] private TMP_Text       _evidenceName;
        [SerializeField] private TMP_Text       _evidenceType;

        [Header("Note input")]
        [SerializeField] private TMP_InputField _noteInput;
        [SerializeField] private TMP_Text       _charCount;
        [SerializeField] private int            _maxChars = 500;

        [Header("Tags / Links")]
        [SerializeField] private Toggle         _linkToKillerToggle;
        [SerializeField] private Toggle         _linkToWeaponToggle;
        [SerializeField] private Toggle         _linkToLocationToggle;

        [Header("Actions")]
        [SerializeField] private Button         _saveBtn;
        [SerializeField] private Button         _closeBtn;
        [SerializeField] private TMP_Text       _statusText;

        private Evidence _currentEvidence;
        private bool     _dirty;

        void Start() => _panel?.SetActive(false);

        void Update()
        {
            if (_noteInput && _charCount)
                _charCount.text = $"{_noteInput.text.Length}/{_maxChars}";
        }

        // ============================================
        // Open
        // ============================================

        public void Open(Evidence evidence)
        {
            _currentEvidence = evidence;
            _panel?.SetActive(true);
            _dirty = false;

            if (_evidenceName) _evidenceName.text = evidence.name;
            if (_evidenceType) _evidenceType.text = evidence.type?.ToUpper();
            if (_noteInput)
            {
                _noteInput.text = evidence.notes ?? "";
                _noteInput.onValueChanged.AddListener(_ => _dirty = true);
            }
            if (_statusText) _statusText.text = "";
        }

        public void Close()
        {
            if (_dirty) AutoSave();
            _panel?.SetActive(false);
            _currentEvidence = null;
        }

        // ============================================
        // Save
        // ============================================

        public void OnClickSave() => StartCoroutine(SaveCoroutine());

        private void AutoSave()
        {
            if (_currentEvidence != null && _dirty)
                StartCoroutine(SaveCoroutine(silent: true));
        }

        private IEnumerator SaveCoroutine(bool silent = false)
        {
            if (_currentEvidence == null) yield break;
            if (_saveBtn) _saveBtn.interactable = false;

            string notes = _noteInput ? _noteInput.text : "";
            _currentEvidence.notes = notes;

            yield return ApiClient.Instance.Patch<object>(
                Api.Evidence.Notes(GameSession.MatchId, _currentEvidence.id),
                new { notes },
                _ =>
                {
                    _dirty = false;
                    if (!silent && _statusText) _statusText.text = "✓ Saved";
                    if (_saveBtn) _saveBtn.interactable = true;
                },
                err =>
                {
                    if (!silent) UI.NotificationToast.Show($"Save failed: {err}", "error");
                    if (_saveBtn) _saveBtn.interactable = true;
                });
        }

        // ============================================
        // Quick note from HUD (shortcut)
        // ============================================

        public void AppendNote(string text)
        {
            if (_noteInput == null) return;
            string current = _noteInput.text;
            string separator = string.IsNullOrEmpty(current) ? "" : "\n";
            _noteInput.text = current + separator + text;
            _dirty = true;
        }
    }
}
