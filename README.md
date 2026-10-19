# Echoes of Aethel — v2

**Fresh start.** Dark-fantasy isometric action RPG for Windows. Primary artistic benchmark: [Hades (2020)](https://store.steampowered.com/app/1145360/Hades/); all characters, environments and visual assets must be original.

**Technical direction:** Unity 6 **Universal 3D (URP)**, real-time 3D characters/scenery and orthographic isometric camera. First scope: a small arena with Bastion, Ethereal Wolf, movement, dash/i-frames and a basic attack. This v2 is independent from the archived v1 and does **not** import its scene builders.

## Starting in Unity (one-time)
1. Install a stable Unity 6 editor via Unity Hub, with **Windows Build Support (IL2CPP)** where needed. Record the exact installed editor version in the first project import PR.
2. Create a **Universal 3D** project *in a temporary folder*; use the editor-generated `ProjectSettings/`, `Packages/`, URP assets and Input System setup as the source of truth.
3. Copy the generated `ProjectSettings/` and `Packages/` into this repository root. Do not overwrite its `Assets/Scripts/`. Open the repository root as the Unity project in Hub.
4. Ensure `Input System` package is installed and **Active Input Handling** includes `Input System Package (New)`. Restart editor if requested.
5. Wait for compilation. Select **Echoes of Aethel > Create First Arena (one time)**. This writes `Assets/Scenes/Aethel_FirstArena.unity` *once*. Do not invoke it after art adjustments. The command refuses to overwrite an existing scene.
6. Open the scene, press Play. **WASD/arrows** move; **Space/Shift** dash; **left mouse/J** attack. Gamepad left stick moves; South button dashes; West button attacks.
7. Commit the generated `ProjectSettings/`, `Packages/`, `Assets/Scenes/`, `Assets/Materials/` and their `.meta` files in a separate integration PR. Test the Windows build in Unity.

**Current state:** this repository contains editable starter **C# code and a one-time Editor arena creator**, **not** a fully configured/compiled Unity project or a finished art/gameplay build. Runtime test in Editor and Windows export remain pending.

## Architecture
- `Assets/Scripts/Runtime/AethelPlayerController.cs`: Input System keyboard/gamepad, movement, dash, stamina and single basic melee attack.
- `AethelHealth.cs`: health with monotonic time-based invulnerability grants.
- `AethelWolfAI.cs`: simple chase, windup, contact attack and recovery.
- `AethelIsometricCamera.cs`: fixed orthographic tracking.
- `Assets/Editor/AethelFirstArenaCreator.cs`: **explicit one-time** arena generator; no generation in builds.
- `docs/ART_DIRECTION.md`: visual target and acceptance gate.

## Production rules
- Do not overwrite/regenerate Unity scenes during builds; preserve `.meta` GUIDs.
- Keep game rules, rendering, prefabs and asset pipeline separate.
- Avoid committing generated `Library/` or `Temp/`.
- Install/configure Git LFS before adding large FBX/BLEND/textures, then enable the commented rules in `.gitattributes`.
- Work in focused PRs; validate playability and visual quality in actual Unity Game view.
- Preserve original lore from v1 as reference only, after selective review. No bulk copying of legacy runtime.
