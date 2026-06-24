// ============================================
// RoomListItem — one row in the room list
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    public class RoomListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _difficultyText;
        [SerializeField] private TMP_Text _playersText;
        [SerializeField] private TMP_Text _lockIcon;   // "🔒" or ""
        [SerializeField] private Button   _joinBtn;

        private Action _onJoin;

        public void Setup(Room room, Action onJoin)
        {
            _onJoin = onJoin;
            if (_nameText)       _nameText.text       = room.name;
            if (_difficultyText) _difficultyText.text = room.difficulty.ToUpper();
            if (_playersText)    _playersText.text     = $"{room.currentPlayers}/{room.maxPlayers}";
            if (_lockIcon)       _lockIcon.text        = room.hasPassword ? "🔒" : "";

            bool full = room.currentPlayers >= room.maxPlayers;
            if (_joinBtn) _joinBtn.interactable = !full && !room.isInMatch;
        }

        public void OnClickJoin() => _onJoin?.Invoke();
    }
}
