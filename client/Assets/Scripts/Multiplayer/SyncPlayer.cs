// ============================================
// SyncPlayer — shows other players' real-time actions
// in the investigation scene (evidence found, submitted, etc.)
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    public class SyncPlayer : MonoBehaviour
    {
        [Header("Player status panel")]
        [SerializeField] private Transform   _playerStatusContent;
        [SerializeField] private GameObject  _playerStatusPrefab;

        [Header("Evidence sharing toast")]
        [SerializeField] private TMP_Text    _sharedEvidenceToast;

        private readonly Dictionary<string, GameObject> _statusSlots = new();

        void Start()
        {
            SocketManager.Instance.OnEvidenceFound.AddListener(OnEvidenceFound);
            SocketManager.Instance.OnPlayerSubmitted.AddListener(OnPlayerSubmitted);
            SocketManager.Instance.OnMatchStarted.AddListener(OnMatchStarted);
            InitPlayerSlots();
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnEvidenceFound.RemoveListener(OnEvidenceFound);
            SocketManager.Instance.OnPlayerSubmitted.RemoveListener(OnPlayerSubmitted);
            SocketManager.Instance.OnMatchStarted.RemoveListener(OnMatchStarted);
        }

        // ---- Build player slot per player in match ----

        private void InitPlayerSlots()
        {
            if (GameSession.Match == null) return;
            string myId = AuthState.Instance.Player?.id;

            foreach (var pid in GameSession.Match.playerIds)
            {
                if (pid == myId) continue; // skip self
                if (_playerStatusPrefab == null) break;
                var go = Instantiate(_playerStatusPrefab, _playerStatusContent);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 0) texts[0].text = pid[..8]; // show partial ID until name loaded
                _statusSlots[pid] = go;
            }
        }

        // ---- Socket events ----

        private void OnEvidenceFound(EvidenceFoundPayload p)
        {
            if (p.playerId == AuthState.Instance.Player?.id) return; // handled by EvidenceSystem
            NotificationToast.Show($"A player found evidence.", "info");
        }

        private void OnPlayerSubmitted(SubmitPayload p)
        {
            if (p.playerId == AuthState.Instance.Player?.id) return;

            if (_statusSlots.TryGetValue(p.playerId, out var slot))
            {
                var texts = slot.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 1) texts[1].text = "✓ Submitted";
                var img = slot.GetComponent<Image>();
                if (img) img.color = new Color(0.2f, 1f, 0.3f, 0.4f);
            }
            NotificationToast.Show("Another investigator has submitted their conclusion.", "info");
        }

        private void OnMatchStarted(MatchStartedPayload _) => InitPlayerSlots();
    }
}
