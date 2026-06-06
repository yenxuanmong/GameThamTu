// ============================================
// SocketEvents — all Socket.IO event name constants
// Mirrors backend ClientToServerEvents & ServerToClientEvents exactly
// ============================================
namespace DetectiveRoyale.Core
{
    /// <summary>Events the CLIENT emits → SERVER</summary>
    public static class ClientEvent
    {
        // Room
        public const string RoomCreate = "room:create";
        public const string RoomJoin   = "room:join";
        public const string RoomLeave  = "room:leave";
        public const string RoomReady  = "room:ready";
        public const string RoomChat   = "room:chat";

        // Queue
        public const string QueueJoin  = "queue:join";
        public const string QueueLeave = "queue:leave";

        // Match
        public const string MatchSubmitConclusion = "match:submit_conclusion";
        public const string MatchRequestHint      = "match:request_hint";

        // Investigation
        public const string InvestigationExamineEvidence   = "investigation:examine_evidence";
        public const string InvestigationInterrogateWitness = "investigation:interrogate_witness";
        public const string InvestigationAddNote            = "investigation:add_note";
        public const string InvestigationEmote              = "investigation:emote";

        // Voice
        public const string VoiceJoin  = "voice:join";
        public const string VoiceLeave = "voice:leave";
        public const string VoiceMute  = "voice:mute";
        public const string VoiceOffer = "voice:offer";
        public const string VoiceAnswer = "voice:answer";
        public const string VoiceIce   = "voice:ice";

        // System
        public const string Ping = "ping";
    }

    /// <summary>Events the SERVER emits → CLIENT</summary>
    public static class ServerEvent
    {
        // Room
        public const string RoomJoined    = "room:joined";
        public const string RoomUpdated   = "room:updated";
        public const string RoomLeft      = "room:left";
        public const string RoomCountdown = "room:countdown";
        public const string RoomChat      = "room:chat";

        // Match
        public const string MatchStarted         = "match:started";
        public const string MatchPhaseChanged    = "match:phase_changed";
        public const string MatchTimer           = "match:timer";
        public const string MatchPlayerSubmitted = "match:player_submitted";
        public const string MatchEnded           = "match:ended";

        // Investigation
        public const string InvestigationEvidenceFound = "investigation:evidence_found";
        public const string InvestigationHint          = "investigation:hint";
        public const string InvestigationEmote         = "investigation:emote";

        // NPC
        public const string NpcResponse = "npc:response";

        // Voice
        public const string VoiceOffer  = "voice:offer";
        public const string VoiceAnswer = "voice:answer";
        public const string VoiceIce    = "voice:ice";
        public const string VoiceMuted  = "voice:muted";

        // System
        public const string Error        = "error";
        public const string Notification = "notification";
        public const string Pong         = "pong";
    }
}
