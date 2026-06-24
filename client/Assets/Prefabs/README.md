# Detective Royale — Prefab Guide

Create these prefabs in Unity Editor by building them in the scene then dragging to the Prefabs folder.

---

## UI Prefabs

### `UI/ToastItem.prefab`
```
Canvas (Screen Space Overlay)
└── ToastItem [RectTransform, Image (CanvasGroup), LayoutElement]
    └── Message [TMP_Text]
```
**Used by:** `NotificationToast._toastPrefab`

---

### `UI/RoomListItem.prefab`
```
RoomListItem [RectTransform, Image, RoomListItem.cs]
├── RoomName [TMP_Text]
├── Difficulty [TMP_Text]
├── Players [TMP_Text]
├── LockIcon [TMP_Text]
└── JoinBtn [Button]
    └── Label [TMP_Text] = "Join"
```
**Used by:** `LobbyManager._roomListItemPrefab`

---

### `UI/PlayerSlot.prefab`
```
PlayerSlot [RectTransform, Image, PlayerSlotUI.cs]
├── Avatar [Image]
├── Username [TMP_Text]
├── Tier [TMP_Text]
├── HostBadge [TMP_Text] = "HOST"
└── ReadyBadge [TMP_Text] = "READY"
```
**Used by:** `LobbyManager._playerSlotPrefab`

---

### `UI/ScoreItem.prefab`
```
ScoreItem [RectTransform, Image]
├── Rank [TMP_Text]       # e.g. "#1"
├── Username [TMP_Text]
├── Score [TMP_Text]      # e.g. "850"
└── RPChange [TMP_Text]   # e.g. "+42"
```
**Used by:** `ResultUI._scoreItemPrefab`

---

### `UI/EvidenceItem.prefab`
```
EvidenceItem [RectTransform, Image, EvidenceListItem.cs]
├── Name [TMP_Text]
├── Type [TMP_Text]
└── ViewBtn [Button]
    └── Label [TMP_Text] = "View"
```
**Used by:** `EvidenceUI._evidenceItemPrefab`

---

### `UI/LeaderboardEntry.prefab`
```
LeaderboardEntry [RectTransform, Image]
├── Rank [TMP_Text]
├── Username [TMP_Text]
├── Tier [TMP_Text]
├── Points [TMP_Text]
└── Wins [TMP_Text]
```
**Used by:** `RankingUI._entryPrefab`

---

### `UI/PlayerStatusIcon.prefab`
```
PlayerStatusIcon [RectTransform, Image]
├── PlayerId [TMP_Text]
└── Status [TMP_Text]
```
**Used by:** `SyncPlayer._playerStatusPrefab`

---

### `UI/NoteItem.prefab`
```
NoteItem [RectTransform, Image]
├── Content [TMP_Text]
└── DeleteBtn [Button, name="DeleteBtn"]
    └── Label [TMP_Text] = "✕"
```
**Used by:** `NotebookManager._noteItemPrefab`, `InventoryUI._noteItemPrefab`

---

### `UI/SuspectCard.prefab`
```
SuspectCard [RectTransform, Image, Button]
├── Name [TMP_Text]
└── Occupation [TMP_Text]
```
**Used by:** `SuspectBoard._suspectCardPrefab`

---

### `UI/TimelineNode.prefab`
```
TimelineNode [RectTransform, Image, Button]
├── Timestamp [TMP_Text]
└── Description [TMP_Text]
```
**Used by:** `TimelineViewer._eventNodePrefab`

---

### `UI/KeyTimelineNode.prefab`
Same as `TimelineNode.prefab` but with highlighted Image color (gold).
**Used by:** `TimelineViewer._keyEventNodePrefab`

---

### `UI/SeasonHistoryItem.prefab`
```
SeasonHistoryItem [RectTransform, Image]
├── Name [TMP_Text]
└── Date [TMP_Text]
```
**Used by:** `SeasonUI._historyItemPrefab`

---

### `UI/AchievementItem.prefab`
```
AchievementItem [RectTransform, Image]
├── Title [TMP_Text]
├── Description [TMP_Text]
└── ProgressBar [Slider]
```
**Used by:** `AchievementUI._itemPrefab`

---

### `UI/TrendDot.prefab`
```
TrendDot [RectTransform, Image, LayoutElement]
```
**Used by:** `MatchStats._trendDotPrefab`

---

### `UI/WitnessItem.prefab`
```
WitnessItem [RectTransform, Image, Button]
├── Name [TMP_Text]
├── Occupation [TMP_Text]
└── Personality [TMP_Text]
```
**Used by:** `WitnessListUI._witnessItemPrefab`

---

### `UI/SpeakingIndicator.prefab`
```
SpeakingIndicator [RectTransform, Image]
```
**Used by:** `VoiceChat._speakingIndicatorPrefab`

---

### `UI/SuspectItem.prefab` (InventoryUI)
```
SuspectItem [RectTransform, Image]
├── Name [TMP_Text]
└── Occupation [TMP_Text]
```
**Used by:** `InventoryUI._suspectItemPrefab`

---

### `UI/TimelineItem.prefab` (InventoryUI)
```
TimelineItem [RectTransform, Image]
├── Time [TMP_Text]
└── Description [TMP_Text]
```
**Used by:** `InventoryUI._timelineItemPrefab`

---

### `UI/ChatBubble_Player.prefab`
```
PlayerBubble [RectTransform, Image, HorizontalLayoutGroup]
└── Message [TMP_Text]
```
**Used by:** `DialogueSystem._playerBubblePrefab`, `ChatUI._myMessagePrefab`

---

### `UI/ChatBubble_NPC.prefab`
```
NPCBubble [RectTransform, Image, HorizontalLayoutGroup]
└── Message [TMP_Text]
```
**Used by:** `DialogueSystem._npcBubblePrefab`, `ChatUI._otherMessagePrefab`

---

### `UI/ChatBubble_System.prefab`
```
SystemBubble [RectTransform, Image]
└── Message [TMP_Text]
```
**Used by:** `ChatUI._systemMessagePrefab`

---

### `UI/OtherPlayerIcon.prefab`
```
OtherPlayerIcon [RectTransform, Image]
└── PlayerId [TMP_Text]
```
**Used by:** `GameManager._otherPlayerIconPrefab`

---

### `UI/EmotePopup.prefab`
```
EmotePopup [RectTransform, CanvasGroup]
└── Emote [TMP_Text]
```
**Used by:** `EmoteSystem._emotePopupPrefab`

---

### `UI/EvidenceMapIcon.prefab`
```
EvidenceMapIcon [RectTransform, Image]
```
**Used by:** `MiniMapUI._evidenceIconPrefab`

---

### `UI/PlayerMapIcon.prefab`
```
PlayerMapIcon [RectTransform, Image]
```
**Used by:** `MiniMapUI._playerIconPrefab`

---

## NPC / Scene Prefabs

### `NPC/Witness_Base.prefab`
```
Witness [GameObject, AIChat.cs, WitnessBehaviour.cs]
├── SpriteRenderer (or MeshRenderer)
├── Animator
├── BoxCollider2D (or CapsuleCollider)
└── NameLabel [Canvas > TMP_Text]
```

---

### `Scene/EvidenceObject.prefab`
```
EvidenceObject [GameObject, ClueCollector.cs]
├── SpriteRenderer / MeshRenderer
├── GlowEffect [ParticleSystem or Material glow]
└── Collider
```
Assign `_evidenceId` and `_evidenceName` in Inspector per instance.

---

### `Scene/CrimeSceneMarker.prefab`
```
CrimeSceneMarker [GameObject, SpriteRenderer]
```
Visual tape/outline for crime scene boundary.

---

## Dialogue Prefab

### `NPC/DialoguePanel.prefab`
```
DialoguePanel [RectTransform, DialogueSystem.cs]
├── Header
│   ├── WitnessName [TMP_Text]
│   ├── WitnessTrait [TMP_Text]
│   └── StressLabel [TMP_Text]
├── ChatScroll [ScrollRect]
│   └── Viewport > Content [VerticalLayoutGroup]
├── Input Row
│   ├── MessageInput [TMP_InputField]
│   ├── CharCount [TMP_Text]
│   └── SendBtn [Button]
├── WaitingIndicator [TMP_Text] = "..."
└── CloseBtn [Button]
```
