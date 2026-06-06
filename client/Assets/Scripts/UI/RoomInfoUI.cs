// ============================================
// RoomInfoUI — shows room details panel inside the lobby
// Displays room name, difficulty, player count, host info
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class RoomInfoUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Info")]
        [SerializeField] private TMP_Text  _roomNameText;
        [SerializeField] private TMP_Text  _difficultyText;
        [SerializeField] private TMP_Text  _playerCountText;
        [SerializeField] private TMP_Text  _hostNameText;
        [SerializeField] private TMP_Text  _visibilityText;
        [SerializeField] private TMP_Text  _roomIdText;       // for invite code
        [SerializeField] private TMP_Text  _statusText;

        [Header("Buttons")]
        [SerializeField] private Button    _copyIdBtn;
        [SerializeField] private Button    _settingsBtn;      // host only
        [SerializeField] private TMP_Text  _copyToastText;

        void Start() => _panel?.SetActive(false);

        // ---- Socket-driven refresh ----

        void OnEnable()
        {
            SocketManager.Instance?.OnRoomJoined.AddListener(OnRoomJoined);
            SocketManager.Instance?.OnRoomUpdated.AddListener(OnRoomUpdated);
        }

        void OnDisable()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnRoomJoined.RemoveListener(OnRoomJoined);
            SocketManager.Instance.OnRoomUpdated.RemoveListener(OnRoomUpdated);
        }

        private void OnRoomJoined(RoomSocketPayload p)  => Populate(p.room);
        private void OnRoomUpdated(RoomSocketPayload p) => Populate(p.room);

        // ---- Populate ----

        public void Show(Room room)
        {
            _panel?.SetActive(true);
            Populate(room);
        }

        public void Hide() => _panel?.SetActive(false);

        private void Populate(Room room)
        {
            if (room == null) return;

            if (_roomNameText)   _roomNameText.text   = room.name;
            if (_difficultyText) _difficultyText.text = room.difficulty.ToUpper();
            if (_playerCountText)
                _playerCountText.text = $"{room.currentPlayers}/{room.maxPlayers} players";
            if (_visibilityText)
                _visibilityText.text = room.hasPassword ? "🔒 Private" : "🌐 Public";
            if (_roomIdText)
                _roomIdText.text = room.id[..Mathf.Min(8, room.id.Length)].ToUpper();
            if (_statusText)
                _statusText.text = room.isInMatch ? "IN MATCH" : "WAITING";

            // Show settings button only to host
            bool isHost = AuthState.Instance.Player?.id == room.hostId;
            _settingsBtn?.gameObject.SetActive(isHost);
        }

        // ---- Copy room ID to clipboard ----

        public void OnClickCopyId()
        {
            if (_roomIdText == null) return;
            GUIUtility.systemCopyBuffer = _roomIdText.text;
            StartCoroutine(ShowCopyToast());
        }

        private IEnumerator ShowCopyToast()
        {
            if (_copyToastText)
            {
                _copyToastText.gameObject.SetActive(true);
                _copyToastText.text = "Copied!";
                yield return new WaitForSeconds(1.5f);
                _copyToastText.gameObject.SetActive(false);
            }
        }
    }
}
