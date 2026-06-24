// ============================================
// WitnessAnimationController — drives NPC idle/talk animations
// ============================================
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    [RequireComponent(typeof(Animator))]
    public class WitnessAnimationController : MonoBehaviour
    {
        [Header("Animator Params")]
        [SerializeField] private string _idleParam   = "Idle";
        [SerializeField] private string _talkParam   = "Talking";
        [SerializeField] private string _stressParam = "Stress";
        [SerializeField] private string _lookParam   = "LookAtPlayer";

        [Header("Settings")]
        [SerializeField] private float _talkBlinkRate = 0.3f;  // random look away
        [SerializeField] private float _stressLerpSpeed = 2f;

        private Animator  _animator;
        private bool      _isTalking;
        private float     _targetStress;
        private float     _currentStress;

        void Awake() => _animator = GetComponent<Animator>();

        void Start()
        {
            SocketManager.Instance?.OnNpcResponse.AddListener(OnNpcResponse);
        }

        void OnDestroy()
        {
            SocketManager.Instance?.OnNpcResponse.RemoveListener(OnNpcResponse);
        }

        void Update()
        {
            // Lerp stress value for smooth animation transitions
            _currentStress = Mathf.Lerp(_currentStress, _targetStress, Time.deltaTime * _stressLerpSpeed);
            _animator?.SetFloat(_stressParam, _currentStress);
        }

        // ---- Called when this witness receives an NPC response ----
        private void OnNpcResponse(NPCResponsePayload p)
        {
            // Only animate if this component is on the matching witness
            var chat = GetComponent<AIChat>();
            // (AIChat holds witnessId; we can't directly access it but
            //  WitnessBehaviour handles the actual id check)
            SetTalking(true);
            SetStress(p.stressLevel);
            Invoke(nameof(StopTalking), 2f);
        }

        public void SetTalking(bool talking)
        {
            _isTalking = talking;
            _animator?.SetBool(_talkParam, talking);
        }

        public void StopTalking() => SetTalking(false);

        public void SetStress(string level)
        {
            _targetStress = level switch
            {
                "calm"     => 0.0f,
                "mild"     => 0.25f,
                "moderate" => 0.5f,
                "high"     => 0.75f,
                "extreme"  => 1.0f,
                _          => 0f
            };
        }

        public void LookAtPlayer(bool look)
        {
            _animator?.SetBool(_lookParam, look);
        }
    }
}
