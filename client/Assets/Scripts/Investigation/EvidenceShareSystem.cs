// ============================================
// EvidenceShareSystem — share evidence with other players
// Uses /api/matches/:matchId/evidence/:evidenceId/share
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Investigation
{
    [System.Serializable]
    class ShareRequest { public string[] playerIds; }

    [System.Serializable]
    class ShareResponse { public bool success; public string message; }

    public class EvidenceShareSystem : MonoBehaviour
    {
        public static EvidenceShareSystem Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject  _panel;

        [Header("Evidence info")]
        [SerializeField] private TMP_Text    _evidenceNameText;

        [Header("Player list")]
        [SerializeField] private Transform   _playerListContent;
        [SerializeField] private GameObject  _playerTogglePrefab;

        [Header("Buttons")]
        [SerializeField] private Button      _shareBtn;
        [SerializeField] private Button      _cancelBtn;
        [SerializeField] private TMP_Text    _statusText;

        private Evidence         _currentEvidence;
        private List<string>     _selectedPlayerIds = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _panel?.SetActive(false);
        }

        // ============================================
        // Open
        // ============================================

        public void OpenForEvidence(Evidence evidence)
        {
            _currentEvidence = evidence;
            _selectedPlayerIds.Clear();
            _panel?.SetActive(true);

            if (_evidenceNameText) _evidenceNameText.text = evidence.name;
            if (_statusText)       _statusText.text        = "";

            PopulatePlayerList();
        }

        public void Close() => _panel?.SetActive(false);

        // ============================================
        // Player list
        // ============================================

        private void PopulatePlayerList()
        {
            ClearList();
            if (GameSession.Match == null) return;

            string myId = AuthState.Instance.Player?.id;
            foreach (var pid in GameSession.Match.playerIds)
            {
                if (pid == myId) continue;
                SpawnPlayerToggle(pid);
            }
        }

        private void SpawnPlayerToggle(string playerId)
        {
            if (_playerTogglePrefab == null || _playerListContent == null) return;
            var go  = Instantiate(_playerTogglePrefab, _playerListContent);
            var tgl = go.GetComponentInChildren<Toggle>();
            var txt = go.GetComponentInChildren<TMP_Text>();

            if (txt) txt.text = playerId[..System.Math.Min(8, playerId.Length)];

            if (tgl != null)
            {
                tgl.isOn = false;
                tgl.onValueChanged.AddListener(on =>
                {
                    if (on) _selectedPlayerIds.Add(playerId);
                    else    _selectedPlayerIds.Remove(playerId);
                });
            }
        }

        // ============================================
        // Share
        // ============================================

        public void OnClickShare()
        {
            if (_currentEvidence == null || _selectedPlayerIds.Count == 0)
            {
                if (_statusText) _statusText.text = "Select at least one player.";
                return;
            }

            if (_shareBtn) _shareBtn.interactable = false;
            StartCoroutine(DoShare());
        }

        private IEnumerator DoShare()
        {
            var body = new ShareRequest { playerIds = _selectedPlayerIds.ToArray() };

            yield return ApiClient.Instance.Post<ShareResponse>(
                Api.Evidence.Share(GameSession.MatchId, _currentEvidence.id),
                body,
                resp =>
                {
                    if (_statusText) _statusText.text = "✓ Shared!";
                    UI.NotificationToast.Show($"Evidence shared with {_selectedPlayerIds.Count} player(s).", "success");
                    _currentEvidence.isShared = true;
                    if (_shareBtn) _shareBtn.interactable = true;
                    StartCoroutine(CloseAfterDelay(1.5f));
                },
                err =>
                {
                    if (_statusText) _statusText.text = $"Error: {err}";
                    if (_shareBtn) _shareBtn.interactable = true;
                });
        }

        private IEnumerator CloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Close();
        }

        private void ClearList()
        {
            if (_playerListContent == null) return;
            foreach (Transform t in _playerListContent) Destroy(t.gameObject);
        }
    }
}
