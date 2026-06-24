// ============================================
// EventBus — lightweight global event system
// Decouples systems without direct references
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<string, List<Delegate>> _handlers = new();

        // ---- Subscribe ----

        public static void Subscribe<T>(string eventName, Action<T> handler)
        {
            if (!_handlers.ContainsKey(eventName))
                _handlers[eventName] = new List<Delegate>();
            _handlers[eventName].Add(handler);
        }

        public static void Subscribe(string eventName, Action handler)
        {
            if (!_handlers.ContainsKey(eventName))
                _handlers[eventName] = new List<Delegate>();
            _handlers[eventName].Add(handler);
        }

        // ---- Unsubscribe ----

        public static void Unsubscribe<T>(string eventName, Action<T> handler)
        {
            if (_handlers.TryGetValue(eventName, out var list))
                list.Remove(handler);
        }

        public static void Unsubscribe(string eventName, Action handler)
        {
            if (_handlers.TryGetValue(eventName, out var list))
                list.Remove(handler);
        }

        // ---- Publish ----

        public static void Publish<T>(string eventName, T payload)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            foreach (var d in list.ToArray())
            {
                try { (d as Action<T>)?.Invoke(payload); }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Handler error on '{eventName}': {ex.Message}");
                }
            }
        }

        public static void Publish(string eventName)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            foreach (var d in list.ToArray())
            {
                try { (d as Action)?.Invoke(); }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Handler error on '{eventName}': {ex.Message}");
                }
            }
        }

        // ---- Clear all ----

        public static void Clear() => _handlers.Clear();

        // ---- Predefined event names ----
        public static class Events
        {
            public const string EVIDENCE_COLLECTED  = "evidence.collected";
            public const string WITNESS_INTERROGATED = "witness.interrogated";
            public const string HINT_USED           = "hint.used";
            public const string CONCLUSION_SUBMITTED = "conclusion.submitted";
            public const string MATCH_ENDED         = "match.ended";
            public const string PHASE_CHANGED       = "phase.changed";
            public const string PLAYER_JOINED       = "player.joined";
            public const string PLAYER_LEFT         = "player.left";
            public const string ACHIEVEMENT_UNLOCKED = "achievement.unlocked";
            public const string PROFILE_UPDATED     = "profile.updated";
        }
    }
}
