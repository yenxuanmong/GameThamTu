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

        void Start() => StartCoroutine(Check());

        private IEnumerator Check()
        {
            yield return ApiClient.Instance.Get<VersionResponse>("/health",
                resp =>
                {
                    if (resp.maintenanceMode)
                        ShowToast(resp.maintenanceMessage ?? "Server maintenance in progress.");

                    if (!string.IsNullOrEmpty(resp.minClientVersion) &&
                        CompareVersions(_clientVersion, resp.minClientVersion) < 0)
                        ShowToast("Your client is outdated. Please update the game.");
                },
                _ => { /* ignore */ });
        }

        private static void ShowToast(string msg)
        {
            var go = GameObject.Find("NotificationToast");
            if (go != null)
                go.SendMessage("ShowToast", msg, SendMessageOptions.DontRequireReceiver);
        }

        private static int CompareVersions(string a, string b)
        {
            var pa = System.Version.Parse(a);
            var pb = System.Version.Parse(b);
            return pa.CompareTo(pb);
        }
    }
}
