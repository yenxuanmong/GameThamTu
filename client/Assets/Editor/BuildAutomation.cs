// ============================================
// BuildAutomation — CI/CD build scripts for Detective Royale
// Run via: Unity -batchmode -executeMethod BuildAutomation.BuildWindows
// ============================================
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DetectiveRoyale.Editor
{
    public static class BuildAutomation
    {
        private static readonly string[] Scenes = {
            "Assets/Scenes/Splash.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/Investigation.unity",
            "Assets/Scenes/Results.unity",
        };

        private static string BuildDir =>
            Path.Combine(Directory.GetCurrentDirectory(), "Builds");

        // ---- Windows x64 ----
        [MenuItem("Detective Royale/Build/Windows x64")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64,
                  Path.Combine(BuildDir, "Windows", "DetectiveRoyale.exe"));
        }

        // ---- macOS ----
        [MenuItem("Detective Royale/Build/macOS")]
        public static void BuildMac()
        {
            Build(BuildTarget.StandaloneOSX,
                  Path.Combine(BuildDir, "macOS", "DetectiveRoyale.app"));
        }

        // ---- Linux ----
        [MenuItem("Detective Royale/Build/Linux x64")]
        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64,
                  Path.Combine(BuildDir, "Linux", "DetectiveRoyale.x86_64"));
        }

        // ---- Android ----
        [MenuItem("Detective Royale/Build/Android APK")]
        public static void BuildAndroid()
        {
            PlayerSettings.Android.bundleVersionCode++;
            Build(BuildTarget.Android,
                  Path.Combine(BuildDir, "Android", "DetectiveRoyale.apk"));
        }

        // ---- WebGL ----
        [MenuItem("Detective Royale/Build/WebGL")]
        public static void BuildWebGL()
        {
            Build(BuildTarget.WebGL,
                  Path.Combine(BuildDir, "WebGL", "DetectiveRoyale"));
        }

        // ---- Development build ----
        [MenuItem("Detective Royale/Build/Development (Windows)")]
        public static void BuildDevelopment()
        {
            Build(BuildTarget.StandaloneWindows64,
                  Path.Combine(BuildDir, "Dev", "DetectiveRoyale_Dev.exe"),
                  development: true);
        }

        // ============================================
        // Core build method
        // ============================================

        private static void Build(BuildTarget target, string path, bool development = false)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            var options = new BuildPlayerOptions
            {
                scenes             = Scenes,
                locationPathName   = path,
                target             = target,
                options            = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            Debug.Log($"[Build] Starting {target} build → {path}");
            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] ✓ Succeeded in {report.summary.totalTime.TotalSeconds:F1}s " +
                          $"({report.summary.totalSize / 1024 / 1024} MB)");
            }
            else
            {
                Debug.LogError($"[Build] ✗ Failed: {report.summary.result}");
                foreach (var step in report.steps)
                    foreach (var msg in step.messages)
                        if (msg.type == LogType.Error)
                            Debug.LogError($"  {msg.content}");
            }
        }

        // ---- Version bump helper ----
        [MenuItem("Detective Royale/Build/Bump Version (Patch)")]
        public static void BumpPatchVersion()
        {
            var parts = Application.version.Split('.');
            if (parts.Length == 3 && int.TryParse(parts[2], out int patch))
            {
                string newVersion = $"{parts[0]}.{parts[1]}.{patch + 1}";
                PlayerSettings.bundleVersion = newVersion;
                Debug.Log($"[Build] Version bumped to {newVersion}");
            }
        }
    }
}
#endif
