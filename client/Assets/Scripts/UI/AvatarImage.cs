// ============================================
// AvatarImage — loads and displays a player avatar from URL
// Caches texture in memory to avoid redundant requests
// ============================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace DetectiveRoyale.UI
{
    [RequireComponent(typeof(Image))]
    public class AvatarImage : MonoBehaviour
    {
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Color  _loadingTint = new Color(0.7f, 0.7f, 0.7f, 1f);

        private static readonly Dictionary<string, Texture2D> _cache = new();

        private Image  _image;
        private string _currentUrl;

        void Awake()
        {
            _image = GetComponent<Image>();
            if (_defaultSprite != null && _image != null)
                _image.sprite = _defaultSprite;
        }

        public void Load(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                SetDefault();
                return;
            }

            if (_currentUrl == url) return;
            _currentUrl = url;

            if (_cache.TryGetValue(url, out var cached))
            {
                ApplyTexture(cached);
                return;
            }

            StartCoroutine(LoadCoroutine(url));
        }

        public void SetDefault()
        {
            _currentUrl = null;
            if (_image && _defaultSprite)
                _image.sprite = _defaultSprite;
        }

        private IEnumerator LoadCoroutine(string url)
        {
            if (_image) _image.color = _loadingTint;

            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var tex = ((DownloadHandlerTexture)req.downloadHandler).texture;
                _cache[url] = tex;

                // Only apply if this URL is still current
                if (_currentUrl == url) ApplyTexture(tex);
            }
            else
            {
                if (_image) _image.color = Color.white;
                SetDefault();
            }
        }

        private void ApplyTexture(Texture2D tex)
        {
            if (_image == null) return;
            _image.color  = Color.white;
            _image.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                Vector2.one * 0.5f);
        }

        public static void ClearCache() => _cache.Clear();
    }
}
