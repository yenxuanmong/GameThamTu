// ============================================
// CameraViewer — displays CCTV/camera footage evidence
// Supports play/pause, frame scrubbing for video evidence
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class CameraViewer : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;

        [Header("Video")]
        [SerializeField] private RawImage     _videoDisplay;
        [SerializeField] private VideoPlayer  _videoPlayer;
        [SerializeField] private Button       _playPauseBtn;
        [SerializeField] private TMP_Text     _playPauseIcon;
        [SerializeField] private Slider       _scrubber;

        [Header("Info")]
        [SerializeField] private TMP_Text     _footageLabel;
        [SerializeField] private TMP_Text     _timestampText;

        private Evidence _current;
        private bool     _playing;

        public void Open(Evidence evidence)
        {
            _current = evidence;
            _panel?.SetActive(true);
            if (_footageLabel) _footageLabel.text = evidence.name;

            if (_videoPlayer != null && !string.IsNullOrEmpty(evidence.imageUrl))
            {
                _videoPlayer.url    = evidence.imageUrl;
                _videoPlayer.Pause();
                _playing = false;
                UpdatePlayIcon();
            }
        }

        public void Close()
        {
            _videoPlayer?.Pause();
            _panel?.SetActive(false);
        }

        public void OnClickPlayPause()
        {
            if (_videoPlayer == null) return;
            if (_playing) { _videoPlayer.Pause(); _playing = false; }
            else          { _videoPlayer.Play();  _playing = true;  }
            UpdatePlayIcon();
        }

        public void OnScrubberChanged(float val)
        {
            if (_videoPlayer == null) return;
            _videoPlayer.time = val * _videoPlayer.length;
        }

        void Update()
        {
            if (_videoPlayer == null || !_playing) return;

            if (_scrubber != null && _videoPlayer.length > 0)
                _scrubber.SetValueWithoutNotify((float)(_videoPlayer.time / _videoPlayer.length));

            if (_timestampText)
            {
                int s = (int)_videoPlayer.time;
                _timestampText.text = $"{s / 60:D2}:{s % 60:D2}";
            }
        }

        private void UpdatePlayIcon()
        {
            if (_playPauseIcon) _playPauseIcon.text = _playing ? "⏸" : "▶";
        }
    }
}
