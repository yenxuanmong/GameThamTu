// ============================================
// SceneAudioController — plays correct BGM per scene
// Add to any persistent manager or per-scene object
// ============================================
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DetectiveRoyale.Core
{
    public class SceneAudioController : MonoBehaviour
    {
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AudioManager.Instance?.PlaySceneBGM(scene.name);
        }
    }
}
