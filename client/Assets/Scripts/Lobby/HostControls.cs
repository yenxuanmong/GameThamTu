// ============================================
// HostControls — host-only actions in room lobby
// Kick players, start match, change settings
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    [System.Serializable] class StartMatchBody  { public string roomId; }
    [System.Serializable] class KickPlayerBody  { public string playerId; }
    [System.Serializable] class StartMatchResp  { public bool success; public string matchId; }

    public class HostControls : MonoBehaviour
    {
        [Header("Host panel — only shown to room host")]
        [SerializeField] private GameObject _panel;

        [Header("Actions")]
        [SerializeField] private Button   _startMatchBtn;
        [SerializeField] private Button   _forceStartBtn;
        [SerializeField] private Button   _openSettingsBtn;
        [SerializeField] private TMP_Text _startStatusText;

        [Header("Player count check")]
        [SerializeField] private int      _minPlayersToStart = 2;

        [Header("Settings panel ref")]
        [SerializeField] private RoomSettingsUI _settingsUI;

        private bool   _isHost;
        private string _currentRoomId;

        void Start()
        {
            _panel?.SetActive(false);
            SocketManager.Instance?.OnRoomJoined.AddListener(OnRoomJoined);
            SocketManager.Instance?.OnRoomUpdated.AddListener(OnRoomUpdated);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnRoomJoined.RemoveListener(OnRoomJoined);
            SocketManager.Instance.OnRoomUpdated.RemoveListener(OnRoomUpdated);
        }

        private void OnRoomJoined(RoomSocketPayload p)  => Refresh(p.room);
        private void OnRoomUpdated(RoomSocketPayload p) => Refresh(p.room);

        private void Refresh(Room room)
        {
            _currentRoomId = room.id;
            _isHost = AuthState.Instance.Player?.id == room.hostId;
            _panel?.SetActive(_isHost);

            if (_startMatchBtn)
                _startMatchBtn.interactable = room.currentPlayers >= _minPlayersToStart;
            if (_startStatusText)
                _startStatusText.text = room.currentPlayers < _minPlayersToStart
                    ? $"Need {_minPlayersToStart - room.currentPlayers} more player(s)"
                    : "Ready to start!";
        }

        // ============================================
        // Start match
        // ============================================

        public void OnClickStartMatch()
        {
            if (!_isHost || string.IsNullOrEmpty(_currentRoomId)) return;
            StartCoroutine(StartMatch());
        }

        private IEnumerator StartMatch()
        {
            if (_startMatchBtn) _startMatchBtn.interactable = false;
            if (_startStatusText) _startStatusText.text = "Starting...";

            // Trigger countdown via socket (server validates host)
            SocketManager.Instance.ReadyUp(_currentRoomId);

            yield return new WaitForSeconds(0.5f);
            if (_startMatchBtn) _startMatchBtn.interactable = true;
        }

        // ============================================
        // Open settings
        // ============================================

        public void OnClickOpenSettings()
        {
            if (!_isHost) return;
            var room = Multiplayer.RoomManager.Instance?.CurrentRoom;
            _settingsUI?.Open(room, true);
        }

        // ============================================
        // Kick player (called from PlayerSlotUI)
        // ============================================

        public void KickPlayer(string playerId)
        {
            if (!_isHost) return;
            UI.ConfirmDialog.Ask(
                "Kick Player",
                "Are you sure you want to kick this player?",
                () => StartCoroutine(DoKick(playerId)));
        }

        private IEnumerator DoKick(string playerId)
        {
            yield return ApiClient.Instance.Post<System.Object>(
                $"/rooms/{_currentRoomId}/kick/{playerId}",
                null,
                _ => UI.NotificationToast.Show("Player kicked.", "info"),
                err => UI.NotificationToast.Show($"Kick failed: {err}", "error"));
        }
    }
}
