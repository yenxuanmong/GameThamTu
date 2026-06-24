// ============================================
// DifficultySelector — reusable difficulty picker widget
// Used in CreateRoom, Matchmaking, and Profile preferences
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class DifficultySelector : MonoBehaviour
    {
        public static readonly string[] Difficulties =
            { "easy", "medium", "hard", "expert", "nightmare" };

        public static readonly string[] Labels =
            { "Easy", "Medium", "Hard", "Expert", "Nightmare" };

        public static readonly Color[] Colors =
        {
            new Color(0.3f, 0.9f, 0.3f),   // easy    — green
            new Color(1.0f, 0.8f, 0.2f),   // medium  — yellow
            new Color(1.0f, 0.5f, 0.1f),   // hard    — orange
            new Color(0.9f, 0.2f, 0.2f),   // expert  — red
            new Color(0.6f, 0.1f, 0.9f),   // nightmare — purple
        };

        [Header("UI")]
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private Image        _colorIndicator;
        [SerializeField] private TMP_Text     _descriptionText;

        private static readonly string[] Descriptions =
        {
            "Perfect for new detectives. Clear clues, straightforward case.",
            "Balanced challenge. Some misdirection, moderate evidence.",
            "Complex case with multiple red herrings. Experience required.",
            "Deep investigation needed. Witness lies, hidden motives.",
            "Maximum difficulty. Only the best detectives solve this.",
        };

        public int SelectedIndex => _dropdown ? _dropdown.value : 1;
        public string SelectedValue => Difficulties[SelectedIndex];

        void Start()
        {
            if (_dropdown == null) return;
            _dropdown.ClearOptions();
            _dropdown.AddOptions(new System.Collections.Generic.List<string>(Labels));
            _dropdown.value = 1; // default medium
            _dropdown.onValueChanged.AddListener(OnChanged);
            OnChanged(1);
        }

        public void SetValue(string difficulty)
        {
            for (int i = 0; i < Difficulties.Length; i++)
            {
                if (Difficulties[i] == difficulty)
                {
                    if (_dropdown) _dropdown.SetValueWithoutNotify(i);
                    OnChanged(i);
                    return;
                }
            }
        }

        private void OnChanged(int idx)
        {
            if (idx < 0 || idx >= Colors.Length) return;
            if (_colorIndicator)   _colorIndicator.color = Colors[idx];
            if (_descriptionText)  _descriptionText.text = Descriptions[idx];
        }
    }
}
