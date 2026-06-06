// ============================================
// MiniMapUI — crime scene minimap / floor plan
// Shows player position and evidence icons
// ============================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;
using DetectiveRoyale.Investigation;

namespace DetectiveRoyale.UI
{
    public class MiniMapUI : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private RawImage    _mapImage;
        [SerializeField] private RectTransform _mapRect;

        [Header("Icons")]
        [SerializeField] private GameObject  _evidenceIconPrefab;
        [SerializeField] private GameObject  _witnessIconPrefab;
        [SerializeField] private GameObject  _playerIconPrefab;
        [SerializeField] private Color       _discoveredColor = Color.green;
        [SerializeField] private Color       _undiscoveredColor = Color.grey;

        [Header("Scale")]
        [SerializeField] private Vector2 _worldBoundsMin = new Vector2(-10, -10);
        [SerializeField] private Vector2 _worldBoundsMax = new Vector2(10,  10);

        private readonly Dictionary<string, RectTransform> _icons = new();
        private RectTransform _playerIcon;

        void Start()
        {
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded += OnEvidenceDiscovered;

            SpawnPlayerIcon();
        }

        void OnDestroy()
        {
            if (EvidenceSystem.Instance != null)
                EvidenceSystem.Instance.OnEvidenceAdded -= OnEvidenceDiscovered;
        }

        void Update()
        {
            UpdatePlayerIcon();
        }

        // ============================================
        // Player icon
        // ============================================

        private void SpawnPlayerIcon()
        {
            if (_playerIconPrefab == null || _mapRect == null) return;
            var go = Instantiate(_playerIconPrefab, _mapRect);
            _playerIcon = go.GetComponent<RectTransform>();
        }

        private void UpdatePlayerIcon()
        {
            if (_playerIcon == null) return;
            // Convert world space → map UV → map rect
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 wp = cam.transform.position;
            _playerIcon.anchoredPosition = WorldToMap(new Vector2(wp.x, wp.z));
        }

        // ============================================
        // Evidence icons
        // ============================================

        public void AddEvidenceMarker(string evidenceId, Vector2 worldPos)
        {
            if (_evidenceIconPrefab == null || _mapRect == null || _icons.ContainsKey(evidenceId)) return;
            var go  = Instantiate(_evidenceIconPrefab, _mapRect);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchoredPosition = WorldToMap(worldPos);
            _icons[evidenceId]  = rt;
        }

        private void OnEvidenceDiscovered(Evidence ev)
        {
            // Mark as discovered (green)
            if (_icons.TryGetValue(ev.id, out var icon))
            {
                var img = icon.GetComponent<Image>();
                if (img) img.color = _discoveredColor;
            }
        }

        // ============================================
        // Coordinate conversion
        // ============================================

        private Vector2 WorldToMap(Vector2 worldPos)
        {
            if (_mapRect == null) return Vector2.zero;
            float u = Mathf.InverseLerp(_worldBoundsMin.x, _worldBoundsMax.x, worldPos.x);
            float v = Mathf.InverseLerp(_worldBoundsMin.y, _worldBoundsMax.y, worldPos.y);

            Vector2 size = _mapRect.rect.size;
            return new Vector2((u - 0.5f) * size.x, (v - 0.5f) * size.y);
        }
    }
}
