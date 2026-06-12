// ============================================
// SocketManager — Socket.IO v4 client (SocketIOUnity)
// Package: com.itisnajim.socketiounity (OpenUPM)
// ============================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DetectiveRoyale.Core
{
    public class SocketManager : MonoBehaviour
    {
        public static SocketManager Instance { get; private set; }

        // ---- Events the game listens to ----
        public UnityEvent<RoomSocketPayload>    OnRoomJoined        = new();
        public UnityEvent<RoomSocketPayload>    OnRoomUpdated       = new();
        public UnityEvent<CountdownPayload>     OnRoomCountdown     = new();
        public UnityEvent<MatchStartedPayload>  OnMatchStarted      = new();
        public UnityEvent<PhasePayload>         OnPhaseChanged      = new();
        public UnityEvent<TimerPayload>         OnTimerTick         = new();
        public UnityEvent<SubmitPayload>        OnPlayerSubmitted   = new();
        public UnityEvent<MatchEndedPayload>    OnMatchEnded        = new();
        public UnityEvent<EvidenceFoundPayload> OnEvidenceFound     = new();
        public UnityEvent<HintPayload>          OnHintReceived      = new();
        public UnityEvent<NPCResponsePayload>   OnNpcResponse       = new();
        public UnityEvent<SocketErrorPayload>   OnSocketError       = new();
        public UnityEvent<NotificationPayload>  OnNotification      = new();
        public UnityEvent                       OnConnected         = new();
        public UnityEvent                       OnDisconnected      = new();

        private bool _isConnected;
        public bool IsConnected => _isConnected;

        // ---- SocketIOUnity client ----
        private SocketIOClient.SocketIO _socket;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        // ============================================
        // Connect / Disconnect
        // ============================================

        public void Connect()
        {
            if (_isConnected || _socket != null) return;
            StartCoroutine(ConnectCoroutine());
        }

        private IEnumerator ConnectCoroutine()
        {
            string token = AuthState.Instance?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[SocketManager] No access token — cannot connect");
                yield break;
            }

            string url = GameConfig.Instance.SocketUrl;
            Debug.Log($"[SocketManager] Connecting to {url}");

            _socket = new SocketIOClient.SocketIO(url, new SocketIOClient.SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Auth      = new Dictionary<string, string> { { "token", token } },
                ReconnectionAttempts = GameConfig.Instance.SocketMaxReconnects,
                ReconnectionDelay    = (int)(GameConfig.Instance.SocketReconnectDelay * 1000),
            });

            RegisterHandlers();

            bool done  = false;
            bool error = false;

            _socket.OnConnected += (_, __) =>
            {
                _isConnected = true;
                done = true;
                UnityMainThreadDispatcher.Instance.Enqueue(() => OnConnected?.Invoke());
                Debug.Log("[SocketManager] Connected ✅");
            };

            _socket.OnDisconnected += (_, reason) =>
            {
                _isConnected = false;
                UnityMainThreadDispatcher.Instance.Enqueue(() => OnDisconnected?.Invoke());
                Debug.LogWarning($"[SocketManager] Disconnected: {reason}");
            };

            _socket.OnError += (_, err) =>
            {
                error = true;
                done  = true;
                Debug.LogError($"[SocketManager] Connection error: {err}");
            };

            // Fire-and-forget async connect
            _ = _socket.ConnectAsync();

            // Wait up to 10 seconds for connection
            float elapsed = 0f;
            while (!done && elapsed < 10f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (error || !_isConnected)
            {
                Debug.LogError("[SocketManager] Failed to connect — disposing socket");
                _socket?.Dispose();
                _socket = null;
            }
        }

        public void Disconnect()
        {
            if (!_isConnected && _socket == null) return;
            _ = _socket?.DisconnectAsync();
            _socket?.Dispose();
            _socket        = null;
            _isConnected   = false;
            OnDisconnected?.Invoke();
        }

        // ============================================
        // Emit helpers
        // ============================================

        public void Emit(string eventName, object data = null)
        {
            if (!_isConnected || _socket == null)
            {
                Debug.LogWarning($"[SocketManager] Cannot emit '{eventName}' — not connected");
                return;
            }

            if (data == null)
                _ = _socket.EmitAsync(eventName);
            else
                _ = _socket.EmitAsync(eventName, data);
        }

        // ---- Room ----
        public void CreateRoom(object settings)
            => Emit(ClientEvent.RoomCreate, new { settings });

        public void JoinRoom(string roomId, string password = null)
            => Emit(ClientEvent.RoomJoin, new { roomId, password });

        public void LeaveRoom(string roomId)
            => Emit(ClientEvent.RoomLeave, new { roomId });

        public void ReadyUp(string roomId)
            => Emit(ClientEvent.RoomReady, new { roomId });

        public void SendRoomChat(string roomId, string message)
            => Emit(ClientEvent.RoomChat, new { roomId, message });

        // ---- Queue ----
        public void JoinQueue(string difficulty, string region = null)
            => Emit(ClientEvent.QueueJoin, new { difficulty, region });

        public void LeaveQueue()
            => Emit(ClientEvent.QueueLeave);

        // ---- Match ----
        public void SubmitConclusion(string matchId, DetectiveRoyale.Core.Models.ConclusionPayload conclusion)
            => Emit(ClientEvent.MatchSubmitConclusion, new { matchId, conclusion });

        public void RequestHint(string matchId)
            => Emit(ClientEvent.MatchRequestHint, new { matchId });

        // ---- Investigation ----
        public void ExamineEvidence(string matchId, string evidenceId)
            => Emit(ClientEvent.InvestigationExamineEvidence, new { matchId, evidenceId });

        public void InterrogateWitness(string matchId, string witnessId, string message)
            => Emit(ClientEvent.InvestigationInterrogateWitness, new { matchId, witnessId, message });

        public void AddNote(string matchId, string note, string relatedId = null)
            => Emit(ClientEvent.InvestigationAddNote, new { matchId, note, relatedId });

        public void SendEmote(string matchId, string emoteKey)
            => Emit(ClientEvent.InvestigationEmote, new { matchId, emote = emoteKey });

        // ---- Voice ----
        public void JoinVoice(string matchId)
            => Emit(ClientEvent.VoiceJoin, new { matchId });

        public void LeaveVoice(string matchId)
            => Emit(ClientEvent.VoiceLeave, new { matchId });

        public void SetVoiceMute(string matchId, bool muted)
            => Emit(ClientEvent.VoiceMute, new { matchId, muted });

        public void SendVoiceOffer(string matchId, string targetId, string sdp)
            => Emit(ClientEvent.VoiceOffer, new { matchId, targetId, sdp });

        public void SendVoiceAnswer(string matchId, string targetId, string sdp)
            => Emit(ClientEvent.VoiceAnswer, new { matchId, targetId, sdp });

        public void SendIceCandidate(string matchId, string targetId, string candidate)
            => Emit(ClientEvent.VoiceIce, new { matchId, targetId, candidate });

        // ============================================
        // Server → Client event handlers
        // ============================================

        private void RegisterHandlers()
        {
            // ---- Room ----
            _socket.On(ServerEvent.RoomJoined,    r => Dispatch(OnRoomJoined,    r.GetValue<RoomSocketPayload>()));
            _socket.On(ServerEvent.RoomUpdated,   r => Dispatch(OnRoomUpdated,   r.GetValue<RoomSocketPayload>()));
            _socket.On(ServerEvent.RoomCountdown, r => Dispatch(OnRoomCountdown, r.GetValue<CountdownPayload>()));

            // ---- Match ----
            _socket.On(ServerEvent.MatchStarted,         r => Dispatch(OnMatchStarted,      r.GetValue<MatchStartedPayload>()));
            _socket.On(ServerEvent.MatchPhaseChanged,    r => Dispatch(OnPhaseChanged,       r.GetValue<PhasePayload>()));
            _socket.On(ServerEvent.MatchTimer,           r => Dispatch(OnTimerTick,          r.GetValue<TimerPayload>()));
            _socket.On(ServerEvent.MatchPlayerSubmitted, r => Dispatch(OnPlayerSubmitted,    r.GetValue<SubmitPayload>()));
            _socket.On(ServerEvent.MatchEnded,           r => Dispatch(OnMatchEnded,         r.GetValue<MatchEndedPayload>()));

            // ---- Investigation ----
            _socket.On(ServerEvent.InvestigationEvidenceFound, r => Dispatch(OnEvidenceFound, r.GetValue<EvidenceFoundPayload>()));
            _socket.On(ServerEvent.InvestigationHint,          r => Dispatch(OnHintReceived,  r.GetValue<HintPayload>()));

            // ---- NPC ----
            _socket.On(ServerEvent.NpcResponse, r => Dispatch(OnNpcResponse, r.GetValue<NPCResponsePayload>()));

            // ---- System ----
            _socket.On(ServerEvent.Error,        r => Dispatch(OnSocketError,   r.GetValue<SocketErrorPayload>()));
            _socket.On(ServerEvent.Notification, r => Dispatch(OnNotification,  r.GetValue<NotificationPayload>()));
        }

        // ---- Thread-safe dispatch to Unity main thread ----
        private void Dispatch<T>(UnityEvent<T> ev, T payload)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                try   { ev?.Invoke(payload); }
                catch (Exception ex) { Debug.LogError($"[SocketManager] Event error: {ex.Message}"); }
            });
        }
    }

    // ============================================
    // Socket payload types
    // ============================================

    [Serializable] public class RoomSocketPayload   { public Room room; public string playerId; public string roomId; }
    [Serializable] public class CountdownPayload    { public int seconds; }
    [Serializable] public class MatchStartedPayload { public string matchId; public string caseId; }
    [Serializable] public class PhasePayload        { public string phase; public int timeRemaining; }
    [Serializable] public class TimerPayload        { public int timeRemaining; }
    [Serializable] public class SubmitPayload       { public string playerId; public string submittedAt; }
    [Serializable] public class MatchEndedPayload   { public MatchScore[] scores; public string winnerId; }
    [Serializable] public class EvidenceFoundPayload{ public string evidenceId; public string playerId; }
    [Serializable] public class HintPayload         { public string hint; public int hintsRemaining; }
    [Serializable] public class NPCResponsePayload  { public string witnessId; public string message; public string stressLevel; }
    [Serializable] public class SocketErrorPayload  { public string code; public string message; }
    [Serializable] public class NotificationPayload { public string type; public string message; }
}
