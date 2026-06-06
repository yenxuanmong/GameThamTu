// ============================================
// NPCManager — tracks all witnesses, opens dialogue sessions
// ============================================
using System.Collections.Generic;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        // Active dialogue (only one open at a time)
        private DialogueSystem _activeDialogue;

        [SerializeField] private DialogueSystem _dialoguePrefab;
        [SerializeField] private Transform      _dialogueContainer;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ---- Open dialogue for a witness ----

        public void OpenWitness(Witness witness)
        {
            if (_activeDialogue != null)
                _activeDialogue.Close();

            if (_dialoguePrefab == null)
            {
                Debug.LogWarning("[NPCManager] No DialogueSystem prefab set");
                return;
            }

            var go = Instantiate(_dialoguePrefab.gameObject, _dialogueContainer);
            _activeDialogue = go.GetComponent<DialogueSystem>();
            _activeDialogue.Open(witness);
        }

        public void CloseActiveDialogue()
        {
            _activeDialogue?.Close();
            _activeDialogue = null;
        }
    }
}
