// ============================================
// VoiceGatewayClient — WebRTC signalling via Socket.IO
// Handles offer/answer/ICE exchange with server relay
// Full WebRTC logic requires com.unity.webrtc package
// ============================================
using System.Collections.Generic;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    /// <summary>
    /// Manages peer-to-peer voice chat signalling.
    /// Connect this to VoiceChat.cs which handles the UI.
    /// Requires Unity WebRTC package (com.unity.webrtc).
    /// </summary>
    public class VoiceGatewayClient : MonoBehaviour
    {
        [Header("Voice settings")]
        [SerializeField] private bool   _autoJoinOnMatchStart = true;
        [SerializeField] private string _stunServer = "stun:stun.l.google.com:19302";

        private string _currentMatchId;
        private bool   _inVoiceSession;

        // Track peers: playerId → connection state
        private readonly Dictionary<string, PeerState> _peers = new();

        void Start()
        {
            SocketManager.Instance.OnMatchStarted.AddListener(OnMatchStarted);
            SocketManager.Instance.OnMatchEnded.AddListener(_ => LeaveVoice());

            // Listen to raw voice socket events
            // These are emitted by the server voice gateway
            // When using SocketIOUnity, wire up:
            // _socket.On("voice:offer",  ...)
            // _socket.On("voice:answer", ...)
            // _socket.On("voice:ice",    ...)
            // _socket.On("voice:muted",  ...)
        }

        void OnDestroy()
        {
            LeaveVoice();
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnMatchStarted.RemoveListener(OnMatchStarted);
        }

        // ============================================
        // Join / Leave
        // ============================================

        private void OnMatchStarted(MatchStartedPayload p)
        {
            _currentMatchId = p.matchId;
            if (_autoJoinOnMatchStart)
                JoinVoice();
        }

        public void JoinVoice()
        {
            if (_inVoiceSession || string.IsNullOrEmpty(_currentMatchId)) return;
            _inVoiceSession = true;
            SocketManager.Instance.JoinVoice(_currentMatchId);
            Debug.Log("[VoiceGateway] Joined voice session");

            // TODO: InitiateWebRTCPeers() when WebRTC package is active
        }

        public void LeaveVoice()
        {
            if (!_inVoiceSession) return;
            _inVoiceSession = false;
            if (!string.IsNullOrEmpty(_currentMatchId))
                SocketManager.Instance.LeaveVoice(_currentMatchId);
            CleanupPeers();
            Debug.Log("[VoiceGateway] Left voice session");
        }

        // ============================================
        // Signalling handlers (called when socket events arrive)
        // ============================================

        public void OnRemoteOffer(string fromPlayerId, string sdp)
        {
            Debug.Log($"[VoiceGateway] Received offer from {fromPlayerId}");
            // TODO: Create RTCPeerConnection, set remote description, create answer
            // SocketManager.Instance.SendVoiceAnswer(_currentMatchId, fromPlayerId, answerSdp);
        }

        public void OnRemoteAnswer(string fromPlayerId, string sdp)
        {
            Debug.Log($"[VoiceGateway] Received answer from {fromPlayerId}");
            // TODO: Set remote description on existing peer connection
        }

        public void OnIceCandidate(string fromPlayerId, string candidate)
        {
            Debug.Log($"[VoiceGateway] ICE candidate from {fromPlayerId}");
            // TODO: Add ICE candidate to peer connection
        }

        public void OnPlayerMuted(string playerId, bool muted)
        {
            if (_peers.TryGetValue(playerId, out var peer))
            {
                peer.IsMuted = muted;
                Debug.Log($"[VoiceGateway] {playerId} is {(muted ? "muted" : "unmuted")}");
            }
        }

        // ============================================
        // Peer management
        // ============================================

        private void CleanupPeers()
        {
            _peers.Clear();
        }

        private class PeerState
        {
            public string PlayerId;
            public bool   IsMuted;
            public bool   IsConnected;
        }
    }
}
