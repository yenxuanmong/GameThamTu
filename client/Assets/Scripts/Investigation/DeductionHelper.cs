// ============================================
// DeductionHelper — assists player in forming conclusions
// Tracks what evidence supports which suspect/weapon/location
// ============================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    /// <summary>
    /// Client-side deduction logic.
    /// Analyses collected evidence and notes to suggest likely killer.
    /// This is purely client-side UX assistance — the server validates the actual answer.
    /// </summary>
    public class DeductionHelper : MonoBehaviour
    {
        public static DeductionHelper Instance { get; private set; }

        // Evidence → suspect linking (player manually assigns via UI)
        private readonly Dictionary<string, string> _evidenceToSuspect = new();
        private readonly Dictionary<string, int>    _suspectScores     = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            EvidenceSystem.Instance?.OnEvidenceAdded.Subscribe(OnEvidenceAdded);
        }

        void OnDestroy()
        {
            EvidenceSystem.Instance?.OnEvidenceAdded.Unsubscribe(OnEvidenceAdded);
        }

        private void OnEvidenceAdded(Evidence ev)
        {
            // Auto-scan for suspect name mentions in description
            if (GameSession.Suspects == null) return;
            foreach (var s in GameSession.Suspects)
            {
                if (ev.description?.Contains(s.name, System.StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    AddSuspectLink(ev.id, s.id);
                }
            }
        }

        // ============================================
        // Manual linking
        // ============================================

        public void AddSuspectLink(string evidenceId, string suspectId)
        {
            _evidenceToSuspect[evidenceId] = suspectId;
            RecalculateScores();
        }

        public void RemoveSuspectLink(string evidenceId)
        {
            _evidenceToSuspect.Remove(evidenceId);
            RecalculateScores();
        }

        // ============================================
        // Score calculation
        // ============================================

        private void RecalculateScores()
        {
            _suspectScores.Clear();
            foreach (var kv in _evidenceToSuspect)
            {
                if (!_suspectScores.ContainsKey(kv.Value))
                    _suspectScores[kv.Value] = 0;
                _suspectScores[kv.Value]++;
            }
        }

        // ============================================
        // Query
        // ============================================

        /// <summary>Returns suspect IDs ordered by evidence count descending.</summary>
        public List<(string suspectId, int evidenceCount)> GetRankedSuspects()
        {
            return _suspectScores
                .OrderByDescending(kv => kv.Value)
                .Select(kv => (kv.Key, kv.Value))
                .ToList();
        }

        /// <summary>Returns the suspect with most evidence links — or null.</summary>
        public string GetTopSuspectId()
        {
            if (_suspectScores.Count == 0) return null;
            return _suspectScores.OrderByDescending(kv => kv.Value).First().Key;
        }

        /// <summary>Returns list of evidence linked to a specific suspect.</summary>
        public List<string> GetEvidenceForSuspect(string suspectId)
        {
            return _evidenceToSuspect
                .Where(kv => kv.Value == suspectId)
                .Select(kv => kv.Key)
                .ToList();
        }

        public int GetEvidenceCount(string suspectId) =>
            _suspectScores.TryGetValue(suspectId, out int c) ? c : 0;

        public void Clear()
        {
            _evidenceToSuspect.Clear();
            _suspectScores.Clear();
        }
    }
}
