// ============================================
// RoomManager (Multiplayer) — thin client-side room state cache
// The authoritative state lives on the server.
// This just caches the last known room for UI access.
// ============================================
using System.Collections;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Multiplayer
{
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        // Last known room state received from server
        public Room CurrentRoom { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            SocketManager.Instance.OnRoomJoined.AddListener(OnRoomJoined);
            SocketManager.Instance.OnRoomUpdated.AddListener(OnRoomUpdated);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnRoomJoined.RemoveListener(OnRoomJoined);
            SocketManager.Instance.OnRoomUpdated.RemoveListener(OnRoomUpdated);
        }

        // ---- Fetch current room from REST ----

        public IEnumerator FetchRoom(string roomId)
        {
            yield return ApiClient.Instance.Get<RoomResponse>($"/rooms/{roomId}",
                resp => CurrentRoom = resp.room,
                err  => Debug.LogWarning($"[RoomManager] Fetch error: {err}"));
        }

        // ---- Socket handlers ----

        private void OnRoomJoined(RoomSocketPayload p)
        {
            CurrentRoom          = p.room;
            GameSession.RoomId   = p.room.id;
            GameSession.Difficulty = p.room.difficulty;
        }

        private void OnRoomUpdated(RoomSocketPayload p)
        {
            if (p.room.id == GameSession.RoomId)
                CurrentRoom = p.room;
        }

        // ---- Response wrapper ----
        [System.Serializable] class RoomResponse { public Room room; }
    }
}
