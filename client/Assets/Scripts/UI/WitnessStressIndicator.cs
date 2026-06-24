// ============================================
// WitnessStressIndicator — UI indicator showing witness stress level
// Displayed in dialogue header, updates on npc:response
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.UI
{
    public class WitnessStressIndicator : MonoBehaviour
    {
        [SerializeField] private Slider   _stressBar;
        [SerializeField] private TMP_Text _stressLabel;
        [SerializeField] private Image    _stressIcon;

        [SerializeField] private Color _calmColor     = Color.green;
        [SerializeField] private Color _mildColor     = new Color(0.7f, 1f, 0.2f);
        [SerializeField] private Color _moderateColor = Color.yellow;
        [SerializeField] private Color _highColor     = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color _extremeColor  = Color.red;

        public void SetStressLevel(string level)
        {
            var (val, col, label) = level switch
            {
                "calm"     => (0.0f, _calmColor,     "Calm"),
                "mild"     => (0.25f,_mildColor,     "Mild"),
                "moderate" => (0.5f, _moderateColor, "Moderate"),
                "high"     => (0.75f,_highColor,     "High"),
                "extreme"  => (1.0f, _extremeColor,  "Extreme"),
                _          => (0.0f, _calmColor,     "Calm")
            };

            if (_stressBar)   _stressBar.value = val;
            if (_stressLabel) _stressLabel.text = label;
            if (_stressIcon)  _stressIcon.color = col;
            if (_stressBar?.fillRect?.GetComponent<Image>() is Image fill)
                fill.color = col;
        }
    }
}
