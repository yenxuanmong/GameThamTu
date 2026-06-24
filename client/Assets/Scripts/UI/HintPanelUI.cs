// ============================================
// HintPanelUI — displays received hints with history
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class HintPanelUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;

        [Header("Hint list")]
        [SerializeField] private Transform   _hintListContent;
        [SerializeField] private GameObject  _hintItemPrefab;

        [Header("Request button")]
        [SerializeField] private Button      _requestBtn;
        [SerializeField] private TMP_Text    _remainingText;
        [SerializeField] private TMP_Text    _cooldownText;

        private readonly List<string>        _hints = new();
        private float                        _cooldownRemaining;
        private bool                         _onCooldown;

        void Start()
        {
            _panel?.SetActive(false);
            SocketManager.Instance?.OnHintReceived.AddListener(OnHintReceived);
            UpdateRemainingDisplay(GameSession.HintsRemaining);
        }

        void OnDestroy()
        {
            SocketManager.Instance?.OnHintReceived.RemoveListener(OnHintReceived);
        }

        void Update()
        {
            if (!_onCooldown) return;
            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining <= 0f)
            {
                _onCooldown = false;
                if (_cooldownText) _cooldownText.gameObject.SetActive(false);
                if (_requestBtn)   _requestBtn.interactable = GameSession.HintsRemaining > 0;
            }
            else
            {
                if (_cooldownText)
                    _cooldownText.text = $"Next hint in {Mathf.CeilToInt(_cooldownRemaining)}s";
            }
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
        // Request hint
        // ============================================

        public void OnClickRequest()
        {
            if (GameSession.HintsRemaining <= 0 || _onCooldown) return;
            SocketManager.Instance.RequestHint(GameSession.MatchId);
            if (_requestBtn) _requestBtn.interactable = false;
        }

        // ============================================
        // Socket
        // ============================================

        private void OnHintReceived(HintPayload p)
        {
            _hints.Add(p.hint);
            SpawnHintItem(p.hint, _hints.Count);
            UpdateRemainingDisplay(p.hintsRemaining);

            // Start cooldown
            _cooldownRemaining = GameConfig.Instance.HintCooldownSeconds;
            _onCooldown        = true;
            if (_cooldownText) _cooldownText.gameObject.SetActive(true);
            if (_requestBtn)   _requestBtn.interactable = false;
        }

        // ============================================
        // Helpers
        // ============================================

        private void SpawnHintItem(string hint, int index)
        {
            if (_hintItemPrefab == null || _hintListContent == null) return;
            var go    = Instantiate(_hintItemPrefab, _hintListContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 1) texts[0].text = $"#{index}";
            if (texts.Length >= 2) texts[1].text = hint;
        }

        private void UpdateRemainingDisplay(int remaining)
        {
            GameSession.HintsRemaining = remaining;
            if (_remainingText)
                _remainingText.text = $"{remaining}/{GameConfig.Instance.MaxHints} hints";
            if (_requestBtn)
                _requestBtn.interactable = remaining > 0 && !_onCooldown;
        }
    }
}
