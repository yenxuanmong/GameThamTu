// ============================================
// SceneTransitionManager — wraps SceneLoader with fade transitions
// Single call point for all scene changes in the game
// ============================================
using System;
using System.Collections;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private float _defaultFadeDuration = 0.35f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ---- Navigate with optional pre-action ----

        public void GoTo(string sceneName, Action preLoad = null)
        {
            StartCoroutine(Transition(sceneName, preLoad));
        }

        public void GoToMainMenu()    => GoTo(SceneLoader.SCENE_MAIN_MENU);
        public void GoToLobby()       => GoTo(SceneLoader.SCENE_LOBBY);
        public void GoToInvestigation(string matchId, string caseId, string roomId)
        {
            GoTo(SceneLoader.SCENE_INVESTIGATION, () =>
            {
                GameSession.MatchId = matchId;
                GameSession.CaseId  = caseId;
                GameSession.RoomId  = roomId;
            });
        }
        public void GoToResults()
        {
            GoTo(SceneLoader.SCENE_RESULTS);
        }

        // ---- Core transition coroutine ----

        private IEnumerator Transition(string sceneName, Action preLoad)
        {
            // Fade out using SendMessage to avoid circular asmdef dependency
            var fadeGo = GameObject.Find("FadeTransition");
            if (fadeGo != null)
                yield return StartCoroutine(
                    (IEnumerator)fadeGo.SendMessage("FadeIn",
                        SendMessageOptions.DontRequireReceiver));

            // Pre-load action (e.g. set GameSession data)
            preLoad?.Invoke();

            // Load scene
            SceneLoader.Instance.LoadScene(sceneName);
        }
    }
}
