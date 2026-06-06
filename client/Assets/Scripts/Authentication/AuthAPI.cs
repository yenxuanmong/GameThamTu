// ============================================
// AuthAPI — calls to /api/auth/*
// ============================================
using System;
using System.Collections;
using UnityEngine;
using DetectiveRoyale.Core;

namespace DetectiveRoyale.Authentication
{
    [Serializable] class LoginRequest    { public string email; public string password; }
    [Serializable] class RegisterRequest { public string username; public string email; public string password; }
    [Serializable] class ForgotRequest   { public string email; }
    [Serializable] class ResetRequest    { public string token; public string newPassword; }
    [Serializable] class ChangePassReq   { public string currentPassword; public string newPassword; }

    [Serializable] class LoginResponse   { public PlayerProfile player; public string accessToken; public string refreshToken; public long expiresIn; }
    [Serializable] class ProfileResponse { public PlayerProfile player; }
    [Serializable] class SuccessResponse { public bool success; public string message; }

    public class AuthAPI : MonoBehaviour
    {
        public static AuthAPI Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ---- Login ----

        public IEnumerator Login(string email, string password,
            Action<PlayerProfile> onSuccess, Action<string> onError)
        {
            var body = new LoginRequest { email = email, password = password };
            yield return ApiClient.Instance.Post<LoginResponse>("/auth/login", body,
                resp =>
                {
                    AuthState.Instance.SaveTokens(new AuthTokens
                    {
                        accessToken  = resp.accessToken,
                        refreshToken = resp.refreshToken,
                        expiresIn    = resp.expiresIn,
                    });
                    AuthState.Instance.SavePlayer(resp.player);
                    onSuccess?.Invoke(resp.player);
                },
                onError);
        }

        // ---- Register ----

        public IEnumerator Register(string username, string email, string password,
            Action<PlayerProfile> onSuccess, Action<string> onError)
        {
            var body = new RegisterRequest { username = username, email = email, password = password };
            yield return ApiClient.Instance.Post<LoginResponse>("/auth/register", body,
                resp =>
                {
                    AuthState.Instance.SaveTokens(new AuthTokens
                    {
                        accessToken  = resp.accessToken,
                        refreshToken = resp.refreshToken,
                        expiresIn    = resp.expiresIn,
                    });
                    AuthState.Instance.SavePlayer(resp.player);
                    onSuccess?.Invoke(resp.player);
                },
                onError);
        }

        // ---- Fetch own profile ----

        public IEnumerator GetProfile(Action<PlayerProfile> onSuccess, Action<string> onError = null)
        {
            yield return ApiClient.Instance.Get<ProfileResponse>("/auth/me",
                resp =>
                {
                    AuthState.Instance.SavePlayer(resp.player);
                    onSuccess?.Invoke(resp.player);
                },
                onError);
        }

        // ---- Logout ----

        public IEnumerator Logout(Action onDone = null)
        {
            yield return ApiClient.Instance.Post<SuccessResponse>("/auth/logout", null,
                _ => { }, _ => { });
            AuthState.Instance.Logout();
            onDone?.Invoke();
        }

        // ---- Forgot password ----

        public IEnumerator ForgotPassword(string email,
            Action<string> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.Post<SuccessResponse>("/auth/forgot-password",
                new ForgotRequest { email = email },
                resp => onSuccess?.Invoke(resp.message),
                onError);
        }

        // ---- Reset password ----

        public IEnumerator ResetPassword(string token, string newPassword,
            Action<string> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.Post<SuccessResponse>("/auth/reset-password",
                new ResetRequest { token = token, newPassword = newPassword },
                resp => onSuccess?.Invoke(resp.message),
                onError);
        }

        // ---- Change password ----

        public IEnumerator ChangePassword(string current, string newPass,
            Action onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.Post<SuccessResponse>("/auth/me/change-password",
                new ChangePassReq { currentPassword = current, newPassword = newPass },
                _ => onSuccess?.Invoke(),
                onError);
        }
    }
}
