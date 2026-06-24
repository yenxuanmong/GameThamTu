// ============================================
// TierBadgeUI — displays rank tier as coloured badge
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DetectiveRoyale.UI
{
    public class TierBadgeUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image    _background;

        private static readonly (string tier, Color color, string display)[] TierData =
        {
            ("rookie",       new Color(0.55f, 0.55f, 0.55f), "Rookie"),
            ("detective",    new Color(0.20f, 0.65f, 1.00f), "Detective"),
            ("inspector",    new Color(0.20f, 0.85f, 0.45f), "Inspector"),
            ("sergeant",     new Color(0.20f, 0.45f, 0.90f), "Sergeant"),
            ("lieutenant",   new Color(0.55f, 0.20f, 0.90f), "Lieutenant"),
            ("captain",      new Color(1.00f, 0.55f, 0.10f), "Captain"),
            ("commissioner", new Color(1.00f, 0.85f, 0.10f), "Commissioner"),
        };

        public void SetTier(string tier)
        {
            string key = tier?.ToLower() ?? "rookie";
            foreach (var (t, col, disp) in TierData)
            {
                if (t != key) continue;
                if (_label)      _label.text  = disp;
                if (_background) _background.color = col;
                return;
            }
            // Fallback
            if (_label)      _label.text  = tier ?? "—";
            if (_background) _background.color = Color.grey;
        }
    }
}
