// ============================================
// DebugConsole — in-game debug overlay (Development builds only)
// Toggle with grave accent ` key
// ============================================
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class DebugConsole : MonoBehaviour
    {
        [SerializeField] private GameObject      _panel;
        [SerializeField] private TMP_Text        _logText;
        [SerializeField] private TMP_InputField  _commandInput;
        [SerializeField] private int             _maxLines = 80;
        [SerializeField] private KeyCode         _toggleKey = KeyCode.BackQuote;

        private readonly List<string> _lines   = new();
        private bool                 _visible;

        void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // Disable in release builds
            gameObject.SetActive(false);
            return;
#endif
            Application.logMessageReceived += OnLog;
            _panel?.SetActive(false);
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) Toggle();

            if (_visible && Input.GetKeyDown(KeyCode.Return))
                ExecuteCommand();
        }

        private void Toggle()
        {
            _visible = !_visible;
            _panel?.SetActive(_visible);
            if (_visible) _commandInput?.Select();
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            string color = type switch
            {
                LogType.Error     => "#ff4444",
                LogType.Exception => "#ff2222",
                LogType.Warning   => "#ffaa00",
                _                 => "#cccccc"
            };
            AddLine($"<color={color}>[{type}] {condition}</color>");
        }

        private void AddLine(string line)
        {
            _lines.Add(line);
            if (_lines.Count > _maxLines) _lines.RemoveAt(0);
            if (_logText) _logText.text = string.Join("\n", _lines);
        }

        private void ExecuteCommand()
        {
            string cmd = _commandInput?.text.Trim() ?? "";
            if (string.IsNullOrEmpty(cmd)) return;
            if (_commandInput) _commandInput.text = "";

            AddLine($"<color=#88ff88>> {cmd}</color>");
            ProcessCommand(cmd);
        }

        private void ProcessCommand(string cmd)
        {
            string[] parts = cmd.Split(' ');
            switch (parts[0].ToLower())
            {
                case "scene":
                    if (parts.Length > 1)
                        Core.SceneLoader.Instance?.LoadScene(parts[1]);
                    break;
                case "logout":
                    Core.AuthState.Instance?.Logout();
                    break;
                case "hint":
                    Core.SocketManager.Instance?.RequestHint(Core.GameSession.MatchId);
                    break;
                case "fps":
                    AddLine($"FPS: {1f / Time.unscaledDeltaTime:F1}");
                    break;
                case "session":
                    AddLine($"MatchId: {Core.GameSession.MatchId}");
                    AddLine($"CaseId:  {Core.GameSession.CaseId}");
                    AddLine($"Phase:   {Core.GameSession.CurrentPhase}");
                    AddLine($"Timer:   {Core.GameSession.TimeRemaining}s");
                    break;
                case "clear":
                    _lines.Clear();
                    if (_logText) _logText.text = "";
                    break;
                case "help":
                    AddLine("Commands: scene [name] | logout | hint | fps | session | clear | help");
                    break;
                default:
                    AddLine($"<color=#ff8888>Unknown: {parts[0]}</color>");
                    break;
            }
        }
    }
}
