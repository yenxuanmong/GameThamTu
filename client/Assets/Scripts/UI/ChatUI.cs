// ============================================
// ChatUI — room text chat panel (pre-match in room lobby)
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    [System.Serializable] class ChatMessage
    {
        public string playerId;
        public string username;
        public string message;
        public string timestamp;
    }

    public class ChatUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject     _panel;
        [SerializeField] private ScrollRect     _scrollRect;

        [Header("Messages")]
        [SerializeField] private Transform      _messageContent;
        [SerializeField] private GameObject     _myMessagePrefab;
        [SerializeField] private GameObject     _otherMessagePrefab;
        [SerializeField] private GameObject     _systemMessagePrefab;

        [Header("Input")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button         _sendBtn;
        [SerializeField] private int            _maxLength = 200;

        [Header("Counter")]
        [SerializeField] private TMP_Text       _charCount;

        private readonly Queue<ChatMessage> _history = new();
        private const int MAX_HISTORY = 100;

        void Start()
        {
            // Listen to room chat events via socket
            if (SocketManager.Instance != null)
                SocketManager.Instance.OnRoomJoined.AddListener(_ => ClearMessages());
        }

        void OnDestroy()
        {
            if (SocketManager.Instance != null)
                SocketManager.Instance.OnRoomJoined.RemoveListener(_ => ClearMessages());
        }

        void Update()
        {
            if (_inputField && _charCount)
                _charCount.text = $"{_inputField.text.Length}/{_maxLength}";
        }

        // ============================================
        // Toggle
        // ============================================

        public void Toggle()
        {
            bool on = !(_panel?.activeSelf ?? false);
            _panel?.SetActive(on);
        }

        // ============================================
        // Receive message (called from socket relay)
        // ============================================

        public void ReceiveMessage(string playerId, string username, string message)
        {
            var msg = new ChatMessage
            {
                playerId  = playerId,
                username  = username,
                message   = message,
                timestamp = System.DateTime.Now.ToString("HH:mm")
            };
            EnqueueAndDisplay(msg);
        }

        public void ReceiveSystemMessage(string message)
        {
            var msg = new ChatMessage { message = message, timestamp = System.DateTime.Now.ToString("HH:mm") };
            SpawnSystemMessage(msg);
        }

        // ============================================
        // Send message
        // ============================================

        public void OnClickSend()
        {
            string text = _inputField ? _inputField.text.Trim() : "";
            if (string.IsNullOrEmpty(text) || text.Length > _maxLength) return;

            string myId = AuthState.Instance.Player?.id ?? "";
            SocketManager.Instance.Emit("room:chat", new { roomId = GameSession.RoomId, message = text });

            // Optimistic display
            ReceiveMessage(myId, AuthState.Instance.Player?.username ?? "You", text);
            if (_inputField) _inputField.text = "";
        }

        public void OnInputSubmit(string _) => OnClickSend();

        // ============================================
        // Helpers
        // ============================================

        private void EnqueueAndDisplay(ChatMessage msg)
        {
            _history.Enqueue(msg);
            if (_history.Count > MAX_HISTORY)
            {
                _history.Dequeue();
                // Remove oldest UI element
                if (_messageContent.childCount > 0)
                    Destroy(_messageContent.GetChild(0).gameObject);
            }

            bool isMe = msg.playerId == AuthState.Instance.Player?.id;
            var prefab = isMe ? _myMessagePrefab : _otherMessagePrefab;
            SpawnBubble(prefab, msg, isMe);
        }

        private void SpawnBubble(GameObject prefab, ChatMessage msg, bool isMe)
        {
            if (prefab == null || _messageContent == null) return;
            var go    = Instantiate(prefab, _messageContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (!isMe && texts.Length >= 2)
            {
                texts[0].text = msg.username;
                texts[1].text = msg.message;
            }
            else if (texts.Length >= 1)
            {
                texts[0].text = msg.message;
            }
            ScrollToBottom();
        }

        private void SpawnSystemMessage(ChatMessage msg)
        {
            if (_systemMessagePrefab == null || _messageContent == null) return;
            var go = Instantiate(_systemMessagePrefab, _messageContent);
            var t  = go.GetComponentInChildren<TMP_Text>();
            if (t) t.text = msg.message;
            ScrollToBottom();
        }

        private void ClearMessages()
        {
            _history.Clear();
            foreach (Transform t in _messageContent) Destroy(t.gameObject);
        }

        private void ScrollToBottom()
        {
            StartCoroutine(ScrollNextFrame());
        }

        private IEnumerator ScrollNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (_scrollRect) _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
