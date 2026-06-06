// ============================================
// DialogueSystem — NPC conversation panel
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    public class DialogueSystem : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _witnessName;
        [SerializeField] private TMP_Text _witnessTrait;
        [SerializeField] private TMP_Text _stressLabel;

        [Header("Chat log")]
        [SerializeField] private Transform _chatContent;
        [SerializeField] private GameObject _playerBubblePrefab;
        [SerializeField] private GameObject _npcBubblePrefab;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Input")]
        [SerializeField] private TMP_InputField _messageInput;
        [SerializeField] private Button         _sendBtn;
        [SerializeField] private TMP_Text       _charCountText;
        [SerializeField] private int            _maxChars = 500;

        [Header("Footer")]
        [SerializeField] private Button    _closeBtn;
        [SerializeField] private TMP_Text  _waitingIndicator;

        private Witness _witness;
        private bool    _waiting;

        void Start()
        {
            SocketManager.Instance.OnNpcResponse.AddListener(OnNPCResponse);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance != null)
                SocketManager.Instance.OnNpcResponse.RemoveListener(OnNPCResponse);
        }

        public void Open(Witness witness)
        {
            _witness = witness;
            gameObject.SetActive(true);

            if (_witnessName)  _witnessName.text  = witness.name;
            if (_witnessTrait) _witnessTrait.text = $"{witness.occupation} · {witness.personality}";
            if (_stressLabel)  _stressLabel.text  = "Calm";

            AddNPCBubble($"{witness.name} looks up at you. Ask me anything.");
        }

        public void Close()
        {
            SocketManager.Instance.OnNpcResponse.RemoveListener(OnNPCResponse);
            Destroy(gameObject);
        }

        // ============================================
        // Sending a message
        // ============================================

        void Update()
        {
            if (_messageInput && _charCountText)
                _charCountText.text = $"{_messageInput.text.Length}/{_maxChars}";
        }

        public void OnClickSend()
        {
            if (_waiting) return;
            string msg = _messageInput ? _messageInput.text.Trim() : "";
            if (string.IsNullOrEmpty(msg) || msg.Length > _maxChars) return;

            AddPlayerBubble(msg);
            if (_messageInput) _messageInput.text = "";

            SetWaiting(true);
            SocketManager.Instance.InterrogateWitness(GameSession.MatchId, _witness.id, msg);
        }

        public void OnMessageInputChanged()
        {
            string txt = _messageInput ? _messageInput.text : "";
            if (_sendBtn) _sendBtn.interactable = !_waiting && txt.Trim().Length > 0;
        }

        // ---- Submit on Enter ----
        public void OnMessageSubmit(string _) => OnClickSend();

        // ============================================
        // Receiving NPC response via socket
        // ============================================

        private void OnNPCResponse(NPCResponsePayload p)
        {
            if (_witness == null || p.witnessId != _witness.id) return;
            SetWaiting(false);
            AddNPCBubble(p.message);
            if (_stressLabel) _stressLabel.text = p.stressLevel;
        }

        // ============================================
        // Chat bubbles
        // ============================================

        private void AddPlayerBubble(string text)
        {
            if (_playerBubblePrefab == null || _chatContent == null) return;
            var go = Instantiate(_playerBubblePrefab, _chatContent);
            go.GetComponentInChildren<TMP_Text>().text = text;
            ScrollToBottom();
        }

        private void AddNPCBubble(string text)
        {
            if (_npcBubblePrefab == null || _chatContent == null) return;
            var go = Instantiate(_npcBubblePrefab, _chatContent);
            go.GetComponentInChildren<TMP_Text>().text = text;
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (_scrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void SetWaiting(bool on)
        {
            _waiting = on;
            if (_waitingIndicator) _waitingIndicator.gameObject.SetActive(on);
            if (_sendBtn)          _sendBtn.interactable = !on;
        }
    }
}
