// ============================================
// EvidenceComparator — side-by-side evidence comparison panel
// Helps players cross-reference two pieces of evidence
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    public class EvidenceComparator : MonoBehaviour
    {
        public static EvidenceComparator Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject  _panel;

        [Header("Left slot")]
        [SerializeField] private TMP_Text    _leftName;
        [SerializeField] private TMP_Text    _leftType;
        [SerializeField] private TMP_Text    _leftDesc;
        [SerializeField] private RawImage    _leftImage;
        [SerializeField] private Button      _clearLeftBtn;

        [Header("Right slot")]
        [SerializeField] private TMP_Text    _rightName;
        [SerializeField] private TMP_Text    _rightType;
        [SerializeField] private TMP_Text    _rightDesc;
        [SerializeField] private RawImage    _rightImage;
        [SerializeField] private Button      _clearRightBtn;

        [Header("Notes")]
        [SerializeField] private TMP_InputField _comparisonNotes;
        [SerializeField] private Button         _saveNotesBtn;
        [SerializeField] private TMP_Text       _saveStatus;

        private Evidence _leftEvidence;
        private Evidence _rightEvidence;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _panel?.SetActive(false);
        }

        // ============================================
        // Open / Load
        // ============================================

        public void Open() => _panel?.SetActive(true);
        public void Close() => _panel?.SetActive(false);

        public void LoadLeft(Evidence ev)
        {
            _leftEvidence = ev;
            if (_leftName) _leftName.text = ev.name;
            if (_leftType) _leftType.text = ev.type.ToUpper();
            if (_leftDesc) _leftDesc.text = ev.description;
            if (!string.IsNullOrEmpty(ev.imageUrl))
                StartCoroutine(LoadImage(ev.imageUrl, _leftImage));
        }

        public void LoadRight(Evidence ev)
        {
            _rightEvidence = ev;
            if (_rightName) _rightName.text = ev.name;
            if (_rightType) _rightType.text = ev.type.ToUpper();
            if (_rightDesc) _rightDesc.text = ev.description;
            if (!string.IsNullOrEmpty(ev.imageUrl))
                StartCoroutine(LoadImage(ev.imageUrl, _rightImage));
        }

        // ============================================
        // Buttons
        // ============================================

        public void OnClickClearLeft()
        {
            _leftEvidence = null;
            if (_leftName)  _leftName.text  = "— None —";
            if (_leftType)  _leftType.text  = "";
            if (_leftDesc)  _leftDesc.text  = "";
            if (_leftImage) _leftImage.texture = null;
        }

        public void OnClickClearRight()
        {
            _rightEvidence = null;
            if (_rightName)  _rightName.text  = "— None —";
            if (_rightType)  _rightType.text  = "";
            if (_rightDesc)  _rightDesc.text  = "";
            if (_rightImage) _rightImage.texture = null;
        }

        public void OnClickSaveNotes()
        {
            if (_leftEvidence == null && _rightEvidence == null) return;
            string key = $"compare_{_leftEvidence?.id}_{_rightEvidence?.id}";
            PlayerPrefs.SetString(key, _comparisonNotes ? _comparisonNotes.text : "");
            PlayerPrefs.Save();
            if (_saveStatus) _saveStatus.text = "Saved ✓";
        }

        // ============================================
        // Helpers
        // ============================================

        private IEnumerator LoadImage(string url, RawImage target)
        {
            if (target == null) yield break;
            using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                target.texture = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
        }
    }
}
