// ============================================
// ResultUI — post-match results screen
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    [System.Serializable] class ScoresResponse   { public MatchScore[]  scores;   public string winnerId; }
    [System.Serializable] class SolutionResponse { public CaseSolution  solution; }

    public class ResultUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text     _matchResultText;   // "SOLVED" / "UNSOLVED"
        [SerializeField] private TMP_Text     _winnerNameText;
        [SerializeField] private GameObject   _victoryBanner;
        [SerializeField] private GameObject   _defeatBanner;

        [Header("Scores list")]
        [SerializeField] private Transform    _scoreListContent;
        [SerializeField] private GameObject   _scoreItemPrefab;

        [Header("Solution reveal")]
        [SerializeField] private TMP_Text     _killerText;
        [SerializeField] private TMP_Text     _motiveText;
        [SerializeField] private TMP_Text     _weaponText;
        [SerializeField] private TMP_Text     _locationText;
        [SerializeField] private TMP_Text     _methodText;
        [SerializeField] private TMP_Text     _narrativeText;

        [Header("My result")]
        [SerializeField] private TMP_Text     _myScoreText;
        [SerializeField] private TMP_Text     _myRankText;
        [SerializeField] private TMP_Text     _rpChangeText;
        [SerializeField] private Image        _correctBadge;
        [SerializeField] private Image        _incorrectBadge;

        [Header("Buttons")]
        [SerializeField] private Button       _playAgainBtn;
        [SerializeField] private Button       _mainMenuBtn;

        [Header("Loading")]
        [SerializeField] private GameObject   _loadingPanel;

        void Start()
        {
            StartCoroutine(LoadResults());
        }

        private IEnumerator LoadResults()
        {
            _loadingPanel?.SetActive(true);

            // Fetch scores
            yield return ApiClient.Instance.Get<ScoresResponse>(
                $"/matches/{GameSession.MatchId}/scores",
                resp => DisplayScores(resp.scores, resp.winnerId),
                err  => Debug.LogError($"[ResultUI] Scores error: {err}"));

            // Fetch solution
            yield return ApiClient.Instance.Get<SolutionResponse>(
                $"/cases/{GameSession.CaseId}/solution?matchId={GameSession.MatchId}",
                resp => DisplaySolution(resp.solution),
                err  => Debug.LogWarning($"[ResultUI] Solution: {err}"));

            _loadingPanel?.SetActive(false);
        }

        // ============================================
        // Display scores
        // ============================================

        private void DisplayScores(MatchScore[] scores, string winnerId)
        {
            if (scores == null) return;

            string myId = AuthState.Instance.Player?.id;
            bool iWon   = winnerId == myId;

            _victoryBanner?.SetActive(iWon);
            _defeatBanner?.SetActive(!iWon);
            if (_matchResultText) _matchResultText.text = iWon ? "CASE SOLVED" : "CASE CLOSED";

            // Find winner name
            foreach (var s in scores)
                if (s.playerId == winnerId && _winnerNameText)
                    _winnerNameText.text = $"🏆 {s.username}";

            // Populate score list
            foreach (Transform t in _scoreListContent) Destroy(t.gameObject);
            foreach (var s in scores)
                SpawnScoreItem(s);

            // My result
            foreach (var s in scores)
            {
                if (s.playerId != myId) continue;
                if (_myScoreText)   _myScoreText.text   = $"{s.totalScore} pts";
                if (_myRankText)    _myRankText.text     = $"#{s.rank}";
                if (_rpChangeText)
                {
                    int rp = s.rpChange;
                    _rpChangeText.text  = rp >= 0 ? $"+{rp} RP" : $"{rp} RP";
                    _rpChangeText.color = rp >= 0 ? Color.green : Color.red;
                }
                _correctBadge?.gameObject.SetActive(s.isCorrect);
                _incorrectBadge?.gameObject.SetActive(!s.isCorrect);
                break;
            }
        }

        private void SpawnScoreItem(MatchScore score)
        {
            if (_scoreItemPrefab == null || _scoreListContent == null) return;
            var go = Instantiate(_scoreItemPrefab, _scoreListContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 4)
            {
                texts[0].text = $"#{score.rank}";
                texts[1].text = score.username;
                texts[2].text = $"{score.totalScore}";
                texts[3].text = score.rpChange >= 0 ? $"+{score.rpChange}" : $"{score.rpChange}";
                texts[3].color = score.rpChange >= 0 ? Color.green : Color.red;
            }
        }

        // ============================================
        // Display solution
        // ============================================

        private void DisplaySolution(CaseSolution sol)
        {
            if (sol == null) return;
            if (_killerText)   _killerText.text   = $"Killer: {sol.killerName}";
            if (_motiveText)   _motiveText.text   = $"Motive: {sol.motive?.Replace("_", " ")}";
            if (_weaponText)   _weaponText.text   = $"Weapon: {sol.weapon?.Replace("_", " ")}";
            if (_locationText) _locationText.text = $"Location: {sol.location?.Replace("_", " ")}";
            if (_methodText)   _methodText.text   = sol.method;
            if (_narrativeText)_narrativeText.text= sol.narrative;
        }

        // ============================================
        // Buttons
        // ============================================

        public void OnClickPlayAgain()
        {
            GameSession.Reset();
            SceneLoader.Instance.LoadScene(SceneLoader.SCENE_LOBBY);
        }

        public void OnClickMainMenu()
        {
            GameSession.Reset();
            SceneLoader.Instance.LoadScene(SceneLoader.SCENE_MAIN_MENU);
        }
    }
}
