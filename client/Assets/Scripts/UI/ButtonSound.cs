// ============================================
// ButtonSound — plays click SFX on any Button press
// Attach to Button GameObjects or parent canvas
// ============================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DetectiveRoyale.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] private bool _playOnHover = false;
        [SerializeField] private bool _playOnClick = true;

        private Button _button;

        void Awake() => _button = GetComponent<Button>();

        public void OnPointerClick(PointerEventData _)
        {
            if (!_playOnClick) return;
            if (_button != null && !_button.interactable) return;
            Core.AudioManager.Instance?.PlayClick();
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_playOnHover) return;
            if (_button != null && !_button.interactable) return;
            // Play hover sound — lighter than click
            Core.AudioManager.Instance?.PlayClick();
        }
    }

    /// <summary>
    /// Bulk-add ButtonSound to every Button in a Canvas.
    /// Attach to Canvas root.
    /// </summary>
    public class CanvasButtonSounds : MonoBehaviour
    {
        void Start()
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn.GetComponent<ButtonSound>() == null)
                    btn.gameObject.AddComponent<ButtonSound>();
            }
        }
    }
}
