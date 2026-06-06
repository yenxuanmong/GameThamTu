// ============================================
// TutorialManager — step-by-step tutorial overlay
// Shows new players how to play on first Investigation run
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject  _panel;
        [SerializeField] private Image       _highlightMask;   // dims background
        [SerializeField] private RectTransform _tooltipBox;

        [Header("Tooltip")]
        [SerializeField] private TMP_Text    _titleText;
        [SerializeField] private TMP_Text    _bodyText;
        [SerializeField] private Button      _nextBtn;
        [SerializeField] private Button      _skipBtn;
        [SerializeField] private TMP_Text    _stepCounter;

        private List<TutorialStep> _steps = new();
        private int _currentStep;
        private bool _active;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _panel?.SetActive(false);
        }

        // ============================================
        // Built-in steps
        // ============================================

        private void BuildInvestigationSteps()
        {
            _steps = new List<TutorialStep>
            {
                new TutorialStep("Welcome, Detective",
                    "You are investigating a murder case. Collect evidence, interrogate witnesses, and submit your conclusion before time runs out."),
                new TutorialStep("Timer",
                    "Watch the timer in the top-right. You have limited time to solve the case. Running low? Use a Hint (H key)."),
                new TutorialStep("Collecting Evidence",
                    "Click on glowing objects in the scene to collect evidence. Each piece brings you closer to the truth."),
                new TutorialStep("Inventory (I)",
                    "Press I to open your inventory. Switch between Evidence, Suspects, Notebook, and Timeline tabs."),
                new TutorialStep("Interrogate Witnesses",
                    "Click on witnesses to open a dialogue. Ask them questions — but watch their stress level. High stress = they may be hiding something."),
                new TutorialStep("Notebook",
                    "Use the Notebook tab to jot down your deductions. Press I → Notebook, or use the Notebook button."),
                new TutorialStep("Deduction Board (D)",
                    "When you're ready, press D to open the Deduction Board. Select the killer, motive, weapon and location, then submit!"),
                new TutorialStep("Multiplayer",
                    "Other detectives are investigating the same case! Watch the player bar — green means they've submitted. First correct answer wins more points."),
                new TutorialStep("Good Luck!",
                    "You're ready. The truth is out there — find it before the others do!"),
            };
        }

        // ============================================
        // Start tutorial
        // ============================================

        public void StartInvestigationTutorial()
        {
            if (SaveManager.HasCompletedTutorial) return;
            BuildInvestigationSteps();
            _currentStep = 0;
            _active      = true;
            _panel?.SetActive(true);
            ShowStep(_currentStep);
        }

        public void ForceStart()
        {
            BuildInvestigationSteps();
            _currentStep = 0;
            _active      = true;
            _panel?.SetActive(true);
            ShowStep(_currentStep);
        }

        // ============================================
        // Navigation
        // ============================================

        private void ShowStep(int index)
        {
            if (index >= _steps.Count) { FinishTutorial(); return; }

            var step = _steps[index];
            if (_titleText)   _titleText.text   = step.Title;
            if (_bodyText)    _bodyText.text     = step.Body;
            if (_stepCounter) _stepCounter.text = $"{index + 1} / {_steps.Count}";

            // Position tooltip centre by default; future: near a target RectTransform
            if (_tooltipBox)
                _tooltipBox.anchoredPosition = Vector2.zero;
        }

        public void OnClickNext()
        {
            _currentStep++;
            if (_currentStep >= _steps.Count)
                FinishTutorial();
            else
                ShowStep(_currentStep);
        }

        public void OnClickSkip() => FinishTutorial();

        private void FinishTutorial()
        {
            _active = false;
            _panel?.SetActive(false);
            SaveManager.HasCompletedTutorial = true;
        }

        // ============================================
        // Data
        // ============================================

        private class TutorialStep
        {
            public string Title;
            public string Body;
            public TutorialStep(string title, string body) { Title = title; Body = body; }
        }
    }
}
