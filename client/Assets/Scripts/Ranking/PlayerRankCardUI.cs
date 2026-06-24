// ============================================
// PlayerRankCardUI — shows another player's rank card
// Used in Lobby player list or profile lookup
// Uses /api/ranking/player/:playerId
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Ranking
{
    [System.Serializable] class PlayerRankResponse { public PlayerRankInfo rank; }

    public class PlayerRankCardUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Info")]
        [SerializeField] private TMP_Text   _usernameText;
        [SerializeField] private TMP_Text   _tierText;
        [SerializeField] private TMP_Text   _pointsText;
        [SerializeField] private TMP_Text   _rankText;
        [SerializeField] private TMP_Text   _winsText;
        [SerializeField] private TMP_Text   _winRateText;
        [SerializeField] private TMP_Text   _streakText;
        [SerializeField] private Image      _tierBadge;

        [Header("Tier Colors")]
        [SerializeField] private Color      _rookieColor      = Color.grey;
        [SerializeField] private Color      _detectiveColor   = Color.cyan;
        [SerializeField] private Color      _inspectorColor   = Color.green;
        [SerializeField] private Color      _sergeantColor    = new Color(0f, 0.5f, 1f);
        [SerializeField] private Color      _lieutenantColor  = new Color(0.6f, 0f, 1f);
        [SerializeField] private Color      _captainColor     = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color      _commissionerColor= new Color(1f, 0.85f, 0f);

        void Start() => _panel?.SetActive(false);

        public void ShowForPlayer(string playerId)
        {
            _panel?.SetActive(true);
            StartCoroutine(Load(playerId));
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator Load(string playerId)
        {
            yield return ApiClient.Instance.Get<PlayerRankResponse>(
                Api.Ranking.PlayerRank(playerId),
                resp => Populate(resp.rank),
                err  => Debug.LogWarning($"[PlayerRankCard] {err}"));
        }

        private void Populate(PlayerRankInfo r)
        {
            if (r == null) return;
            if (_usernameText) _usernameText.text = r.username;
            if (_tierText)     _tierText.text     = r.tier.ToUpper();
            if (_pointsText)   _pointsText.text   = $"{r.points} RP";
            if (_rankText)     _rankText.text      = $"#{r.globalRank}";
            if (_winsText)     _winsText.text      = $"{r.wins}W / {r.losses}L";
            if (_winRateText)  _winRateText.text   = $"{r.winRate * 100:F0}% WR";

            if (_streakText)
            {
                int s = r.streak;
                _streakText.text  = s > 0 ? $"🔥 {s}W" : s < 0 ? $"❄ {-s}L" : "—";
                _streakText.color = s > 0 ? Color.green : s < 0 ? new Color(0.4f, 0.8f, 1f) : Color.white;
            }

            if (_tierBadge) _tierBadge.color = TierColor(r.tier);
        }

        private Color TierColor(string tier) => tier?.ToLower() switch
        {
            "rookie"       => _rookieColor,
            "detective"    => _detectiveColor,
            "inspector"    => _inspectorColor,
            "sergeant"     => _sergeantColor,
            "lieutenant"   => _lieutenantColor,
            "captain"      => _captainColor,
            "commissioner" => _commissionerColor,
            _              => Color.white
        };
    }
}
