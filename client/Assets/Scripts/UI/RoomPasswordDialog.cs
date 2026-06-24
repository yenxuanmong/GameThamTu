// ============================================
// RoomPasswordDialog — wraps PasswordInputDialog for room joining
// Integrates with LobbyManager to join password-protected rooms
// ============================================
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class RoomPasswordDialog : MonoBehaviour
    {
        [SerializeField] private PasswordInputDialog _dialog;

        private string _pendingRoomId;

        void Awake()
        {
            if (_dialog == null) _dialog = PasswordInputDialog.Instance;
        }

        /// <summary>
        /// Called from LobbyManager when joining a password-protected room.
        /// </summary>
        public void RequestPassword(string roomId, string roomName)
        {
            _pendingRoomId = roomId;
            var dlg = _dialog ?? PasswordInputDialog.Instance;
            dlg?.Show(
                $"Enter password for \"{roomName}\"",
                pwd => OnPasswordConfirmed(pwd),
                () => _pendingRoomId = null);
        }

        private void OnPasswordConfirmed(string password)
        {
            if (string.IsNullOrEmpty(_pendingRoomId)) return;
            SocketManager.Instance.JoinRoom(_pendingRoomId, password);
            _pendingRoomId = null;
        }
    }
}
