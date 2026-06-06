// ============================================
// JsonHelper — Unity JsonUtility extensions for arrays
// JsonUtility cannot deserialise top-level arrays natively
// ============================================
using System;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public static class JsonHelper
    {
        /// <summary>Deserialise a JSON array into T[].
        /// Wraps the array in {"items":[...]} before parsing.</summary>
        public static T[] FromJsonArray<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<T>();
            string wrapped = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper?.items ?? Array.Empty<T>();
        }

        /// <summary>Serialise T[] to a JSON array string.</summary>
        public static string ToJsonArray<T>(T[] array)
        {
            var wrapper = new Wrapper<T> { items = array };
            string wrapped = JsonUtility.ToJson(wrapper);
            // Strip the outer {"items":...} wrapper
            int start = wrapped.IndexOf('[');
            int end   = wrapped.LastIndexOf(']');
            return start < 0 || end < 0 ? "[]" : wrapped.Substring(start, end - start + 1);
        }

        /// <summary>Safe parse — returns default on error.</summary>
        public static T SafeParse<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try   { return JsonUtility.FromJson<T>(json); }
            catch { return null; }
        }

        /// <summary>Extract a single string field from raw JSON without full deserialisation.</summary>
        public static string ExtractField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string key    = $"\"{fieldName}\"";
            int    keyIdx = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int colon = json.IndexOf(':', keyIdx + key.Length);
            if (colon < 0) return null;

            int valueStart = colon + 1;
            while (valueStart < json.Length && json[valueStart] == ' ') valueStart++;
            if (valueStart >= json.Length) return null;

            if (json[valueStart] == '"')
            {
                int end = json.IndexOf('"', valueStart + 1);
                return end < 0 ? null : json.Substring(valueStart + 1, end - valueStart - 1);
            }
            else
            {
                int end = valueStart;
                while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
                return json.Substring(valueStart, end - valueStart).Trim();
            }
        }

        [Serializable]
        private class Wrapper<T> { public T[] items; }
    }
}
