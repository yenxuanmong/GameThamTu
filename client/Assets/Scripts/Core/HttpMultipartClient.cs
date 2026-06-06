// ============================================
// HttpMultipartClient — multipart/form-data upload helper
// Used for avatar upload and future file uploads
// ============================================
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveRoyale.Core
{
    public static class HttpMultipartClient
    {
        // ---- Upload a file from disk path ----
        public static IEnumerator UploadFile(
            string url,
            string fieldName,
            string filePath,
            Action<string> onSuccess,
            Action<string> onError = null)
        {
            if (!File.Exists(filePath))
            {
                onError?.Invoke($"File not found: {filePath}");
                yield break;
            }

            byte[] data     = File.ReadAllBytes(filePath);
            string mimeType = GetMimeType(Path.GetExtension(filePath));
            string fileName = Path.GetFileName(filePath);

            yield return UploadBytes(url, fieldName, fileName, data, mimeType, onSuccess, onError);
        }

        // ---- Upload raw bytes ----
        public static IEnumerator UploadBytes(
            string url,
            string fieldName,
            string fileName,
            byte[] data,
            string mimeType,
            Action<string> onSuccess,
            Action<string> onError = null)
        {
            var form = new WWWForm();
            form.AddBinaryData(fieldName, data, fileName, mimeType);

            using var req = UnityWebRequest.Post(url, form);

            string token = AuthState.Instance?.AccessToken;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            req.timeout = (int)GameConfig.Instance.HttpTimeoutSeconds;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text);
            else
                onError?.Invoke(TryParseError(req.downloadHandler?.text) ?? req.error);
        }

        // ---- Upload Texture2D directly ----
        public static IEnumerator UploadTexture(
            string url,
            string fieldName,
            Texture2D texture,
            Action<string> onSuccess,
            Action<string> onError = null)
        {
            byte[] png = texture.EncodeToPNG();
            yield return UploadBytes(url, fieldName, "avatar.png", png, "image/png",
                onSuccess, onError);
        }

        // ---- Helpers ----

        private static string GetMimeType(string ext) => ext?.ToLower() switch
        {
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png"  => "image/png",
            ".webp" => "image/webp",
            ".mp4"  => "video/mp4",
            ".mp3"  => "audio/mpeg",
            ".wav"  => "audio/wav",
            _       => "application/octet-stream"
        };

        private static string TryParseError(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<ApiError>(json)?.error; }
            catch { return null; }
        }
    }
}
