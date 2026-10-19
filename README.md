# Echoes of Aethel — v2

**Direção oficial (19/10/2026): Action Roguelite isométrico, single-player, offline**, com apresentação Dark Fantasy estilizada. A referência principal de linguagem artística e legibilidade é [Hades (2020)](https://store.steampowered.com/app/1145360/Hades/), utilizando personagens, cenários, mecânicas concretas e identidade originais.

**Core loop:** Cidadela dos Últimos (hub e desbloqueios permanentes) → incursão em salas com inimigos e escolhas → bênçãos temporárias → derrota/vitória → resultado persistente e retorno ao hub. **Primeiro personagem:** Bastião do Crepúsculo. **Primeiro adversário:** Lobo Etéreo. **Primeiro bioma:** Bosque das Névoas.

**Tecnologia:** Unity 6 Universal 3D (URP), modelos/cenários 3D em tempo real com câmera isométrica ortográfica, Windows PC primeiro; Meshy Pro + Blender para assets originais. Sem importação indiscriminada de cenas ou código da v1.

## Documentos normativos
- [ADR-0001 — Roguelite](docs/engineering/ADR-0001-ROGUELITE.md)
- [Visão e core loop](docs/design/ROGUELITE_VISION.md)
- [Arquitetura e invariantes das runs](docs/engineering/ROGUELITE_ARCHITECTURE.md)
- [Roadmap e critérios de aprovação](docs/production/ROADMAP.md)
- [Direção artística](docs/ART_DIRECTION.md)

**Estado real:** PR #1 contém scripts iniciais e uma ferramenta de criação da arena **uma única vez**, mas ainda não configura integralmente o projeto Unity nem comprova compilação, jogabilidade ou visual final. Os sistemas `RunManager`, `BoonSystem`, `RoomFlow`, geração procedural e metaprogressão estão **planejados**, não implementados. Aprovação exige playtest na Unity e build Windows.

---

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
