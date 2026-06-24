// ============================================
// SceneLoader — centralised scene transitions with loading screen
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DetectiveRoyale.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private UnityEngine.UI.Slider _progressBar;

        // Scene name constants
        public const string SCENE_MAIN_MENU   = "MainMenu";
        public const string SCENE_LOBBY        = "Lobby";
        public const string SCENE_INVESTIGATION= "Investigation";
        public const string SCENE_RESULTS      = "Results";

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadAsync(sceneName));
        }

        private IEnumerator LoadAsync(string sceneName)
        {
            if (_loadingScreen != null) _loadingScreen.SetActive(true);

            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                if (_progressBar != null) _progressBar.value = op.progress;
                yield return null;
            }

            if (_progressBar != null) _progressBar.value = 1f;
            yield return new WaitForSeconds(0.2f);

            op.allowSceneActivation = true;

            if (_loadingScreen != null) _loadingScreen.SetActive(false);
        }
    }
}
