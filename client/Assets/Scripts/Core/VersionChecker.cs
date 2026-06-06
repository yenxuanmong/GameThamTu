// ============================================
// VersionChecker — checks for required backend version compatibility
// ============================================
using System.Collections;
using UnityEngine;

namespace DetectiveRoyale.Core
{
    [System.Serializable]
    class VersionResponse
    {
        public string version;
        public string minClientVersion;
        public bool   maintenanceMode;
        public string maintenanceMessage;
    }

    public class VersionChecker : MonoBehaviour
    {
        [SerializeField] private string _clientVersion = "1.0.0";
        [SerializeField] private UI.NotificationToast _toast;

        void Start() => StartCoroutine(Check());

        private IEnumerator Check()
        {
            yield return ApiClient.Instance.Get<VersionResponse>("/health",
                resp =>
                {
                    if (resp.maintenanceMode)
                    {
                        UI.NotificationToast.Show(
                            resp.maintenanceMessage ?? "Server maintenance in progress.",
                            "warning", 10f);
                    }

                    if (!string.IsNullOrEmpty(resp.minClientVersion) &&
                        CompareVersions(_clientVersion, resp.minClientVersion) < 0)
                    {
                        UI.NotificationToast.Show(
                            "Your client is outdated. Please update the game.",
                            "error", 0f);
                    }
                },
                _ => { /* ignore — server may not have /health */ });
        }

        private static int CompareVersions(string a, string b)
        {
            var pa = System.Version.Parse(a);
            var pb = System.Version.Parse(b);
            return pa.CompareTo(pb);
        }
    }
}
