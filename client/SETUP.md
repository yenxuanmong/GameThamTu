# Detective Royale — Unity Client Setup Guide

## Quick Start (5 steps)

### Step 1 — Open Project
Open Unity Hub → Add → `client/` folder → Open with Unity **2022.3 LTS**

### Step 2 — Install Packages
Open `Window → Package Manager` and install via "Add package by name":
```
com.unity.textmeshpro
com.unity.render-pipelines.universal
com.unity.inputsystem
com.unity.webrtc
com.unity.nuget.newtonsoft-json
```
Then add scoped registry packages:
```
com.endel.nativewebsocket
com.itisnajim.socketiounity
```
> Scoped registry `https://package.openupm.com` is pre-configured in `Packages/manifest.json`

### Step 3 — Create GameConfig Asset
`Detective Royale (menu) → Create GameConfig Asset`

In the Inspector, set:
- **Server URL**: `http://localhost:3000`  (or your deployed URL)
- **Socket URL**: `http://localhost:3000`

### Step 4 — Scene Build Settings
`File → Build Settings → Scenes In Build` — add in this order:
| Index | Scene |
|-------|-------|
| 0 | `Assets/Scenes/Splash.unity` |
| 1 | `Assets/Scenes/MainMenu.unity` |
| 2 | `Assets/Scenes/Lobby.unity` |
| 3 | `Assets/Scenes/Investigation.unity` |
| 4 | `Assets/Scenes/Results.unity` |

### Step 5 — Configure URP
`Edit → Project Settings → Graphics`
- Set **Scriptable Render Pipeline Settings** → `Assets/Settings/UniversalRenderPipelineAsset.asset`

---

## Scene Setup Per Scene

### Every Scene needs:
- `GameBootstrapper` component (auto-creates all singletons on Awake)

### MainMenu
| Component | GameObject |
|-----------|-----------|
| `LoginManager` | UIRoot Canvas |
| `MainMenuUI` | UIRoot Canvas |
| `NotificationToast` | NotificationToast |
| `FadeTransition` | FadeCanvas |

### Lobby
| Component | GameObject |
|-----------|-----------|
| `LobbyManager` | LobbyCanvas |
| `RankingUI` | LobbyCanvas |
| `PasswordInputDialog` | PasswordDialog |
| `ChatUI` | ChatCanvas |
| `HostControls` | LobbyCanvas (host only section) |

### Investigation
| Component | GameObject |
|-----------|-----------|
| `InvestigationManager` | InvestigationManager |
| `GameManager` | InvestigationManager |
| `EvidenceSystem` | InvestigationManager |
| `NPCManager` | InvestigationManager |
| `HUD` | HUDCanvas |
| `InventoryUI` | HUDCanvas |
| `DeductionUI` | HUDCanvas |
| `EvidenceUI` | HUDCanvas |
| `MatchTimerUI` | HUDCanvas |
| `PhaseAnnouncerUI` | HUDCanvas |
| `TutorialManager` | TutorialCanvas |
| `SyncPlayer` | MultiplayerManager |
| `VoiceGatewayClient` | MultiplayerManager |
| `MatchStateSync` | MultiplayerManager |
| `EvidenceScanner` | InvestigationPanelsCanvas |
| `NotebookUI` | InvestigationPanelsCanvas |
| `DeductionBoard` | DeductionCanvas |
| `CaseBriefingUI` | InvestigationPanelsCanvas |

### Results
| Component | GameObject |
|-----------|-----------|
| `ResultUI` | ResultsCanvas |
| `ScoreBreakdownUI` | ResultsCanvas |
| `MatchStats` | ResultsCanvas |
| `AchievementUI` | AchievementCanvas |

---

## Prefabs to Create

See `Assets/Prefabs/README.md` for the complete list of prefabs needed.

Key prefabs:
- `UI/ToastItem.prefab` → assign to `NotificationToast._toastPrefab`
- `UI/RoomListItem.prefab` → assign to `LobbyManager._roomListItemPrefab`
- `UI/PlayerSlot.prefab` → assign to `LobbyManager._playerSlotPrefab`
- `NPC/Witness_Base.prefab` → assign to `NPCManager._dialoguePrefab` (DialogueSystem)
- `Scene/EvidenceObject.prefab` → place in Investigation scene

---

## Backend Connection

Start the backend:
```bash
cd backend
npm install
npm run dev
```

Backend runs at `http://localhost:3000`.  
GameConfig default URL matches this — no change needed for local development.

---

## Controls (Investigation Scene)

| Key | Action |
|-----|--------|
| Click | Move to location / Collect evidence |
| I | Toggle inventory |
| D | Open deduction board |
| H | Request hint |
| E | Talk to nearby witness |
| Esc | Close active panel |
| F12 | Screenshot |
| ` (backtick) | Debug console (dev builds only) |

---

## Build

`Detective Royale (menu) → Build → Windows x64` (or macOS / Linux / Android)

Or via command line:
```bash
Unity -batchmode -quit -projectPath ./client -executeMethod BuildAutomation.BuildWindows
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `GameConfig not found` | Run menu: `Detective Royale → Create GameConfig Asset` |
| Socket not connecting | Check `GameConfig.SocketUrl`, ensure backend is running |
| Missing TextMeshPro | `Window → TextMeshPro → Import TMP Essential Resources` |
| URP pink materials | Set Graphics settings to use `UniversalRenderPipelineAsset` |
| Scene not in build | Add all 5 scenes to `File → Build Settings` |
| Prefab reference missing | Run `Detective Royale → Validate Prefab References` |
| Assembly errors | Delete `Library/` folder and let Unity reimport |
