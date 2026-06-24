// ============================================
// PlayerSlotUI — one player row in the room lobby
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class PlayerSlotUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _tierText;
        [SerializeField] private TMP_Text _hostBadge;    // "HOST" label
        [SerializeField] private TMP_Text _readyBadge;   // "READY" label
        [SerializeField] private Image    _avatarImage;

        public void Setup(RoomPlayer player)
        {
            if (_usernameText) _usernameText.text = player.username;
            if (_tierText)     _tierText.text     = player.tier ?? "rookie";
            if (_hostBadge)    _hostBadge.gameObject.SetActive(player.isHost);
            if (_readyBadge)   _readyBadge.gameObject.SetActive(false); // updated via socket
        }

        public void SetReady(bool ready)
        {
            if (_readyBadge) _readyBadge.gameObject.SetActive(ready);
        }
    }
}
