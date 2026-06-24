// ============================================
// EmoteSystem — quick emote wheel for multiplayer communication
// Broadcasts emotes to all players in the match
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class EmoteSystem : MonoBehaviour
    {
        [Header("Wheel")]
        [SerializeField] private GameObject   _wheel;
        [SerializeField] private List<Button> _emoteButtons;
        [SerializeField] private List<string> _emoteKeys;   // emoji or keys like "think","wave","celebrate"

        [Header("Popup")]
        [SerializeField] private Transform    _emotePopupContainer;
        [SerializeField] private GameObject   _emotePopupPrefab;
        [SerializeField] private float        _popupDuration = 2f;

        private bool _wheelOpen;

        void Start()
        {
            _wheel?.SetActive(false);
            SocketManager.Instance?.OnNpcResponse.AddListener(_ => { }); // placeholder for extended emotes
        }

        // ============================================
        // Wheel toggle
        // ============================================

        public void ToggleWheel()
        {
            _wheelOpen = !_wheelOpen;
            _wheel?.SetActive(_wheelOpen);
        }

        public void OnClickEmote(int index)
        {
            if (index < 0 || index >= _emoteKeys.Count) return;
            string key = _emoteKeys[index];

            SocketManager.Instance?.Emit("investigation:emote", new
            {
                matchId = GameSession.MatchId,
                emote   = key
            });

            ShowPopupFor(AuthState.Instance.Player?.id, key);
            _wheelOpen = false;
            _wheel?.SetActive(false);
        }

        // ============================================
        // Receive emote from socket (called externally)
        // ============================================

        public void OnEmoteReceived(string playerId, string emoteKey)
        {
            ShowPopupFor(playerId, emoteKey);
        }

        // ============================================
        // Display popup
        // ============================================

        private void ShowPopupFor(string playerId, string emoteKey)
        {
            if (_emotePopupPrefab == null || _emotePopupContainer == null) return;
            var go  = Instantiate(_emotePopupPrefab, _emotePopupContainer);
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = EmoteKeyToDisplay(emoteKey);
            StartCoroutine(FadePopup(go, _popupDuration));
        }

        private IEnumerator FadePopup(GameObject go, float duration)
        {
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            yield return new WaitForSeconds(duration * 0.7f);
            float t = 0f;
            while (t < duration * 0.3f)
            {
                t     += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / (duration * 0.3f));
                yield return null;
            }
            Destroy(go);
        }

        private static string EmoteKeyToDisplay(string key) => key switch
        {
            "think"     => "🤔",
            "wave"      => "👋",
            "celebrate" => "🎉",
            "suspect"   => "🔍",
            "nervous"   => "😰",
            "agree"     => "👍",
            "disagree"  => "👎",
            "clue"      => "💡",
            _           => key
        };
    }
}
