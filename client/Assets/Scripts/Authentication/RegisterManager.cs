// ============================================
// RegisterManager — standalone register screen (optional separate scene)
// If using LoginManager's built-in panel, this script is not needed.
// ============================================
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Authentication
{
    /// <summary>
    /// Thin wrapper — delegates to LoginManager.ShowRegister() if
    /// login and register are on the same scene.
    /// Otherwise attach this to a dedicated RegisterScene.
    /// </summary>
    public class RegisterManager : MonoBehaviour
    {
        void Start()
        {
            // If in same scene as LoginManager, just show register panel
            LoginManager lm = FindFirstObjectByType<LoginManager>();
            lm?.ShowRegister();
        }
    }
}
