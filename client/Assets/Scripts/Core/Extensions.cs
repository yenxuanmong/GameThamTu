// ============================================
// Extensions — helpful C# extension methods
// ============================================
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public static class Extensions
    {
        // Convert async Task to coroutine
        public static IEnumerator AsCoroutine(this Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        // Capitalise first letter
        public static string Capitalise(this string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

        // Format snake_case → Title Case
        public static string ToTitleCase(this string s) =>
            string.IsNullOrEmpty(s) ? s :
            System.Text.RegularExpressions.Regex.Replace(
                s.Replace("_", " "), @"\b\w", m => m.Value.ToUpper());

        // Format seconds as mm:ss
        public static string ToTimerString(this int seconds)
        {
            int m = seconds / 60, s = seconds % 60;
            return $"{m:D2}:{s:D2}";
        }

        // Tier display label
        public static string TierLabel(this string tier) => tier?.ToTitleCase() ?? "Rookie";

        // RP change colour
        public static Color RpColor(this int rpChange) =>
            rpChange >= 0 ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
    }
}
