// ============================================
// ClientErrorHandler — global uncaught exception handler
// Logs to file and shows user-friendly error messages
// ============================================
using System;
using System.IO;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class ClientErrorHandler : MonoBehaviour
    {
        public static ClientErrorHandler Instance { get; private set; }

        [SerializeField] private bool  _logToFile    = true;
        [SerializeField] private bool  _showToast    = true;

        private string _logFilePath;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _logFilePath = Path.Combine(
                Application.persistentDataPath, "DetectiveRoyale", "error.log");

            Application.logMessageReceived += OnLogMessage;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;

            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}]\n{condition}\n{stackTrace}\n---\n";

            if (_logToFile)
                WriteToFile(entry);

            if (_showToast && type == LogType.Exception)
            {
                UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                {
                    UI.NotificationToast.Show(
                        "An unexpected error occurred. Please restart if issues persist.",
                        "error", GameConstants.TOAST_ERROR_DURATION);
                });
            }
        }

        private void WriteToFile(string entry)
        {
            try
            {
                string dir = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                // Keep log under 1MB by rotating
                if (File.Exists(_logFilePath))
                {
                    var info = new FileInfo(_logFilePath);
                    if (info.Length > 1_000_000)
                        File.Move(_logFilePath, _logFilePath + ".bak", overwrite: true);
                }

                File.AppendAllText(_logFilePath, entry);
            }
            catch { /* never throw from error handler */ }
        }

        public string GetLogPath() => _logFilePath;

        /// <summary>Report a handled error with context.</summary>
        public static void Report(string message, Exception ex = null)
        {
            string full = ex != null ? $"{message}: {ex.Message}" : message;
            Debug.LogError(full);
        }
    }
}
