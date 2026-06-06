# Detective Royale — Materials Guide

All materials use URP (Universal Render Pipeline).

## UI Materials

| File | Shader | Usage |
|------|--------|-------|
| `UI_Dark.mat` | UI/Default | Dark semi-transparent panel backgrounds |
| `UI_Highlight.mat` | UI/Default | Highlighted evidence / selected items |
| `UI_GlassPanel.mat` | Custom/UIGlass | Frosted glass effect for panels |

## Scene Materials

| File | Shader | Usage |
|------|--------|-------|
| `Scene_Floor.mat` | URP/Lit | Crime scene floor |
| `Scene_Wall.mat` | URP/Lit | Room walls |
| `Scene_EvidenceGlow.mat` | URP/Unlit + Emission | Glowing evidence objects |
| `Scene_BloodStain.mat` | URP/Lit | Blood stain decals |
| `Scene_TapeYellow.mat` | URP/Lit | Crime scene tape |
| `NPC_Suspect.mat` | URP/Lit | Generic suspect character material |
| `NPC_Witness.mat` | URP/Lit | Generic witness character material |

## Post-Processing

| File | Usage |
|------|-------|
| `PP_Investigation.asset` | Volume profile for Investigation scene (Vignette, Color Grading dark/noir) |
| `PP_Results.asset` | Volume profile for Results scene (Bloom, Color Grading warm/bright) |
| `PP_MainMenu.asset` | Volume profile for Main Menu (subtle vignette) |

## Shader Graphs (URP)

### `EvidenceGlow.shadergraph`
- Pulsing emission to highlight collectable evidence
- Parameters: `GlowColor`, `GlowIntensity`, `PulseSpeed`

### `WitnessStress.shadergraph`
- Lerps character tint between calm (white) and stressed (red)
- Parameters: `StressLevel` (0-1)

### `UIFrost.shadergraph`
- Frosted glass effect for dialogue panels
- Parameters: `BlurAmount`, `TintColor`, `Opacity`

## How to create materials
1. Right-click in `Assets/Materials`
2. `Create → Material`
3. Select shader from dropdown
4. Adjust properties in Inspector
