// ============================================
// SaveManager — local game data persistence
// Stores tutorial progress, settings, offline cache
// ============================================
using System;
using System.IO;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public static class SaveManager
    {
        private static readonly string SaveDir =
            Path.Combine(Application.persistentDataPath, "DetectiveRoyale");

        // ---- Tutorial flags ----
        public static bool HasCompletedTutorial
        {
            get => PlayerPrefs.GetInt("tutorial_done", 0) == 1;
            set => PlayerPrefs.SetInt("tutorial_done", value ? 1 : 0);
        }

        // ---- Notification badges ----
        public static int UnreadNotifications
        {
            get => PlayerPrefs.GetInt("unread_notifs", 0);
            set => PlayerPrefs.SetInt("unread_notifs", Mathf.Max(0, value));
        }

        // ---- Match draft (offline safety) ----

        public static void SaveMatchDraft(string matchId, string conclusionJson)
        {
            EnsureDir();
            string path = Path.Combine(SaveDir, $"draft_{matchId}.json");
            File.WriteAllText(path, conclusionJson);
        }

        public static string LoadMatchDraft(string matchId)
        {
            string path = Path.Combine(SaveDir, $"draft_{matchId}.json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static void DeleteMatchDraft(string matchId)
        {
            string path = Path.Combine(SaveDir, $"draft_{matchId}.json");
            if (File.Exists(path)) File.Delete(path);
        }

        // ---- Achievement cache ----

        public static void SaveAchievements(string json)
        {
            EnsureDir();
            File.WriteAllText(Path.Combine(SaveDir, "achievements.json"), json);
        }

        public static string LoadAchievements()
        {
            string path = Path.Combine(SaveDir, "achievements.json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        // ---- Clear all local data ----

        public static void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            if (Directory.Exists(SaveDir))
                Directory.Delete(SaveDir, recursive: true);
        }

        private static void EnsureDir()
        {
            if (!Directory.Exists(SaveDir))
                Directory.CreateDirectory(SaveDir);
        }
    }
}
