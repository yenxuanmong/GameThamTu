// ============================================
// LoadingScreen — animated loading overlay
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [SerializeField] private GameObject _panel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private TMP_Text   _messageText;
        [SerializeField] private Image      _spinnerImage;
        [SerializeField] private float      _spinSpeed = 180f;

        private static readonly string[] LoadingMessages = {
            "Examining the evidence...",
            "Interviewing witnesses...",
            "Reviewing the case files...",
            "Reconstructing the timeline...",
            "Analysing forensic data...",
            "Following the clues..."
        };

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _panel?.SetActive(false);
        }

        void Update()
        {
            if (_spinnerImage && (_panel?.activeSelf ?? false))
                _spinnerImage.transform.Rotate(0, 0, -_spinSpeed * Time.deltaTime);
        }

        public void Show(string message = null)
        {
            _panel?.SetActive(true);
            if (_messageText) _messageText.text = message ?? RandomMessage();
            if (_progressBar) _progressBar.value = 0;
        }

        public void SetProgress(float progress)
        {
            if (_progressBar) _progressBar.value = progress;
        }

        public void SetMessage(string message)
        {
            if (_messageText) _messageText.text = message;
        }

        public void Hide()
        {
            _panel?.SetActive(false);
        }

        public IEnumerator ShowForSeconds(float seconds, string message = null)
        {
            Show(message);
            yield return new WaitForSeconds(seconds);
            Hide();
        }

        private static string RandomMessage() =>
            LoadingMessages[Random.Range(0, LoadingMessages.Length)];
    }
}
