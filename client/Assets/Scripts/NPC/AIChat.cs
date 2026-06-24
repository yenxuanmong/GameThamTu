// ============================================
// AIChat — clickable witness object in the 3D/2D scene
// Attach to each witness GameObject/Sprite
// ============================================
using UnityEngine;
using DetectiveRoyale.Core;
using DetectiveRoyale.NPC;

namespace DetectiveRoyale.NPC
{
    public class AIChat : MonoBehaviour
    {
        [SerializeField] private string _witnessId;   // set in Inspector

        private Witness _witnessData;

        void Start()
        {
            // Find witness data from GameSession
            if (GameSession.Witnesses == null) return;
            foreach (var w in GameSession.Witnesses)
            {
                if (w.id == _witnessId) { _witnessData = w; break; }
            }
        }

        void OnMouseDown()
        {
            if (_witnessData != null)
                NPCManager.Instance?.OpenWitness(_witnessData);
        }

        // For UI Button use
        public void OnClickWitness()
        {
            if (_witnessData != null)
                NPCManager.Instance?.OpenWitness(_witnessData);
        }
    }
}
