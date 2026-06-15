// ============================================
// AchievementUI — achievement system panel
// Loads achievements from /api/auth/me/achievements
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public string category;
        public bool   unlocked;
        public string unlockedAt;
        public int    progress;
        public int    maxProgress;
        public string iconKey;
    }

    [System.Serializable]
    class AchievementsResponse { public Achievement[] achievements; }

    public class AchievementUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject   _panel;
        [SerializeField] private GameObject   _loadingOverlay;

        [Header("Filter tabs")]
        [SerializeField] private Button       _tabAll;
        [SerializeField] private Button       _tabUnlocked;
        [SerializeField] private Button       _tabLocked;

        [Header("List")]
        [SerializeField] private Transform    _listContent;
        [SerializeField] private GameObject   _itemPrefab;

        [Header("Stats")]
        [SerializeField] private TMP_Text     _unlockedCount;
        [SerializeField] private Slider       _progressBar;

        private Achievement[] _all;
        private string        _filter = "all";

        void Start()
        {
            _panel?.SetActive(false);
            // Listen for real-time achievement unlocks via socket
            SocketManager.Instance?.OnAchievementUnlocked.AddListener(OnAchievementUnlocked);
        }

        void OnDestroy()
        {
            SocketManager.Instance?.OnAchievementUnlocked.RemoveListener(OnAchievementUnlocked);
        }

        private void OnAchievementUnlocked(AchievementPayload p)
        {
            // Show toast immediately
            NotificationToast.Show($"\ud83c\udfc6 Achievement unlocked: {p.title}", "success", 5f);
            // Refresh list if panel is open
            if (_panel != null && _panel.activeSelf)
                StartCoroutine(Load());
        }

        // ============================================
        // Open
        // ============================================

        public void Open()
        {
            _panel?.SetActive(true);
            StartCoroutine(Load());
        }

        public void Close() => _panel?.SetActive(false);

        private IEnumerator Load()
        {
            _loadingOverlay?.SetActive(true);

            // Try cache first
            string cached = SaveManager.LoadAchievements();
            if (!string.IsNullOrEmpty(cached))
            {
                try
                {
                    _all = JsonUtility.FromJson<AchievementsResponse>(cached).achievements;
                    Render();
                }
                catch { /* ignore, fetch fresh */ }
            }

            yield return ApiClient.Instance.Get<AchievementsResponse>(
                "/auth/me/achievements",
                resp =>
                {
                    _all = resp.achievements;
                    SaveManager.SaveAchievements(JsonUtility.ToJson(resp));
                    Render();
                },
                err => Debug.LogWarning($"[Achievement] {err}"));

            _loadingOverlay?.SetActive(false);
        }

        // ============================================
        // Render
        // ============================================

        private void Render()
        {
            if (_all == null) return;

            ClearList();
            int unlocked = 0;
            foreach (var a in _all)
            {
                if (a.unlocked) unlocked++;
                if (_filter == "unlocked" && !a.unlocked) continue;
                if (_filter == "locked"   &&  a.unlocked) continue;
                SpawnItem(a);
            }

            if (_unlockedCount)
                _unlockedCount.text = $"{unlocked} / {_all.Length} unlocked";
            if (_progressBar)
                _progressBar.value = _all.Length > 0 ? (float)unlocked / _all.Length : 0f;
        }

        private void SpawnItem(Achievement a)
        {
            if (_itemPrefab == null || _listContent == null) return;
            var go    = Instantiate(_itemPrefab, _listContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = a.title;
                texts[1].text = a.description;
            }

            // Locked style
            var img = go.GetComponent<Image>();
            if (img && !a.unlocked) img.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);

            // Progress bar inside item (optional)
            var bar = go.GetComponentInChildren<Slider>();
            if (bar && a.maxProgress > 0)
                bar.value = (float)a.progress / a.maxProgress;
        }

        // ============================================
        // Filter tabs
        // ============================================

        public void OnClickTabAll()      { _filter = "all";      Render(); }
        public void OnClickTabUnlocked() { _filter = "unlocked"; Render(); }
        public void OnClickTabLocked()   { _filter = "locked";   Render(); }

        // ============================================
        // In-game unlock toast
        // ============================================

        public static void ShowUnlockToast(Achievement a)
        {
            NotificationToast.Show($"🏆 Achievement unlocked: {a.title}", "success", 5f);
        }

        private void ClearList()
        {
            foreach (Transform t in _listContent) Destroy(t.gameObject);
        }
    }
}
