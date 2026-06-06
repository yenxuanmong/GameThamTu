// ============================================
// AuthState — global auth state + token storage
// ============================================
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DetectiveRoyale.Core
{
    public class AuthState : MonoBehaviour
    {
        public static AuthState Instance { get; private set; }

        // ---- Persistent keys ----
        private const string KEY_ACCESS  = "dr_access_token";
        private const string KEY_REFRESH = "dr_refresh_token";
        private const string KEY_PLAYER  = "dr_player_json";

        // ---- State ----
        public string        AccessToken  { get; private set; }
        public string        RefreshToken { get; private set; }
        public PlayerProfile Player       { get; private set; }
        public bool          IsLoggedIn   => !string.IsNullOrEmpty(AccessToken);

        // ---- Events ----
        public UnityEvent          OnLogin  = new UnityEvent();
        public UnityEvent          OnLogout = new UnityEvent();
        public UnityEvent<string>  OnError  = new UnityEvent<string>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromPrefs();
        }

        // ---- Save tokens ----

        public void SaveTokens(AuthTokens tokens)
        {
            AccessToken  = tokens.accessToken;
            RefreshToken = tokens.refreshToken;
            PlayerPrefs.SetString(KEY_ACCESS,  AccessToken);
            PlayerPrefs.SetString(KEY_REFRESH, RefreshToken);
            PlayerPrefs.Save();
        }

        public void SavePlayer(PlayerProfile profile)
        {
            Player = profile;
            PlayerPrefs.SetString(KEY_PLAYER, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }

        public void Logout()
        {
            AccessToken  = null;
            RefreshToken = null;
            Player       = null;
            PlayerPrefs.DeleteKey(KEY_ACCESS);
            PlayerPrefs.DeleteKey(KEY_REFRESH);
            PlayerPrefs.DeleteKey(KEY_PLAYER);
            PlayerPrefs.Save();
            OnLogout?.Invoke();
        }

        // ---- Refresh ----

        public IEnumerator TryRefreshToken(Action<bool> callback)
        {
            if (string.IsNullOrEmpty(RefreshToken)) { callback(false); yield break; }

            string json  = $"{{\"refreshToken\":\"{RefreshToken}\"}}";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string url   = $"{GameConfig.Instance.ApiBase}/auth/refresh";

            var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var tokens = JsonUtility.FromJson<AuthTokens>(req.downloadHandler.text);
                SaveTokens(tokens);
                callback(true);
            }
            else
            {
                callback(false);
            }
        }

        // ---- Load from PlayerPrefs ----

        private void LoadFromPrefs()
        {
            AccessToken  = PlayerPrefs.GetString(KEY_ACCESS,  null);
            RefreshToken = PlayerPrefs.GetString(KEY_REFRESH, null);
            string playerJson = PlayerPrefs.GetString(KEY_PLAYER, null);
            if (!string.IsNullOrEmpty(playerJson))
            {
                try { Player = JsonUtility.FromJson<PlayerProfile>(playerJson); }
                catch { /* ignore */ }
            }
        }
    }
}
