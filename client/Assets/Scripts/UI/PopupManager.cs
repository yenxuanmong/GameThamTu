// ============================================
// PopupManager — manages modal popup stack
// Prevents multiple popups from overlapping
// ============================================
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveRoyale.UI
{
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        private readonly Stack<GameObject> _stack = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // Escape closes top popup
            if (Input.GetKeyDown(KeyCode.Escape) && _stack.Count > 0)
                CloseTop();
        }

        // ---- Open a popup ----
        public void Open(GameObject popup)
        {
            if (popup == null) return;
            // Disable current top without destroying
            if (_stack.Count > 0 && _stack.Peek() != null)
                _stack.Peek().SetActive(false);

            popup.SetActive(true);
            _stack.Push(popup);
        }

        // ---- Close top popup ----
        public void CloseTop()
        {
            if (_stack.Count == 0) return;
            var top = _stack.Pop();
            top?.SetActive(false);

            // Re-enable the one below
            if (_stack.Count > 0 && _stack.Peek() != null)
                _stack.Peek().SetActive(true);
        }

        // ---- Close a specific popup ----
        public void Close(GameObject popup)
        {
            if (popup == null) return;
            popup.SetActive(false);

            var temp = new Stack<GameObject>();
            while (_stack.Count > 0)
            {
                var item = _stack.Pop();
                if (item != popup) temp.Push(item);
            }
            while (temp.Count > 0)
                _stack.Push(temp.Pop());
        }

        // ---- Close all ----
        public void CloseAll()
        {
            while (_stack.Count > 0)
                _stack.Pop()?.SetActive(false);
        }

        public bool HasOpenPopup => _stack.Count > 0;
        public int  Count        => _stack.Count;
    }
}
