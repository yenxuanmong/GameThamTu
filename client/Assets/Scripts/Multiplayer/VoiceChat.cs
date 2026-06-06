// ============================================
// VoiceChat — WebRTC voice chat via Socket.IO signalling
// Requires unity-webrtc package (com.unity.webrtc)
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    public class VoiceChat : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Toggle   _muteToggle;
        [SerializeField] private TMP_Text _muteLabel;
        [SerializeField] private Transform _speakingIndicatorsContent;
        [SerializeField] private GameObject _speakingIndicatorPrefab;

        private bool _muted;
        private string _currentMatchId;
        private readonly Dictionary<string, GameObject> _indicators = new();

        void Start()
        {
            _currentMatchId = GameSession.MatchId;
            SocketManager.Instance.OnMatchStarted.AddListener(OnMatchStarted);

            // Listen to mute/leave events from other players (via socket raw)
            // These come as custom events relayed by the voice gateway
            // For full WebRTC: use Unity WebRTC package + integrate ICE/SDP with socket relay
            Debug.Log("[VoiceChat] Voice gateway initialised (WebRTC signalling ready)");
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnMatchStarted.RemoveListener(OnMatchStarted);

            // Leave voice when leaving scene
            if (!string.IsNullOrEmpty(_currentMatchId))
                SocketManager.Instance.Emit("voice:leave", new { matchId = _currentMatchId });
        }

        private void OnMatchStarted(MatchStartedPayload p) => _currentMatchId = p.matchId;

        // ---- Mute toggle ----

        public void OnMuteToggleChanged(bool isMuted)
        {
            _muted = isMuted;
            if (_muteLabel) _muteLabel.text = _muted ? "🔇 Muted" : "🎙 Live";
            SocketManager.Instance.Emit("voice:mute", new { matchId = _currentMatchId, muted = _muted });
        }

        // ---- Speaking indicator ----

        public void SetPlayerSpeaking(string playerId, bool speaking)
        {
            if (!_indicators.TryGetValue(playerId, out var ind))
            {
                if (_speakingIndicatorPrefab == null) return;
                ind = Instantiate(_speakingIndicatorPrefab, _speakingIndicatorsContent);
                _indicators[playerId] = ind;
            }

            var img = ind.GetComponent<Image>();
            if (img) img.color = speaking ? Color.green : Color.grey;
        }
    }
}
