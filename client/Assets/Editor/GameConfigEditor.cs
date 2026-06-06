// ============================================
// GameConfigEditor — Unity Editor menu items
// ============================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Editor
{
    public static class GameConfigEditor
    {
        [MenuItem("Detective Royale/Create GameConfig Asset")]
        public static void CreateGameConfigAsset()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            string path = "Assets/Resources/GameConfig.asset";

            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (existing != null)
            {
                Debug.Log("[GameConfig] Asset already exists at: " + path);
                Selection.activeObject = existing;
                return;
            }

            var asset = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            Debug.Log("[GameConfig] Created GameConfig asset at: " + path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("Detective Royale/Open Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/your-repo/DetectiveRoyale/wiki");
        }

        [MenuItem("Detective Royale/Validate Scene Setup")]
        public static void ValidateSceneSetup()
        {
            var bootstrapper = Object.FindFirstObjectByType<GameBootstrapper>();
            if (bootstrapper == null)
                Debug.LogWarning("[Validate] No GameBootstrapper found in scene. Add one to your first scene.");
            else
                Debug.Log("[Validate] ✓ GameBootstrapper found: " + bootstrapper.gameObject.name);

            var config = Resources.Load<GameConfig>("GameConfig");
            if (config == null)
                Debug.LogWarning("[Validate] No GameConfig asset found in Resources. Use Detective Royale → Create GameConfig Asset.");
            else
                Debug.Log($"[Validate] ✓ GameConfig loaded. Server: {config.ServerUrl}");
        }
    }
}
#endif
