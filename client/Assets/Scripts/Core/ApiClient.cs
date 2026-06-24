// ============================================
// ApiClient — HTTP REST calls to backend
// ============================================
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveRoyale.Core
{
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        private string _baseUrl;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _baseUrl = GameConfig.Instance.ApiBase;
        }

        // ---- Generic request helpers ----

        public IEnumerator Get<T>(string path, Action<T> onSuccess, Action<string> onError = null)
        {
            yield return SendRequest<T>(UnityWebRequest.Get(Url(path)), onSuccess, onError);
        }

        public IEnumerator Post<T>(string path, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            yield return SendRequest<T>(BuildPost(path, body), onSuccess, onError);
        }

        public IEnumerator Patch<T>(string path, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            var req = BuildPost(path, body);
            req.method = "PATCH";
            yield return SendRequest<T>(req, onSuccess, onError);
        }

        public IEnumerator Delete<T>(string path, Action<T> onSuccess, Action<string> onError = null)
        {
            yield return SendRequest<T>(UnityWebRequest.Delete(Url(path)), onSuccess, onError);
        }

        // ---- Core send ----

        private IEnumerator SendRequest<T>(UnityWebRequest req, Action<T> onSuccess, Action<string> onError)
        {
            // Attach auth token
            string token = AuthState.Instance.AccessToken;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            req.SetRequestHeader("Accept", "application/json");
            req.timeout = (int)GameConfig.Instance.HttpTimeoutSeconds;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.ProtocolError)
            {
                string errMsg = TryParseError(req.downloadHandler?.text) ?? req.error;

                // 401 → try refresh then retry once
                if (req.responseCode == 401)
                {
                    bool refreshed = false;
                    yield return StartCoroutine(RefreshAndRetry<T>(req.url, req.method,
                        req.uploadHandler?.data, (ok) => refreshed = ok,
                        (result) => onSuccess?.Invoke(result),
                        (err)    => onError?.Invoke(err)));
                    if (refreshed) yield break;
                }

                onError?.Invoke(errMsg);
            }
            else
            {
                try
                {
                    T result = JsonUtility.FromJson<T>(req.downloadHandler.text);
                    onSuccess?.Invoke(result);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }
            }
        }

        // ---- Token refresh ----

        private IEnumerator RefreshAndRetry<T>(string url, string method, byte[] body,
            Action<bool> refreshed, Action<T> onSuccess, Action<string> onError)
        {
            bool ok = false;
            yield return StartCoroutine(AuthState.Instance.TryRefreshToken((success) => ok = success));

            if (!ok) { AuthState.Instance.Logout(); onError?.Invoke("Session expired. Please log in again."); refreshed(false); yield break; }

            var retryReq = new UnityWebRequest(url, method);
            if (body != null)
                retryReq.uploadHandler = new UploadHandlerRaw(body);
            retryReq.downloadHandler = new DownloadHandlerBuffer();
            retryReq.SetRequestHeader("Authorization", $"Bearer {AuthState.Instance.AccessToken}");
            retryReq.SetRequestHeader("Content-Type", "application/json");
            retryReq.SetRequestHeader("Accept", "application/json");
            retryReq.timeout = (int)GameConfig.Instance.HttpTimeoutSeconds;

            yield return retryReq.SendWebRequest();

            refreshed(true);

            if (retryReq.result != UnityWebRequest.Result.Success)
            { onError?.Invoke(TryParseError(retryReq.downloadHandler?.text) ?? retryReq.error); }
            else
            {
                try { onSuccess?.Invoke(JsonUtility.FromJson<T>(retryReq.downloadHandler.text)); }
                catch (Exception ex) { onError?.Invoke($"Parse error: {ex.Message}"); }
            }
        }

        // ---- Helpers ----

        private UnityWebRequest BuildPost(string path, object body)
        {
            string json = body != null ? JsonUtility.ToJson(body) : "{}";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            var req = new UnityWebRequest(Url(path), "POST");
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            return req;
        }

        private string Url(string path) =>
            path.StartsWith("http") ? path : $"{_baseUrl}{path}";

        private static string TryParseError(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<ApiError>(json)?.error; }
            catch { return null; }
        }
    }
}
