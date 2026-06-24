// ============================================
// EvidenceSystem — tracks discovered evidence for this player
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    [Serializable] class DiscoveredResp { public Evidence[] evidence; }
    [Serializable] class EvidenceResp   { public Evidence   evidence; }

    public class EvidenceSystem : MonoBehaviour
    {
        public static EvidenceSystem Instance { get; private set; }

        private readonly List<Evidence> _discovered = new();

        public IReadOnlyList<Evidence> DiscoveredEvidence => _discovered;

        public event System.Action<Evidence> OnEvidenceAdded;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        IEnumerator Start()
        {
            // Load already-discovered evidence (e.g. reconnect)
            yield return StartCoroutine(LoadDiscoveredEvidence());
        }

        // ---- Called when socket says we found evidence ----

        public void OnEvidenceDiscovered(string evidenceId)
        {
            // Don't double-add
            if (_discovered.Exists(e => e.id == evidenceId)) return;

            StartCoroutine(FetchEvidence(evidenceId));
        }

        private IEnumerator FetchEvidence(string evidenceId)
        {
            yield return ApiClient.Instance.Get<EvidenceResp>(
                $"/matches/{GameSession.MatchId}/evidence/{evidenceId}",
                resp =>
                {
                    _discovered.Add(resp.evidence);
                    OnEvidenceAdded?.Invoke(resp.evidence);
                },
                err => Debug.LogWarning($"[EvidenceSystem] Fetch error: {err}"));
        }

        private IEnumerator LoadDiscoveredEvidence()
        {
            yield return ApiClient.Instance.Get<DiscoveredResp>(
                $"/matches/{GameSession.MatchId}/evidence",
                resp =>
                {
                    _discovered.Clear();
                    if (resp.evidence != null)
                        _discovered.AddRange(resp.evidence);
                },
                err => Debug.LogWarning($"[EvidenceSystem] Load error: {err}"));
        }

        // ---- Examine an object in the scene ----

        public void ExamineObject(string evidenceId)
        {
            SocketManager.Instance.ExamineEvidence(GameSession.MatchId, evidenceId);
        }

        // ---- Notes ----

        public IEnumerator UpdateNotes(string evidenceId, string notes, System.Action onDone = null)
        {
            yield return ApiClient.Instance.Patch<object>(
                $"/matches/{GameSession.MatchId}/evidence/{evidenceId}/notes",
                new { notes },
                _ => onDone?.Invoke(),
                err => Debug.LogWarning($"[EvidenceSystem] Notes error: {err}"));
        }
    }
}
