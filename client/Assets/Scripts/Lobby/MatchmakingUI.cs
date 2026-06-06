// ============================================
// MatchmakingUI — queue spinner and status display
// Attach to the matchmaking panel object
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Lobby
{
    public class MatchmakingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _waitTimeText;
        [SerializeField] private Image    _spinnerImage;
        [SerializeField] private float    _spinSpeed = 200f;

        private float _waitSeconds;
        private bool  _searching;
        private Coroutine _timerCoroutine;

        void OnEnable()
        {
            _waitSeconds = 0;
            _searching   = true;
            SetStatus("Searching for a match...");
            _timerCoroutine = StartCoroutine(WaitTimer());
        }

        void OnDisable()
        {
            _searching = false;
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        }

        void Update()
        {
            if (_spinnerImage && _searching)
                _spinnerImage.transform.Rotate(0, 0, -_spinSpeed * Time.deltaTime);
        }

        private IEnumerator WaitTimer()
        {
            while (_searching)
            {
                yield return new WaitForSeconds(1f);
                _waitSeconds++;
                if (_waitTimeText) _waitTimeText.text = FormatTime((int)_waitSeconds);
            }
        }

        public void SetStatus(string msg)
        {
            if (_statusText) _statusText.text = msg;
        }

        public void OnMatchFound()
        {
            _searching = false;
            SetStatus("Match found! Loading...");
        }

        private static string FormatTime(int seconds)
        {
            int m = seconds / 60, s = seconds % 60;
            return $"{m:D2}:{s:D2}";
        }
    }
}
