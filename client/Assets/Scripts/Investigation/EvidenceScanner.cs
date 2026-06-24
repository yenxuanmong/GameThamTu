// ============================================
// EvidenceScanner — interactive evidence detail panel
// Shows full evidence info after it's been collected
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class EvidenceScanner : MonoBehaviour
    {
        public static EvidenceScanner Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Info")]
        [SerializeField] private TMP_Text   _nameText;
        [SerializeField] private TMP_Text   _typeText;
        [SerializeField] private TMP_Text   _descText;
        [SerializeField] private TMP_Text   _locationText;
        [SerializeField] private RawImage   _previewImage;

        [Header("Notes")]
        [SerializeField] private TMP_InputField _notesInput;
        [SerializeField] private Button         _saveNotesBtn;
        [SerializeField] private TMP_Text       _saveStatus;

        [Header("Actions")]
        [SerializeField] private Button         _closeBtn;

        private Evidence _current;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _panel?.SetActive(false);
        }

        // ---- Open a specific evidence ----

        public void Open(Evidence evidence)
        {
            _current = evidence;
            _panel?.SetActive(true);

            if (_nameText)     _nameText.text     = evidence.name;
            if (_typeText)     _typeText.text      = evidence.type.Replace("_", " ").ToUpper();
            if (_descText)     _descText.text      = evidence.description;
            if (_locationText) _locationText.text  = $"Found: {evidence.location.Replace("_", " ")}";
            if (_notesInput)   _notesInput.text    = evidence.notes ?? "";
            if (_saveStatus)   _saveStatus.text    = "";

            // Load image if URL present
            if (!string.IsNullOrEmpty(evidence.imageUrl))
                StartCoroutine(LoadImage(evidence.imageUrl));
        }

        public void Close()
        {
            _panel?.SetActive(false);
            _current = null;
        }

        // ---- Notes ----

        public void OnClickSaveNotes()
        {
            if (_current == null) return;
            string notes = _notesInput ? _notesInput.text : "";
            if (_saveNotesBtn) _saveNotesBtn.interactable = false;

            StartCoroutine(EvidenceSystem.Instance.UpdateNotes(_current.id, notes, () =>
            {
                _current.notes = notes;
                if (_saveStatus)    _saveStatus.text = "Saved ✓";
                if (_saveNotesBtn)  _saveNotesBtn.interactable = true;
            }));
        }

        // ---- Image loader ----

        private IEnumerator LoadImage(string url)
        {
            using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success && _previewImage)
                _previewImage.texture = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
        }
    }
}
