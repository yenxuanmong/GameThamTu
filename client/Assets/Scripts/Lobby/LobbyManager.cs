// ============================================
// LobbyManager — orchestrates the Lobby scene
// ============================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    [Serializable] class RoomsResponse   { public Core.Room[]       rooms;   }
    [Serializable] class RoomResponse    { public Core.Room         room;    }
    [Serializable] class PlayersResponse { public Core.RoomPlayer[] players; }

    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _roomListPanel;
        [SerializeField] private GameObject _createRoomPanel;
        [SerializeField] private GameObject _roomLobbyPanel;
        [SerializeField] private GameObject _matchmakingPanel;

        [Header("Header")]
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _tierText;
        [SerializeField] private TMP_Text _pointsText;

        [Header("Room List")]
        [SerializeField] private Transform      _roomListContent;
        [SerializeField] private GameObject     _roomListItemPrefab;
        [SerializeField] private TMP_Dropdown   _difficultyFilter;
        [SerializeField] private Button         _refreshBtn;

        [Header("Create Room")]
        [SerializeField] private TMP_InputField _roomNameInput;
        [SerializeField] private TMP_Dropdown   _createDifficulty;
        [SerializeField] private Slider         _maxPlayersSlider;
        [SerializeField] private TMP_Text       _maxPlayersLabel;
        [SerializeField] private Toggle         _privateToggle;
        [SerializeField] private TMP_InputField _passwordInput;

        [Header("Room Lobby")]
        [SerializeField] private TMP_Text       _roomNameLabel;
        [SerializeField] private TMP_Text       _roomDifficultyLabel;
        [SerializeField] private Transform      _playerListContent;
        [SerializeField] private GameObject     _playerSlotPrefab;
        [SerializeField] private Button         _readyBtn;
        [SerializeField] private TMP_Text       _readyBtnText;
        [SerializeField] private Button         _leaveRoomBtn;
        [SerializeField] private TMP_Text       _countdownText;

        [Header("Matchmaking")]
        [SerializeField] private TMP_Text       _queueStatusText;
        [SerializeField] private TMP_Dropdown   _queueDifficulty;
        [SerializeField] private Button         _cancelQueueBtn;

        private string _currentRoomId;
        private bool   _isReady;
        private bool   _inQueue;

        private static readonly string[] DifficultyValues =
            { "", "easy", "medium", "hard", "expert", "nightmare" };

        // ============================================

        void Start()
        {
            Instance = this;
            UpdateHeader();
            SetupSocketListeners();
            ShowMainPanel();
        }

        void OnDestroy() => RemoveSocketListeners();

        // ============================================
        // Header
        // ============================================

        private void UpdateHeader()
        {
            var p = AuthState.Instance.Player;
            if (p == null) return;
            if (_usernameText) _usernameText.text = p.username;
            if (_tierText)     _tierText.text     = p.rank?.tier ?? "rookie";
            if (_pointsText)   _pointsText.text   = $"{p.rank?.points ?? 0} RP";
        }

        // ============================================
        // Panels
        // ============================================

        private void ShowMainPanel()
        {
            _mainPanel?.SetActive(true);
            _roomListPanel?.SetActive(false);
            _createRoomPanel?.SetActive(false);
            _roomLobbyPanel?.SetActive(false);
            _matchmakingPanel?.SetActive(false);
        }

        public void OnClickBrowseRooms()
        {
            _mainPanel?.SetActive(false);
            _roomListPanel?.SetActive(true);
            StartCoroutine(RefreshRoomList());
        }

        public void OnClickCreateRoom()
        {
            _mainPanel?.SetActive(false);
            _createRoomPanel?.SetActive(true);
        }

        public void OnClickMatchmaking()
        {
            _mainPanel?.SetActive(false);
            _matchmakingPanel?.SetActive(true);
        }

        public void OnClickBackToMain() => ShowMainPanel();

        // ============================================
        // Room List
        // ============================================

        public void OnClickRefreshRooms() => StartCoroutine(RefreshRoomList());

        private IEnumerator RefreshRoomList()
        {
            if (_refreshBtn) _refreshBtn.interactable = false;
            ClearList(_roomListContent);

            int    diffIdx = _difficultyFilter ? _difficultyFilter.value : 0;
            string diff    = (diffIdx > 0 && diffIdx < DifficultyValues.Length)
                             ? DifficultyValues[diffIdx] : "";
            string path    = string.IsNullOrEmpty(diff)
                             ? "/rooms?pageSize=30"
                             : $"/rooms?pageSize=30&difficulty={diff}";

            yield return ApiClient.Instance.Get<RoomsResponse>(path,
                resp => { foreach (var r in resp.rooms) SpawnRoomItem(r); },
                err  => Debug.LogWarning($"[Lobby] Room list: {err}"));

            if (_refreshBtn) _refreshBtn.interactable = true;
        }

        private void SpawnRoomItem(Core.Room room)
        {
            if (_roomListItemPrefab == null || _roomListContent == null) return;
            var go   = Instantiate(_roomListItemPrefab, _roomListContent);
            var item = go.GetComponent<RoomListItem>();
            item?.Setup(room, () => OnClickJoinRoom(room.id, room.hasPassword));
        }

        public void OnClickJoinRoom(string roomId, bool hasPassword)
        {
            if (hasPassword) { Debug.Log("[Lobby] Password room — show dialog"); return; }
            JoinRoom(roomId);
        }

        // ============================================
        // Create Room
        // ============================================

        public void OnMaxPlayersChanged(float val)
        {
            if (_maxPlayersLabel) _maxPlayersLabel.text = Mathf.RoundToInt(val).ToString();
        }

        public void OnPrivateToggleChanged(bool on) =>
            _passwordInput?.gameObject.SetActive(on);

        public void OnClickConfirmCreate()
        {
            string name    = _roomNameInput ? _roomNameInput.text.Trim() : "";
            int    diffIdx = _createDifficulty ? _createDifficulty.value : 1;
            string diff    = (diffIdx < DifficultyValues.Length)
                             ? DifficultyValues[Mathf.Max(1, diffIdx)] : "medium";
            int    maxP    = _maxPlayersSlider ? Mathf.RoundToInt(_maxPlayersSlider.value) : 4;
            bool   priv    = _privateToggle && _privateToggle.isOn;
            string pwd     = (priv && _passwordInput) ? _passwordInput.text : null;

            StartCoroutine(CreateRoomCoroutine(name, diff, maxP,
                priv ? "private" : "public", pwd));
        }

        private IEnumerator CreateRoomCoroutine(string name, string difficulty,
            int maxPlayers, string visibility, string password)
        {
            // Anonymous types aren't serialised by JsonUtility — use a serialisable class
            var body = new CreateRoomBody
            {
                name = name, difficulty = difficulty,
                maxPlayers = maxPlayers, visibility = visibility, password = password
            };

            yield return ApiClient.Instance.Post<RoomResponse>("/rooms", body,
                resp =>
                {
                    _currentRoomId = resp.room.id;
                    _createRoomPanel?.SetActive(false);
                    ShowRoomLobby(resp.room);
                    SocketManager.Instance.JoinRoom(resp.room.id);
                },
                err => Debug.LogWarning($"[Lobby] Create room: {err}"));
        }

        // ============================================
        // Room Lobby
        // ============================================

        private void JoinRoom(string roomId, string password = null)
        {
            _currentRoomId = roomId;
            SocketManager.Instance.JoinRoom(roomId, password);
        }

        private void ShowRoomLobby(Core.Room room)
        {
            _roomListPanel?.SetActive(false);
            _roomLobbyPanel?.SetActive(true);
            if (_roomNameLabel)       _roomNameLabel.text       = room.name;
            if (_roomDifficultyLabel) _roomDifficultyLabel.text = room.difficulty.ToUpper();
            if (_countdownText)       _countdownText.gameObject.SetActive(false);
            RefreshPlayerList();
        }

        private void RefreshPlayerList()
        {
            if (string.IsNullOrEmpty(_currentRoomId)) return;
            StartCoroutine(ApiClient.Instance.Get<PlayersResponse>(
                $"/rooms/{_currentRoomId}/players",
                resp =>
                {
                    ClearList(_playerListContent);
                    foreach (var p in resp.players) SpawnPlayerSlot(p);
                },
                err => Debug.LogWarning($"[Lobby] Players: {err}")));
        }

        private void SpawnPlayerSlot(Core.RoomPlayer p)
        {
            if (_playerSlotPrefab == null || _playerListContent == null) return;
            var go   = Instantiate(_playerSlotPrefab, _playerListContent);
            var slot = go.GetComponent<UI.PlayerSlotUI>();
            slot?.Setup(p);
        }

        public void OnClickReady()
        {
            _isReady = !_isReady;
            SocketManager.Instance.ReadyUp(_currentRoomId);
            if (_readyBtnText) _readyBtnText.text = _isReady ? "Cancel Ready" : "Ready";
        }

        public void OnClickLeaveRoom()
        {
            if (!string.IsNullOrEmpty(_currentRoomId))
                SocketManager.Instance.LeaveRoom(_currentRoomId);
            _currentRoomId = null;
            _isReady = false;
            ShowMainPanel();
        }

        // ============================================
        // Matchmaking
        // ============================================

        public void OnClickJoinQueue()
        {
            if (_inQueue) return;
            int    diffIdx = _queueDifficulty ? _queueDifficulty.value : 1;
            string diff    = (diffIdx < DifficultyValues.Length)
                             ? DifficultyValues[Mathf.Max(1, diffIdx)] : "medium";
            SocketManager.Instance.JoinQueue(diff);
            _inQueue = true;
            if (_queueStatusText) _queueStatusText.text = $"Searching for {diff} match...";
        }

        public void OnClickCancelQueue()
        {
            SocketManager.Instance.LeaveQueue();
            _inQueue = false;
            ShowMainPanel();
        }

        // ============================================
        // Socket listeners
        // ============================================

        private void SetupSocketListeners()
        {
            SocketManager.Instance.OnRoomJoined.AddListener(OnRoomJoined);
            SocketManager.Instance.OnRoomUpdated.AddListener(OnRoomUpdated);
            SocketManager.Instance.OnRoomCountdown.AddListener(OnCountdown);
            SocketManager.Instance.OnMatchStarted.AddListener(OnMatchStarted);
        }

        private void RemoveSocketListeners()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnRoomJoined.RemoveListener(OnRoomJoined);
            SocketManager.Instance.OnRoomUpdated.RemoveListener(OnRoomUpdated);
            SocketManager.Instance.OnRoomCountdown.RemoveListener(OnCountdown);
            SocketManager.Instance.OnMatchStarted.RemoveListener(OnMatchStarted);
        }

        private void OnRoomJoined(RoomSocketPayload p)
        {
            _currentRoomId = p.room.id;
            ShowRoomLobby(p.room);
        }

        private void OnRoomUpdated(RoomSocketPayload p)
        {
            if (p.room.id != _currentRoomId) return;
            if (_roomNameLabel) _roomNameLabel.text = p.room.name;
            RefreshPlayerList();
        }

        private void OnCountdown(CountdownPayload p)
        {
            if (_countdownText == null) return;
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = p.seconds > 0 ? $"Starting in {p.seconds}..." : "GO!";
        }

        private void OnMatchStarted(MatchStartedPayload p)
        {
            GameSession.MatchId = p.matchId;
            GameSession.CaseId  = p.caseId;
            SceneLoader.Instance.LoadScene(SceneLoader.SCENE_INVESTIGATION);
        }

        // ============================================
        // Helpers
        // ============================================

        private void ClearList(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform t in parent) Destroy(t.gameObject);
        }

        // Serialisable body for room creation (JsonUtility requirement)
        [Serializable]
        private class CreateRoomBody
        {
            public string name;
            public string difficulty;
            public int    maxPlayers;
            public string visibility;
            public string password;
        }
    }
}
