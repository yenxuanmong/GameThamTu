// ============================================
// SocketManager — Socket.IO client wrapper
// Uses NativeWebSocket + manual Socket.IO protocol (engine.io v4)
// Or use the SocketIOUnity package if available in project
// ============================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace DetectiveRoyale.Core
{
    /// <summary>
    /// Lightweight Socket.IO v4 client.
    /// Requires: NativeWebSocket package  (com.endel.nativewebsocket)
    /// OR SocketIOUnity (itisnajim.socketiounity) — adjust accordingly.
    /// This implementation uses SocketIOUnity conventions.
    /// </summary>
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

        // ---- Internal: SocketIOUnity client ----
        // Uncomment and adapt if using SocketIOUnity package:
        // private SocketIOClient.SocketIO _socket;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ============================================
        // Connect
        // ============================================

        public void Connect()
        {
            if (_isConnected) return;
            StartCoroutine(ConnectCoroutine());
        }

        private IEnumerator ConnectCoroutine()
        {
            string token = AuthState.Instance?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[SocketManager] No token — cannot connect");
                yield break;
            }

            // ----- SocketIOUnity implementation -----
            // var uri = new Uri(GameConfig.Instance.SocketUrl);
            // _socket = new SocketIOClient.SocketIO(uri, new SocketIOClient.SocketIOOptions
            // {
            //     Transport    = SocketIOClient.Transport.TransportProtocol.WebSocket,
            //     Auth         = new { token = token }
            // });
            // RegisterHandlers();
            // yield return _socket.ConnectAsync().AsCoroutine();
            // _isConnected = true;
            // OnConnected?.Invoke();

            // ----- Stub until Socket.IO package is installed -----
            Debug.Log($"[SocketManager] Connecting to {GameConfig.Instance.SocketUrl}");
            yield return new WaitForSeconds(0.5f);
            _isConnected = true;
            OnConnected?.Invoke();
            Debug.Log("[SocketManager] Connected (stub)");
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            // _socket?.DisconnectAsync();
            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        // ============================================
        // Emit helpers
        // ============================================

        public void Emit(string eventName, object data = null)
        {
            if (!_isConnected)
            {
                Debug.LogWarning($"[SocketManager] Cannot emit '{eventName}' — not connected");
                return;
            }
            string json = data != null ? JsonUtility.ToJson(data) : "{}";
            Debug.Log($"[SocketManager] Emit → {eventName}: {json}");
            // _socket.EmitAsync(eventName, data);
        }

        // ---- Room ----
        public void CreateRoom(object settings) => Emit(ClientEvent.RoomCreate, new { settings });
        public void JoinRoom(string roomId, string password = null) =>
            Emit(ClientEvent.RoomJoin, new { roomId, password });
        public void LeaveRoom(string roomId) =>
            Emit(ClientEvent.RoomLeave, new { roomId });
        public void ReadyUp(string roomId) =>
            Emit(ClientEvent.RoomReady, new { roomId });
        public void SendRoomChat(string roomId, string message) =>
            Emit(ClientEvent.RoomChat, new { roomId, message });

        // ---- Queue ----
        public void JoinQueue(string difficulty, string region = null) =>
            Emit(ClientEvent.QueueJoin, new { difficulty, region });
        public void LeaveQueue() => Emit(ClientEvent.QueueLeave);

        // ---- Match ----
        public void SubmitConclusion(string matchId, DetectiveRoyale.Core.Models.ConclusionPayload conclusion) =>
            Emit(ClientEvent.MatchSubmitConclusion, new { matchId, conclusion });
        public void RequestHint(string matchId) =>
            Emit(ClientEvent.MatchRequestHint, new { matchId });

        // ---- Investigation ----
        public void ExamineEvidence(string matchId, string evidenceId) =>
            Emit(ClientEvent.InvestigationExamineEvidence, new { matchId, evidenceId });
        public void InterrogateWitness(string matchId, string witnessId, string message) =>
            Emit(ClientEvent.InvestigationInterrogateWitness, new { matchId, witnessId, message });
        public void AddNote(string matchId, string note, string relatedId = null) =>
            Emit(ClientEvent.InvestigationAddNote, new { matchId, note, relatedId });
        public void SendEmote(string matchId, string emoteKey) =>
            Emit(ClientEvent.InvestigationEmote, new { matchId, emote = emoteKey });

        // ---- Voice ----
        public void JoinVoice(string matchId) =>
            Emit(ClientEvent.VoiceJoin, new { matchId });
        public void LeaveVoice(string matchId) =>
            Emit(ClientEvent.VoiceLeave, new { matchId });
        public void SetVoiceMute(string matchId, bool muted) =>
            Emit(ClientEvent.VoiceMute, new { matchId, muted });
        public void SendVoiceOffer(string matchId, string targetId, string sdp) =>
            Emit(ClientEvent.VoiceOffer, new { matchId, targetId, sdp });
        public void SendVoiceAnswer(string matchId, string targetId, string sdp) =>
            Emit(ClientEvent.VoiceAnswer, new { matchId, targetId, sdp });
        public void SendIceCandidate(string matchId, string targetId, string candidate) =>
            Emit(ClientEvent.VoiceIce, new { matchId, targetId, candidate });

        // ============================================
        // Register server → client event handlers
        // ============================================

        private void RegisterHandlers()
        {
            // Uncomment when using SocketIOUnity:
            // _socket.On("room:joined",       r => Dispatch(OnRoomJoined,       r, () => r.GetValue<RoomSocketPayload>()));
            // _socket.On("room:updated",      r => Dispatch(OnRoomUpdated,      r, () => r.GetValue<RoomSocketPayload>()));
            // _socket.On("room:countdown",    r => Dispatch(OnRoomCountdown,    r, () => r.GetValue<CountdownPayload>()));
            // _socket.On("match:started",     r => Dispatch(OnMatchStarted,     r, () => r.GetValue<MatchStartedPayload>()));
            // _socket.On("match:phase_changed",r => Dispatch(OnPhaseChanged,    r, () => r.GetValue<PhasePayload>()));
            // _socket.On("match:timer",       r => Dispatch(OnTimerTick,        r, () => r.GetValue<TimerPayload>()));
            // _socket.On("match:player_submitted", r => Dispatch(OnPlayerSubmitted, r, () => r.GetValue<SubmitPayload>()));
            // _socket.On("match:ended",       r => Dispatch(OnMatchEnded,       r, () => r.GetValue<MatchEndedPayload>()));
            // _socket.On("investigation:evidence_found", r => Dispatch(OnEvidenceFound, r, () => r.GetValue<EvidenceFoundPayload>()));
            // _socket.On("investigation:hint",r => Dispatch(OnHintReceived,     r, () => r.GetValue<HintPayload>()));
            // _socket.On("npc:response",      r => Dispatch(OnNpcResponse,      r, () => r.GetValue<NPCResponsePayload>()));
            // _socket.On("error",             r => Dispatch(OnSocketError,      r, () => r.GetValue<SocketErrorPayload>()));
            // _socket.On("notification",      r => Dispatch(OnNotification,     r, () => r.GetValue<NotificationPayload>()));
        }

        private void Dispatch<T>(UnityEvent<T> ev, object raw, Func<T> parse)
        {
            try
            {
                T payload = parse();
                UnityMainThreadDispatcher.Instance.Enqueue(() => ev?.Invoke(payload));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketManager] Dispatch error: {ex.Message}");
            }
        }
    }

    // ---- Socket payload types ----

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
