// ============================================
// DeductionBoard — the conclusion submission form
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class DeductionBoard : MonoBehaviour
    {
        [Header("Suspect Dropdown")]
        [SerializeField] private TMP_Dropdown _suspectDropdown;

        [Header("Motive Dropdown")]
        [SerializeField] private TMP_Dropdown _motiveDropdown;

        [Header("Weapon Dropdown")]
        [SerializeField] private TMP_Dropdown _weaponDropdown;

        [Header("Location Dropdown")]
        [SerializeField] private TMP_Dropdown _locationDropdown;

        [Header("Timeline & Narrative")]
        [SerializeField] private TMP_InputField _timelineInput;
        [SerializeField] private TMP_InputField _narrativeInput;

        [Header("Submit")]
        [SerializeField] private Button   _submitBtn;
        [SerializeField] private TMP_Text _errorText;

        private static readonly string[] Motives   = { "jealousy","greed","revenge","love","fear","power","blackmail","inheritance","rivalry","self_defense","ideology","other" };
        private static readonly string[] Weapons   = { "knife","gun","poison","blunt_object","strangulation","drowning","fire","explosion","fall","other" };
        private static readonly string[] Locations = { "living_room","kitchen","bedroom","bathroom","garden","garage","basement","attic","office","library","dining_room","hallway","cellar","rooftop","other" };

        void Start()
        {
            PopulateSuspects();
            PopulateDropdown(_motiveDropdown,   Motives,   label => label.Replace("_", " "));
            PopulateDropdown(_weaponDropdown,   Weapons,   label => label.Replace("_", " "));
            PopulateDropdown(_locationDropdown, Locations, label => label.Replace("_", " "));
        }

        private void PopulateSuspects()
        {
            if (_suspectDropdown == null) return;
            _suspectDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string> { "-- Select Suspect --" };
            if (GameSession.Suspects != null)
                foreach (var s in GameSession.Suspects)
                    opts.Add(s.name);
            _suspectDropdown.AddOptions(opts);
        }

        private void PopulateDropdown(TMP_Dropdown dd, string[] values, System.Func<string, string> labelFmt)
        {
            if (dd == null) return;
            dd.ClearOptions();
            var opts = new System.Collections.Generic.List<string>();
            foreach (var v in values)
                opts.Add(labelFmt(v));
            dd.AddOptions(opts);
        }

        public void OnClickSubmit()
        {
            if (_errorText) _errorText.text = "";

            int suspIdx = _suspectDropdown ? _suspectDropdown.value - 1 : -1;
            if (suspIdx < 0 || GameSession.Suspects == null || suspIdx >= GameSession.Suspects.Length)
            { SetError("Please select a suspect."); return; }

            string timeline  = _timelineInput  ? _timelineInput.text.Trim()  : "";
            string narrative = _narrativeInput ? _narrativeInput.text.Trim() : "";

            if (string.IsNullOrEmpty(timeline))
            { SetError("Please describe the timeline."); return; }

            var conclusion = new SocketManager.Models.ConclusionPayload
            {
                killerId  = GameSession.Suspects[suspIdx].id,
                motive    = Motives[_motiveDropdown.value],
                weapon    = Weapons[_weaponDropdown.value],
                location  = Locations[_locationDropdown.value],
                timeline  = timeline,
                narrative = narrative,
            };

            if (_submitBtn) _submitBtn.interactable = false;

            SocketManager.Instance.SubmitConclusion(GameSession.MatchId, conclusion);
            InvestigationManager.Instance?.OnConclusionSubmitted();
        }

        private void SetError(string msg)
        {
            if (_errorText) _errorText.text = msg;
        }
    }
}
