// ============================================
// ScreenshotManager — capture screenshots (F12 / share evidence)
// ============================================
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    public class ScreenshotManager : MonoBehaviour
    {
        public static ScreenshotManager Instance { get; private set; }

        [SerializeField] private KeyCode _captureKey   = KeyCode.F12;
        [SerializeField] private bool    _showFlash    = true;
        [SerializeField] private int     _supersampling = 1;

        public event Action<string> OnScreenshotSaved;   // path

        private string _saveDir;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _saveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "DetectiveRoyale");
        }

        void Update()
        {
            if (Input.GetKeyDown(_captureKey))
                StartCoroutine(Capture());
        }

        public void CaptureNow() => StartCoroutine(Capture());

        private IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();

            if (!Directory.Exists(_saveDir))
                Directory.CreateDirectory(_saveDir);

            string filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path     = Path.Combine(_saveDir, filename);

            ScreenCapture.CaptureScreenshot(path, _supersampling);
            Debug.Log($"[Screenshot] Saved: {path}");

            if (_showFlash)
                StartCoroutine(ScreenFlash());

            yield return new WaitForSeconds(0.5f);
            OnScreenshotSaved?.Invoke(path);
            // Use SendMessage to avoid circular asmdef dependency
            var toast = GameObject.Find("NotificationToast");
            if (toast != null)
                toast.SendMessage("ShowToast", "Screenshot saved!", SendMessageOptions.DontRequireReceiver);
        }

        private IEnumerator ScreenFlash()
        {
            // Simple flash effect via AudioManager or a white overlay
            // (requires a full-screen white Image in the Canvas)
            yield return null;
        }
    }
}
