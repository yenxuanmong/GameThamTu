# Detective Royale — Audio Guide

## Audio Mixer

Create `Assets/Audio/MasterMixer.mixer` in Unity:
1. `Create → Audio Mixer` → name it `MasterMixer`
2. Add groups: **BGM**, **SFX**, **Voice**
3. Expose parameters: `MasterVolume`, `BGMVolume`, `SFXVolume`, `VoiceVolume`
4. Assign to `AudioManager._mixer` in Inspector

---

## BGM Tracks

| File | Scene | Notes |
|------|-------|-------|
| `BGM_MainMenu.ogg` | Main Menu | Mysterious, slow piano + strings |
| `BGM_Lobby.ogg` | Lobby | Light jazzy background |
| `BGM_Investigation.ogg` | Investigation | Tense, orchestral noir |
| `BGM_FinalMinutes.ogg` | Investigation (last 5 min) | Urgent strings, rising tension |
| `BGM_Results_Win.ogg` | Results (win) | Triumphant, brass fanfare |
| `BGM_Results_Lose.ogg` | Results (lose) | Somber, slow piano |

**Recommended format:** OGG Vorbis, 44100 Hz, stereo, quality 7

---

## SFX Clips

| File | Event | Used by |
|------|-------|---------|
| `SFX_Click.wav` | UI button click | AudioManager.PlayClick() |
| `SFX_EvidenceFound.wav` | Evidence collected | OnEvidenceFound socket event |
| `SFX_Hint.wav` | Hint received | OnHintReceived socket event |
| `SFX_Submit.wav` | Conclusion submitted | Match start / submit |
| `SFX_Error.wav` | Error / invalid action | AudioManager.PlayError() |
| `SFX_Notification.wav` | Toast notification | AudioManager.PlayNotif() |
| `SFX_Countdown.wav` | Final countdown beep | OnCountdown (≤3 seconds) |
| `SFX_PageTurn.wav` | Notebook / inventory tab switch | InventoryUI |
| `SFX_Unlock.wav` | Achievement unlocked | AchievementUI.ShowUnlockToast |
| `SFX_MatchFound.wav` | Match found in queue | MatchmakingUI.OnMatchFound |
| `SFX_PlayerJoin.wav` | Player joined room | Socket room:joined |
| `SFX_PlayerLeave.wav` | Player left room | Socket room:updated |
| `SFX_Footstep.wav` | Player movement (optional) | PlayerController (future) |
| `SFX_DoorOpen.wav` | Scene transition (optional) | SceneLoader |

**Recommended format:** WAV, 44100 Hz, mono for SFX

---

## Voice / Ambient

| File | Usage |
|------|-------|
| `AMB_CrimeScene.ogg` | Ambient loop for Investigation scene |
| `AMB_Clock.ogg` | Ticking clock ambience (final minutes) |
| `AMB_Rain.ogg` | Optional rain atmosphere |

---

## Import Settings (Unity)

### BGM
- Load Type: **Streaming**
- Compression: **Vorbis**
- Quality: 70

### SFX
- Load Type: **Decompress On Load**
- Compression: **PCM** (short clips) or **ADPCM**

### Ambient
- Load Type: **Streaming**
- Compression: **Vorbis**
- Quality: 50

---

## Assign clips to AudioManager

In Inspector, drag clips to the matching slots on `AudioManager`:
- `_mainMenuBgm` → `BGM_MainMenu`
- `_lobbyBgm` → `BGM_Lobby`
- `_investigationBgm` → `BGM_Investigation`
- `_finalMinutesBgm` → `BGM_FinalMinutes`
- `_resultsBgm` → `BGM_Results_Win` or `_Lose` (swap via script)
- `_clickSfx` → `SFX_Click`
- `_evidenceFoundSfx` → `SFX_EvidenceFound`
- etc.
