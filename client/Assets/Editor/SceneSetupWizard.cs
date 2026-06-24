// ============================================
// SceneSetupWizard — validates and auto-fixes scene setups
// ============================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DetectiveRoyale.Core;
using DetectiveRoyale.UI;

namespace DetectiveRoyale.Editor
{
    public class SceneSetupWizard : EditorWindow
    {
        private Vector2 _scroll;
        private readonly List<ValidationResult> _results = new();

        [MenuItem("Detective Royale/Scene Setup Wizard")]
        public static void Open()
        {
            var win = GetWindow<SceneSetupWizard>("DR Scene Wizard");
            win.minSize = new Vector2(450, 500);
            win.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Detective Royale — Scene Setup Wizard",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            if (GUILayout.Button("Validate Current Scene", GUILayout.Height(30)))
                ValidateCurrentScene();

            if (GUILayout.Button("Auto-Fix Missing Managers", GUILayout.Height(30)))
                AutoFixManagers();

            EditorGUILayout.Space(8);
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var r in _results)
            {
                Color prev = GUI.color;
                GUI.color = r.Pass ? Color.green : Color.red;
                GUILayout.Label($"{(r.Pass ? "✓" : "✗")} {r.Message}");
                GUI.color = prev;
            }
            EditorGUILayout.EndScrollView();
        }

        // ============================================
        // Validation
        // ============================================

        private void ValidateCurrentScene()
        {
            _results.Clear();
            string sceneName = SceneManager.GetActiveScene().name;

            Check<GameBootstrapper>("GameBootstrapper");
            Check<NotificationToast>("NotificationToast");
            Check<FadeTransition>("FadeTransition");

            if (sceneName == "MainMenu" || sceneName == "Splash")
            {
                Check<SplashScreen>("SplashScreen (or LoginManager)", optional: true);
            }

            if (sceneName == "Lobby")
            {
                Check<DetectiveRoyale.Lobby.LobbyManager>("LobbyManager");
            }

            if (sceneName == "Investigation")
            {
                Check<DetectiveRoyale.Investigation.InvestigationManager>("InvestigationManager");
                Check<DetectiveRoyale.Investigation.EvidenceSystem>("EvidenceSystem");
                Check<DetectiveRoyale.NPC.NPCManager>("NPCManager");
                Check<HUD>("HUD");
                Check<InventoryUI>("InventoryUI");

                var cam = Camera.main;
                Add(cam != null, cam != null
                    ? "Main Camera found"
                    : "No Camera tagged MainCamera");
            }

            if (sceneName == "Results")
            {
                Check<ResultUI>("ResultUI");
            }

            // GameConfig asset
            var config = Resources.Load<GameConfig>("GameConfig");
            Add(config != null,
                config != null
                    ? $"GameConfig loaded (server: {config.ServerUrl})"
                    : "GameConfig asset missing — run 'Create GameConfig Asset'");

            Repaint();
        }

        private void Check<T>(string label, bool optional = false) where T : Object
        {
            var obj = FindFirstObjectByType<T>();
            if (obj != null)
                Add(true, $"{label} found");
            else if (!optional)
                Add(false, $"{label} MISSING in scene");
        }

        private void Add(bool pass, string message) =>
            _results.Add(new ValidationResult { Pass = pass, Message = message });

        // ============================================
        // Auto-fix
        // ============================================

        private void AutoFixManagers()
        {
            bool changed = false;

            if (FindFirstObjectByType<GameBootstrapper>() == null)
            {
                var go = new GameObject("GameBootstrapper");
                go.AddComponent<GameBootstrapper>();
                changed = true;
                Debug.Log("[Wizard] Created GameBootstrapper");
            }

            if (FindFirstObjectByType<NotificationToast>() == null)
            {
                var go = new GameObject("NotificationToast");
                go.AddComponent<NotificationToast>();
                changed = true;
                Debug.Log("[Wizard] Created NotificationToast");
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                ValidateCurrentScene();
            }
            else
            {
                Debug.Log("[Wizard] Nothing to fix");
            }
        }

        private class ValidationResult
        {
            public bool   Pass;
            public string Message;
        }
    }
}
#endif
