// ============================================
// DeductionBoard — conclusion submission form
// Attach to DeductionCanvas in Investigation scene.
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class DeductionBoard : MonoBehaviour
    {
        // ---- Suspect selection ----
        [Header("Suspect")]
        [SerializeField] private TMP_Dropdown _suspectDropdown;
        [SerializeField] private TMP_Text     _suspectDetails;   // shows age + occupation

        // ---- Dropdowns ----
        [Header("Motive / Weapon / Location")]
        [SerializeField] private TMP_Dropdown _motiveDropdown;
        [SerializeField] private TMP_Dropdown _weaponDropdown;
        [SerializeField] private TMP_Dropdown _locationDropdown;

        // ---- Text inputs ----
        [Header("Timeline & Narrative")]
        [SerializeField] private TMP_InputField _timelineInput;
        [SerializeField] private TMP_Text       _timelineCharCount;
        [SerializeField] private TMP_InputField _narrativeInput;
        [SerializeField] private TMP_Text       _narrativeCharCount;

        // ---- Submit ----
        [Header("Submit")]
        [SerializeField] private Button   _submitBtn;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private TMP_Text _submittedLabel;
        [SerializeField] private GameObject _submittedOverlay;

        // ---- Close ----
        [Header("Navigation")]
        [SerializeField] private Button _closeBtn;

        // ---- Options ----
        private static readonly string[] Motives = {
            "jealousy", "greed", "revenge", "love", "fear",
            "power", "blackmail", "inheritance", "rivalry",
            "self_defense", "ideology", "other"
        };
        private static readonly string[] Weapons = {
            "knife", "gun", "poison", "blunt_object", "strangulation",
            "drowning", "fire", "explosion", "fall", "other"
        };
        private static readonly string[] Locations = {
            "living_room", "kitchen", "bedroom", "bathroom", "garden",
            "garage", "basement", "attic", "office", "library",
            "dining_room", "hallway", "cellar", "rooftop", "other"
        };

        private const int MaxTimeline  = 1000;
        private const int MaxNarrative = 2000;
        private bool _submitted;

        // ============================================
        // Lifecycle
        // ============================================

        void Start()
        {
            PopulateSuspects();
            PopulateDropdown(_motiveDropdown,   Motives,   FormatLabel);
            PopulateDropdown(_weaponDropdown,   Weapons,   FormatLabel);
            PopulateDropdown(_locationDropdown, Locations, FormatLabel);

            _suspectDropdown?.onValueChanged.AddListener(_ => OnSuspectChanged());
            _submitBtn?.onClick.AddListener(OnClickSubmit);
            _closeBtn?.onClick.AddListener(OnClickClose);

            if (_timelineInput != null)
                _timelineInput.onValueChanged.AddListener(
                    _ => UpdateCharCount(_timelineInput, _timelineCharCount, MaxTimeline));
            if (_narrativeInput != null)
                _narrativeInput.onValueChanged.AddListener(
                    _ => UpdateCharCount(_narrativeInput, _narrativeCharCount, MaxNarrative));

            _submittedOverlay?.SetActive(false);
            if (_submittedLabel) _submittedLabel.gameObject.SetActive(false);
            ClearError();
        }

        // ============================================
        // Populate dropdowns
        // ============================================

        private void PopulateSuspects()
        {
            if (_suspectDropdown == null) return;
            _suspectDropdown.ClearOptions();

            var opts = new List<string> { "— Select Suspect —" };
            if (GameSession.Suspects != null)
                foreach (var s in GameSession.Suspects)
                    opts.Add(s.name);

            _suspectDropdown.AddOptions(opts);
        }

        private void OnSuspectChanged()
        {
            int idx = _suspectDropdown.value - 1;
            if (_suspectDetails == null) return;

            if (idx < 0 || GameSession.Suspects == null || idx >= GameSession.Suspects.Length)
            {
                _suspectDetails.text = "";
                return;
            }

            var s = GameSession.Suspects[idx];
            _suspectDetails.text = $"Age {s.age}  ·  {s.occupation}\n\"{s.alibi}\"";
        }

        private static void PopulateDropdown(TMP_Dropdown dd, string[] values,
            System.Func<string, string> fmt)
        {
            if (dd == null) return;
            dd.ClearOptions();
            var opts = new List<string>();
            foreach (var v in values) opts.Add(fmt(v));
            dd.AddOptions(opts);
        }

        // ============================================
        // Character counters
        // ============================================

        private static void UpdateCharCount(TMP_InputField field, TMP_Text counter, int max)
        {
            if (counter == null) return;
            int len = field != null ? field.text.Length : 0;
            counter.text  = $"{len}/{max}";
            counter.color = len > max ? Color.red : Color.white;
        }

        // ============================================
        // Submit
        // ============================================

        public void OnClickSubmit()
        {
            if (_submitted) return;
            ClearError();

            // Validate suspect
            int suspIdx = _suspectDropdown != null ? _suspectDropdown.value - 1 : -1;
            if (suspIdx < 0 || GameSession.Suspects == null || suspIdx >= GameSession.Suspects.Length)
            { SetError("Please select a suspect."); return; }

            // Validate timeline
            string timeline = _timelineInput != null ? _timelineInput.text.Trim() : "";
            if (string.IsNullOrEmpty(timeline))
            { SetError("Please describe the timeline of events."); return; }
            if (timeline.Length > MaxTimeline)
            { SetError($"Timeline too long (max {MaxTimeline} chars)."); return; }

            string narrative = _narrativeInput != null ? _narrativeInput.text.Trim() : "";
            if (narrative.Length > MaxNarrative)
            { SetError($"Narrative too long (max {MaxNarrative} chars)."); return; }

            var conclusion = new DetectiveRoyale.Core.Models.ConclusionPayload
            {
                killerId  = GameSession.Suspects[suspIdx].id,
                motive    = Motives[_motiveDropdown != null ? _motiveDropdown.value : 0],
                weapon    = Weapons[_weaponDropdown != null ? _weaponDropdown.value : 0],
                location  = Locations[_locationDropdown != null ? _locationDropdown.value : 0],
                timeline  = timeline,
                narrative = narrative,
            };

            // Disable submit to prevent double-sending
            if (_submitBtn) _submitBtn.interactable = false;

            SocketManager.Instance.SubmitConclusion(GameSession.MatchId, conclusion);
            InvestigationManager.Instance?.OnConclusionSubmitted();

            _submitted = true;
            if (_submittedOverlay) _submittedOverlay.SetActive(true);
            if (_submittedLabel)   _submittedLabel.gameObject.SetActive(true);
        }

        public void OnClickClose()
        {
            gameObject.SetActive(false);
        }

        // ============================================
        // Helpers
        // ============================================

        private void SetError(string msg)
        {
            if (_errorText) { _errorText.text = msg; _errorText.gameObject.SetActive(true); }
        }

        private void ClearError()
        {
            if (_errorText) { _errorText.text = ""; _errorText.gameObject.SetActive(false); }
        }

        private static string FormatLabel(string key) =>
            System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(key.Replace("_", " ").ToLower());
    }
}
