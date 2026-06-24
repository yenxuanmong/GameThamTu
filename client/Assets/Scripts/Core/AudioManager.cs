// ============================================
// AudioManager — centralised audio control
// Handles BGM, SFX and voice chat audio routing
// ============================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace DetectiveRoyale.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _mixer;

        [Header("BGM Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _bgmSourceB;   // cross-fade target

        [Header("SFX Source Pool")]
        [SerializeField] private int _sfxPoolSize = 8;

        [Header("Clips")]
        [SerializeField] private AudioClip _mainMenuBgm;
        [SerializeField] private AudioClip _lobbyBgm;
        [SerializeField] private AudioClip _investigationBgm;
        [SerializeField] private AudioClip _finalMinutesBgm;
        [SerializeField] private AudioClip _resultsBgm;
        [SerializeField] private AudioClip _clickSfx;
        [SerializeField] private AudioClip _evidenceFoundSfx;
        [SerializeField] private AudioClip _hintSfx;
        [SerializeField] private AudioClip _submitSfx;
        [SerializeField] private AudioClip _errorSfx;
        [SerializeField] private AudioClip _notificationSfx;
        [SerializeField] private AudioClip _countdownSfx;

        // Mixer param names
        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_BGM    = "BGMVolume";
        private const string PARAM_SFX    = "SFXVolume";
        private const string PARAM_VOICE  = "VoiceVolume";

        // PlayerPrefs keys (same as SettingsUI)
        private const string K_MASTER = "vol_master";
        private const string K_MUSIC  = "vol_music";
        private const string K_SFX    = "vol_sfx";

        private List<AudioSource> _sfxPool = new();
        private bool _crossFading;

        // ============================================
        // Init
        // ============================================

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSfxPool();
            ApplySavedVolumes();
        }

        void Start()
        {
            // Hook socket events for SFX
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.OnEvidenceFound.AddListener(_ => PlaySfx(_evidenceFoundSfx));
                SocketManager.Instance.OnHintReceived .AddListener(_ => PlaySfx(_hintSfx));
                SocketManager.Instance.OnRoomCountdown.AddListener(OnCountdown);
                SocketManager.Instance.OnMatchStarted .AddListener(_ => PlaySfx(_submitSfx));
                SocketManager.Instance.OnMatchEnded   .AddListener(_ => CrossFadeTo(_resultsBgm));
                SocketManager.Instance.OnPhaseChanged .AddListener(OnPhaseChanged);
            }
        }

        // ============================================
        // BGM
        // ============================================

        public void PlayBGM(AudioClip clip, bool loop = true)
        {
            if (_bgmSource == null || clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
            _bgmSource.clip  = clip;
            _bgmSource.loop  = loop;
            _bgmSource.Play();
        }

        public void PlaySceneBGM(string sceneName)
        {
            AudioClip clip = sceneName switch
            {
                SceneLoader.SCENE_MAIN_MENU    => _mainMenuBgm,
                SceneLoader.SCENE_LOBBY        => _lobbyBgm,
                SceneLoader.SCENE_INVESTIGATION=> _investigationBgm,
                SceneLoader.SCENE_RESULTS      => _resultsBgm,
                _                              => null
            };
            if (clip != null) CrossFadeTo(clip);
        }

        public void CrossFadeTo(AudioClip newClip, float duration = 1.5f)
        {
            if (newClip == null || _crossFading) return;
            StartCoroutine(CrossFadeCoroutine(newClip, duration));
        }

        private IEnumerator CrossFadeCoroutine(AudioClip newClip, float duration)
        {
            _crossFading = true;

            // Prepare B source
            if (_bgmSourceB != null)
            {
                _bgmSourceB.clip   = newClip;
                _bgmSourceB.loop   = true;
                _bgmSourceB.volume = 0f;
                _bgmSourceB.Play();
            }

            float start = _bgmSource ? _bgmSource.volume : 0f;
            float t     = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float ratio = t / duration;
                if (_bgmSource)  _bgmSource.volume  = Mathf.Lerp(start, 0f, ratio);
                if (_bgmSourceB) _bgmSourceB.volume = Mathf.Lerp(0f, start, ratio);
                yield return null;
            }

            if (_bgmSource)
            {
                _bgmSource.Stop();
                _bgmSource.clip   = newClip;
                _bgmSource.volume = start;
                _bgmSource.Play();
            }
            if (_bgmSourceB)
            {
                _bgmSourceB.Stop();
                _bgmSourceB.volume = 0f;
            }

            _crossFading = false;
        }

        public void StopBGM()
        {
            _bgmSource?.Stop();
            _bgmSourceB?.Stop();
        }

        // ============================================
        // SFX
        // ============================================

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var source = GetFreeSfxSource();
            if (source == null) return;
            source.clip   = clip;
            source.volume = volume;
            source.Play();
        }

        public void PlayClick()     => PlaySfx(_clickSfx,        0.6f);
        public void PlayError()     => PlaySfx(_errorSfx,        0.7f);
        public void PlayNotif()     => PlaySfx(_notificationSfx, 0.5f);
        public void PlayCountdown() => PlaySfx(_countdownSfx,    0.8f);

        // ============================================
        // Volume setters (called by SettingsUI)
        // ============================================

        public void SetMasterVolume(float linear)
        {
            SetMixerVolume(PARAM_MASTER, linear);
            PlayerPrefs.SetFloat(K_MASTER, linear);
        }

        public void SetMusicVolume(float linear)
        {
            SetMixerVolume(PARAM_BGM, linear);
            PlayerPrefs.SetFloat(K_MUSIC, linear);
        }

        public void SetSfxVolume(float linear)
        {
            SetMixerVolume(PARAM_SFX, linear);
            PlayerPrefs.SetFloat(K_SFX, linear);
        }

        public void SetVoiceVolume(float linear) => SetMixerVolume(PARAM_VOICE, linear);

        // ============================================
        // Helpers
        // ============================================

        private void ApplySavedVolumes()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(K_MASTER, 1f));
            SetMusicVolume(PlayerPrefs.GetFloat(K_MUSIC,   0.7f));
            SetSfxVolume(PlayerPrefs.GetFloat(K_SFX,       1f));
        }

        private void SetMixerVolume(string param, float linear)
        {
            if (_mixer == null) return;
            // Convert linear to dB: 0 → -80dB, 1 → 0dB
            float db = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
            _mixer.SetFloat(param, db);
        }

        private void BuildSfxPool()
        {
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go  = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            foreach (var src in _sfxPool)
                if (!src.isPlaying) return src;
            return _sfxPool.Count > 0 ? _sfxPool[0] : null;
        }

        // ============================================
        // Socket-driven SFX
        // ============================================

        private void OnPhaseChanged(PhasePayload p)
        {
            if (p.phase == "final_minutes")
                CrossFadeTo(_finalMinutesBgm);
        }

        private void OnCountdown(CountdownPayload p)
        {
            if (p.seconds <= 3 && p.seconds > 0)
                PlayCountdown();
        }
    }
}
