# Detective Royale — Unity Client

Unity 2022.3 LTS multiplayer murder mystery game client.

> **New to the project? → Read [SETUP.md](SETUP.md) first.**

## Required Packages (Package Manager)

| Package | ID | Purpose |
|---|---|---|
| TextMeshPro | `com.unity.textmeshpro` | All UI text |
| SocketIOUnity | `com.itisnajim.socketiounity` | Socket.IO client |
| NativeWebSocket | `com.endel.nativewebsocket` | WebSocket transport |
| Unity WebRTC | `com.unity.webrtc` | Voice chat |
| Newtonsoft JSON | `com.unity.nuget.newtonsoft-json` | JSON serialisation |

### Install via Package Manager → Add package by name:
```
com.itisnajim.socketiounity
com.endel.nativewebsocket
com.unity.webrtc
com.unity.nuget.newtonsoft-json
```

## Project Structure

```
Assets/Scripts/
├── Core/
│   ├── GameBootstrapper.cs    ← Add to first scene (initialises all singletons)
│   ├── GameConfig.cs          ← ScriptableObject: server URL, timeouts
│   ├── GameSession.cs         ← Static in-match state (matchId, caseId, etc.)
│   ├── AuthState.cs           ← JWT token storage + refresh
│   ├── ApiClient.cs           ← HTTP REST client (auto-refresh on 401)
│   ├── SocketManager.cs       ← Socket.IO wrapper (all real-time events)
│   ├── SceneLoader.cs         ← Async scene loading with loading screen
│   ├── Models.cs              ← All API data transfer objects
│   ├── Extensions.cs          ← Utility extension methods
│   └── UnityMainThreadDispatcher.cs
│
├── Authentication/
│   ├── AuthAPI.cs             ← /api/auth/* HTTP calls
│   ├── LoginManager.cs        ← Login/Register/ForgotPassword UI
│   └── RegisterManager.cs
│
├── Lobby/
│   ├── LobbyManager.cs        ← Room browse, create, join, matchmaking
│   ├── RoomListUI.cs          ← One room row in the room browser
│   └── MatchmakingUI.cs       ← Queue spinner
│
├── Investigation/
│   ├── InvestigationManager.cs← Scene boot, timer, socket wiring
│   ├── EvidenceSystem.cs      ← Track discovered evidence
│   ├── DeductionBoard.cs      ← Conclusion submission form
│   ├── ClueCollector.cs       ← Clickable evidence object
│   ├── EvidenceScanner.cs     ← Evidence detail panel
│   ├── ForensicSystem.cs      ← Image/zoom for forensic evidence
│   └── CameraViewer.cs        ← CCTV footage player
│
├── NPC/
│   ├── NPCManager.cs          ← Opens/closes dialogue sessions
│   ├── DialogueSystem.cs      ← Chat panel with witness
│   ├── AIChat.cs              ← Clickable witness object
│   └── WitnessBehaviour.cs    ← Stress colour reaction
│
├── Multiplayer/
│   ├── NetworkManager.cs      ← Connect/disconnect on auth events
│   ├── ReconnectManager.cs    ← Auto-reconnect with back-off
│   ├── SyncPlayer.cs          ← Other players' actions display
│   └── VoiceChat.cs           ← WebRTC voice via socket relay
│
├── Ranking/
│   ├── RankingUI.cs           ← Leaderboard panel
│   └── SeasonUI.cs            ← Season info + history
│
├── Analytics/
│   └── MatchStats.cs          ← Performance report panel
│
└── UI/
    ├── MainMenuUI.cs          ← Main menu screen
    ├── DeductionUI.cs         ← Deduction panel toggle
    ├── EvidenceUI.cs          ← Evidence inventory panel
    ├── InventoryUI.cs         ← Tabbed inventory (evidence/suspects/notebook/timeline)
    ├── PlayerSlotUI.cs        ← Player row in room lobby
    ├── ResultUI.cs            ← Post-match results screen
    └── NotificationToast.cs   ← Global toast notifications
```

## Scenes Required

| Scene Name | Description |
|---|---|
| `MainMenu` | Login/Register + menu |
| `Lobby` | Room browser, create room, matchmaking |
| `Investigation` | Core gameplay scene |
| `Results` | Post-match scores + solution reveal |

## Setup

### 1. Create GameConfig asset
`Assets → Create → Detective Royale → Game Config`
- Set `Server URL` to your backend (default: `http://localhost:3000`)
- Place in `Assets/Resources/GameConfig.asset`

### 2. Add Bootstrapper to MainMenu scene
Create empty GameObject → Add `GameBootstrapper` component.
This auto-creates all singleton managers.

### 3. Wire up SocketManager (after installing SocketIOUnity)
In `SocketManager.cs`, uncomment the SocketIOUnity code blocks and remove stubs.

### 4. Scene Build Settings
Add all 4 scenes to `File → Build Settings → Scenes In Build` in this order:
0. MainMenu, 1. Lobby, 2. Investigation, 3. Results

## Backend API

Backend runs at `http://localhost:3000`. See `backend/README.md` for full API docs.

### Quick connection flow:
1. `AuthAPI.Login()` → saves JWT to `AuthState`
2. `NetworkManager` auto-calls `SocketManager.Connect()` with token
3. Socket events fire → Unity events on `SocketManager`
4. All REST calls via `ApiClient.Get/Post/Patch/Delete`

## Environment

Copy `backend/.env.example` to `backend/.env` and fill in:
- `DATABASE_URL` — PostgreSQL connection
- `REDIS_URL` — Redis connection  
- `OPENAI_API_KEY` — for NPC dialogue
- `JWT_SECRET` / `JWT_REFRESH_SECRET`
