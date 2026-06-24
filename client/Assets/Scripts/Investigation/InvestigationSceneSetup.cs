// ============================================
// InvestigationSceneSetup — one-time scene initialization
// Places evidence objects, NPC positions based on case data
// Called by InvestigationManager after loading case data
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DetectiveRoyale.Core;
using DetectiveRoyale.NPC;

namespace DetectiveRoyale.Investigation
{
    public class InvestigationSceneSetup : MonoBehaviour
    {
        [Header("Spawn Points — Evidence")]
        [SerializeField] private Transform[] _evidenceSpawnPoints;

        [Header("Spawn Points — Witnesses")]
        [SerializeField] private Transform[] _witnessSpawnPoints;

        [Header("Spawn Points — Suspects")]
        [SerializeField] private Transform[] _suspectSpawnPoints;

        [Header("Prefabs")]
        [SerializeField] private GameObject _evidenceObjectPrefab;
        [SerializeField] private GameObject _witnessPrefab;

        [Header("Crime Scene")]
        [SerializeField] private GameObject _crimeSceneRoot;
        [SerializeField] private Transform  _playerStartPosition;

        private bool _setupComplete;

        // ---- Called by InvestigationManager after case data loaded ----
        public IEnumerator SetupScene()
        {
            if (_setupComplete) yield break;
            _setupComplete = true;

            yield return null; // wait one frame for everything to initialize

            PlaceEvidenceObjects();
            PlaceWitnesses();
            MovePlayerToStart();

            Debug.Log("[SceneSetup] Investigation scene initialized");
        }

        // ============================================
        // Evidence placement
        // ============================================

        private void PlaceEvidenceObjects()
        {
            var evidence = GameSession.DiscoveredEvidence;
            if (evidence == null) return;

            // If evidence objects already exist in scene (pre-placed), 
            // just assign their IDs. Otherwise spawn from pool.
            var existing = FindObjectsByType<ClueCollector>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
            {
                Debug.Log($"[SceneSetup] Found {existing.Length} pre-placed evidence objects");
                return;
            }

            // Dynamic placement using spawn points
            if (_evidenceObjectPrefab == null || _evidenceSpawnPoints == null) return;

            int count = Mathf.Min(
                GameSession.Case?.evidenceCount ?? 0,
                _evidenceSpawnPoints.Length);

            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(_evidenceObjectPrefab, _evidenceSpawnPoints[i]);
                go.name = $"Evidence_{i}";
                // Evidence ID is assigned in Inspector for pre-placed,
                // or set here from a case data mapping
            }
        }

        // ============================================
        // Witness placement
        // ============================================

        private void PlaceWitnesses()
        {
            if (_witnessPrefab == null || _witnessSpawnPoints == null) return;
            var witnesses = GameSession.Witnesses;
            if (witnesses == null) return;

            var existing = FindObjectsByType<AIChat>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0) return;

            int count = Mathf.Min(witnesses.Length, _witnessSpawnPoints.Length);
            for (int i = 0; i < count; i++)
            {
                var go   = Instantiate(_witnessPrefab, _witnessSpawnPoints[i]);
                go.name  = $"Witness_{witnesses[i].name}";

                // Set witness ID on components
                var chat = go.GetComponent<AIChat>();
                // AIChat._witnessId set via SerializedField — need a public setter
                // For dynamic spawning, use reflection or make the field internal
                var zone = go.GetComponent<NPCInteractionZone>();
                if (zone != null)
                {
                    // Assign witness ID via the zone's serialized field
                    // In a real project, add a public Init(string witnessId) method
                }

                var behaviour = go.GetComponent<WitnessBehaviour>();
                var label     = go.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null) label.text = witnesses[i].name;
            }
        }

        // ============================================
        // Player spawn
        // ============================================

        private void MovePlayerToStart()
        {
            var player = PlayerController.Instance;
            if (player == null || _playerStartPosition == null) return;
            player.transform.position = _playerStartPosition.position;
            player.transform.rotation = _playerStartPosition.rotation;
        }

        // ============================================
        // Crime scene atmosphere
        // ============================================

        public void SetNightMode(bool night)
        {
            // Adjust lighting for night/day based on time of death
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                    l.intensity = night ? 0.15f : 0.6f;
            }
            RenderSettings.ambientIntensity = night ? 0.3f : 0.8f;
        }
    }
}
