// ============================================
// WitnessListUI — panel showing all available witnesses
// Lets player open dialogue from list without 3D scene click
// ============================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.NPC
{
    public class WitnessListUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject  _panel;

        [Header("List")]
        [SerializeField] private Transform   _listContent;
        [SerializeField] private GameObject  _witnessItemPrefab;

        [Header("Status")]
        [SerializeField] private TMP_Text    _countText;

        void Start() => _panel?.SetActive(false);

        public void Toggle()
        {
            bool on = !(_panel?.activeSelf ?? false);
            _panel?.SetActive(on);
            if (on) Populate();
        }

        private void Populate()
        {
            ClearList();
            if (GameSession.Witnesses == null) return;

            foreach (var w in GameSession.Witnesses)
                SpawnItem(w);

            if (_countText)
                _countText.text = $"Witnesses: {GameSession.Witnesses.Length}";
        }

        private void SpawnItem(Witness w)
        {
            if (_witnessItemPrefab == null || _listContent == null) return;
            var go    = Instantiate(_witnessItemPrefab, _listContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 1) texts[0].text = w.name;
            if (texts.Length >= 2) texts[1].text = w.occupation;
            if (texts.Length >= 3) texts[2].text = w.personality;

            var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var captured = w;
                btn.onClick.AddListener(() =>
                {
                    NPCManager.Instance?.OpenWitness(captured);
                    _panel?.SetActive(false);
                });
            }
        }

        private void ClearList()
        {
            if (_listContent == null) return;
            foreach (Transform t in _listContent) Destroy(t.gameObject);
        }
    }
}
