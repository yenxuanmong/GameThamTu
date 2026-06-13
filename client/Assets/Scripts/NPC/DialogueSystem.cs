// ============================================
// DialogueSystem — NPC conversation panel
// Attach to DialoguePanel prefab in Investigation scene.
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    public class DialogueSystem : MonoBehaviour
    {
        // ---- Witness info ----
        [Header("Witness Header")]
        [SerializeField] private TMP_Text _witnessName;
        [SerializeField] private TMP_Text _witnessOccupation;
        [SerializeField] private TMP_Text _witnessPersonality;
        [SerializeField] private TMP_Text _stressLabel;
        [SerializeField] private Image    _stressBar;          // fill image 0-1

        // ---- Chat log ----
        [Header("Chat")]
        [SerializeField] private Transform      _chatContent;
        [SerializeField] private GameObject     _playerBubblePrefab;
        [SerializeField] private GameObject     _npcBubblePrefab;
        [SerializeField] private ScrollRect     _scrollRect;

        // ---- Input ----
        [Header("Input")]
        [SerializeField] private TMP_InputField _messageInput;
        [SerializeField] private Button         _sendBtn;
        [SerializeField] private TMP_Text       _charCountText;
        [SerializeField] private int            _maxChars = 500;

        // ---- Footer ----
        [Header("Footer")]
        [SerializeField] private Button     _closeBtn;
        [SerializeField] private GameObject _typingIndicator;   // "Witness is thinking..."
        [SerializeField] private TMP_Text   _waitingText;

        // ---- Panels ----
        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;

        private Witness _witness;
        private bool    _waiting;

        private static readonly System.Collections.Generic.Dictionary<string, float> StressValues =
            new System.Collections.Generic.Dictionary<string, float>
        {
            { "calm",      0.0f },
            { "uneasy",    0.25f },
            { "nervous",   0.50f },
            { "stressed",  0.75f },
            { "panicking", 1.0f },
        };

        // ============================================
        // Lifecycle
        // ============================================

        void Awake()
        {
            _closeBtn?.onClick.AddListener(Close);
            _sendBtn?.onClick.AddListener(OnClickSend);

            if (_messageInput != null)
            {
                _messageInput.onValueChanged.AddListener(_ => OnInputChanged());
                _messageInput.onSubmit.AddListener(_ => OnClickSend());
            }
        }

        void Start()
        {
            SocketManager.Instance.OnNpcResponse.AddListener(OnNPCResponse);
            SetWaiting(false);
        }

        void OnDestroy()
        {
            if (SocketManager.Instance != null)
                SocketManager.Instance.OnNpcResponse.RemoveListener(OnNPCResponse);
        }

        void Update()
        {
            if (_messageInput != null && _charCountText != null)
                _charCountText.text = $"{_messageInput.text.Length}/{_maxChars}";
        }

        // ============================================
        // Open / Close
        // ============================================

        public void Open(Witness witness)
        {
            _witness = witness;
            gameObject.SetActive(true);

            // Populate header
            if (_witnessName)        _witnessName.text        = witness.name;
            if (_witnessOccupation)  _witnessOccupation.text  = witness.occupation;
            if (_witnessPersonality) _witnessPersonality.text = witness.personality;
            if (_stressLabel)        _stressLabel.text        = "Calm";
            if (_stressBar)          _stressBar.fillAmount    = 0f;

            // Clear chat and show opening line
            ClearChat();
            AddNPCBubble($"{witness.name} looks at you cautiously. What would you like to know?");

            // Fade in
            if (_canvasGroup != null)
                StartCoroutine(Fade(_canvasGroup, 0f, 1f, 0.25f));
        }

        public void Close()
        {
            if (_canvasGroup != null)
                StartCoroutine(FadeAndDestroy());
            else
                Destroy(gameObject);
        }

        private IEnumerator FadeAndDestroy()
        {
            yield return Fade(_canvasGroup, 1f, 0f, 0.2f);
            Destroy(gameObject);
        }

        // ============================================
        // Sending messages
        // ============================================

        public void OnClickSend()
        {
            if (_waiting) return;

            string msg = _messageInput != null ? _messageInput.text.Trim() : "";
            if (string.IsNullOrEmpty(msg)) return;

            if (msg.Length > _maxChars)
            {
                AddSystemBubble($"Message too long (max {_maxChars} chars).");
                return;
            }

            AddPlayerBubble(msg);
            if (_messageInput != null) _messageInput.text = "";

            SetWaiting(true);
            SocketManager.Instance.InterrogateWitness(
                GameSession.MatchId, _witness.id, msg);
        }

        private void OnInputChanged()
        {
            bool hasText = _messageInput != null && _messageInput.text.Trim().Length > 0;
            if (_sendBtn) _sendBtn.interactable = !_waiting && hasText;
        }

        // ============================================
        // Receiving NPC response
        // ============================================

        private void OnNPCResponse(NPCResponsePayload p)
        {
            if (_witness == null || p.witnessId != _witness.id) return;

            SetWaiting(false);
            AddNPCBubble(p.message);
            UpdateStress(p.stressLevel);
        }

        // ============================================
        // Stress display
        // ============================================

        private void UpdateStress(string stressLevel)
        {
            if (_stressLabel) _stressLabel.text = stressLevel;

            if (_stressBar && StressValues.TryGetValue(stressLevel.ToLower(), out float fill))
            {
                // Animate bar
                StartCoroutine(AnimateBar(_stressBar, _stressBar.fillAmount, fill, 0.4f));

                // Color: green → yellow → red
                _stressBar.color = Color.Lerp(Color.green, Color.red, fill);
            }
        }

        private IEnumerator AnimateBar(Image bar, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                bar.fillAmount = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            bar.fillAmount = to;
        }

        // ============================================
        // Chat bubbles
        // ============================================

        private void AddPlayerBubble(string text)
        {
            SpawnBubble(_playerBubblePrefab, text);
            ScrollToBottom();
        }

        private void AddNPCBubble(string text)
        {
            SpawnBubble(_npcBubblePrefab, text);
            ScrollToBottom();
        }

        private void AddSystemBubble(string text)
        {
            if (_chatContent == null) return;
            var go = new GameObject("SystemMsg");
            go.transform.SetParent(_chatContent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = 12f;
            tmp.color     = new Color(0.7f, 0.7f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center;
            ScrollToBottom();
        }

        private void SpawnBubble(GameObject prefab, string text)
        {
            if (prefab == null || _chatContent == null) return;
            var go  = Instantiate(prefab, _chatContent);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp) tmp.text = text;
        }

        private void ClearChat()
        {
            if (_chatContent == null) return;
            foreach (Transform t in _chatContent) Destroy(t.gameObject);
        }

        private void ScrollToBottom()
        {
            if (_scrollRect == null) return;
            StartCoroutine(ScrollNextFrame());
        }

        private IEnumerator ScrollNextFrame()
        {
            yield return null;   // wait one frame for layout rebuild
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        // ============================================
        // Waiting state
        // ============================================

        private void SetWaiting(bool on)
        {
            _waiting = on;
            if (_typingIndicator) _typingIndicator.SetActive(on);
            if (_sendBtn)         _sendBtn.interactable = !on;
            if (on) ScrollToBottom();
        }

        // ============================================
        // Helpers
        // ============================================

        private IEnumerator Fade(CanvasGroup cg, float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / time);
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
