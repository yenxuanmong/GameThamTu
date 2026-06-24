// ============================================
// WitnessBehaviour — visual reaction to stress events
// Attach alongside AIChat on witness GameObjects
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    public class WitnessBehaviour : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator       _animator;
        [SerializeField] private TMP_Text       _nameLabel;

        [Header("Stress Colors")]
        [SerializeField] private Color _calmColor    = Color.white;
        [SerializeField] private Color _stressedColor = Color.red;

        private string _witnessId;
        private float  _stress; // 0–1

        void Start()
        {
            var chat = GetComponent<AIChat>();
            // witnessId will be synced via AIChat
            SocketManager.Instance?.OnNpcResponse.AddListener(OnNpcResponse);
        }

        void OnDestroy()
        {
            SocketManager.Instance?.OnNpcResponse.RemoveListener(OnNpcResponse);
        }

        private void OnNpcResponse(NPCResponsePayload p)
        {
            // Map stress level text → colour
            float stress = p.stressLevel switch
            {
                "calm"     => 0.0f,
                "mild"     => 0.25f,
                "moderate" => 0.5f,
                "high"     => 0.75f,
                "extreme"  => 1.0f,
                _          => 0f
            };
            SetStress(stress);
        }

        public void SetStress(float stress)
        {
            _stress = Mathf.Clamp01(stress);
            if (_spriteRenderer)
                _spriteRenderer.color = Color.Lerp(_calmColor, _stressedColor, _stress);
            _animator?.SetFloat("stress", _stress);
        }
    }
}
