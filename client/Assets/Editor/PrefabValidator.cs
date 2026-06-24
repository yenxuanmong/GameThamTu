// ============================================
// PrefabValidator — checks that all required prefabs are assigned
// ============================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace DetectiveRoyale.Editor
{
    public static class PrefabValidator
    {
        [MenuItem("Detective Royale/Validate Prefab References")]
        public static void ValidateAll()
        {
            int missing = 0;
            int total   = 0;

            // Find all MonoBehaviours in the scene with SerializedField references
            var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var mb in allObjects)
            {
                if (mb == null) continue;

                var so     = new SerializedObject(mb);
                var prop   = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.name.StartsWith("m_")) continue; // skip Unity internals

                    total++;

                    // Check for missing references (assigned but object deleted)
                    if (prop.objectReferenceValue == null &&
                        prop.objectReferenceInstanceIDValue != 0)
                    {
                        Debug.LogWarning(
                            $"[PrefabValidator] MISSING reference: {mb.GetType().Name}.{prop.name} " +
                            $"on '{mb.gameObject.name}'",
                            mb.gameObject);
                        missing++;
                    }
                }
            }

            if (missing == 0)
                Debug.Log($"[PrefabValidator] ✓ All {total} references valid");
            else
                Debug.LogError($"[PrefabValidator] ✗ {missing}/{total} MISSING references found");
        }

        [MenuItem("Detective Royale/List All Prefab Slots (Selected)")]
        public static void ListSlotsForSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[PrefabValidator] Nothing selected");
                return;
            }

            foreach (var mb in selected.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var so   = new SerializedObject(mb);
                var prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.name.StartsWith("m_")) continue;

                    string status = prop.objectReferenceValue != null ? "✓" : "○ unset";
                    Debug.Log($"  [{mb.GetType().Name}] {prop.name}: {status}");
                }
            }
        }
    }
}
#endif
