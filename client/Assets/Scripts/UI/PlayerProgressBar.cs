// ============================================
// PlayerProgressBar — shows each player's investigation progress
// Displayed in HUD, updated via socket events
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class PlayerProgressBar : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private Transform  _container;
        [SerializeField] private GameObject _playerRowPrefab;

        private readonly Dictionary<string, PlayerProgressRow> _rows = new();

        void Start()
        {
            SocketManager.Instance?.OnEvidenceFound.AddListener(OnEvidenceFound);
            SocketManager.Instance?.OnPlayerSubmitted.AddListener(OnPlayerSubmitted);
            SocketManager.Instance?.OnMatchStarted.AddListener(_ => BuildRows());
        }

        void OnDestroy()
        {
            if (SocketManager.Instance == null) return;
            SocketManager.Instance.OnEvidenceFound.RemoveListener(OnEvidenceFound);
            SocketManager.Instance.OnPlayerSubmitted.RemoveListener(OnPlayerSubmitted);
        }

        // ============================================
        // Build rows for all players
        // ============================================

        private void BuildRows()
        {
            ClearRows();
            if (GameSession.Match == null) return;

            foreach (var pid in GameSession.Match.playerIds)
            {
                if (_playerRowPrefab == null || _container == null) break;
                var go   = Instantiate(_playerRowPrefab, _container);
                var row  = go.GetComponent<PlayerProgressRow>();
                if (row == null) row = go.AddComponent<PlayerProgressRow>();
                row.Init(pid, pid == AuthState.Instance.Player?.id);
                _rows[pid] = row;
            }
        }

        // ============================================
        // Socket events
        // ============================================

        private void OnEvidenceFound(EvidenceFoundPayload p)
        {
            if (_rows.TryGetValue(p.playerId, out var row))
                row.IncrementEvidence();
        }

        private void OnPlayerSubmitted(SubmitPayload p)
        {
            if (_rows.TryGetValue(p.playerId, out var row))
                row.MarkSubmitted();
        }

        private void ClearRows()
        {
            _rows.Clear();
            if (_container == null) return;
            foreach (Transform t in _container) Destroy(t.gameObject);
        }
    }

    // ---- Single row ----
    public class PlayerProgressRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _evidenceCountText;
        [SerializeField] private Image    _submitIcon;
        [SerializeField] private Image    _selfHighlight;

        private int _evidenceCount;

        public void Init(string playerId, bool isSelf)
        {
            if (_nameText) _nameText.text = isSelf ? "You" : playerId[..System.Math.Min(6, playerId.Length)];
            if (_selfHighlight) _selfHighlight.enabled = isSelf;
            if (_submitIcon)    _submitIcon.enabled    = false;
            UpdateCount();
        }

        public void IncrementEvidence()
        {
            _evidenceCount++;
            UpdateCount();
        }

        public void MarkSubmitted()
        {
            if (_submitIcon) _submitIcon.enabled = true;
            if (_nameText)   _nameText.color = new Color(0.4f, 1f, 0.4f);
        }

        private void UpdateCount()
        {
            if (_evidenceCountText) _evidenceCountText.text = $"{_evidenceCount} clues";
        }
    }
}
