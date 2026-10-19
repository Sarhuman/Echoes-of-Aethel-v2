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

**Sprint 01:** integração do template Universal 3D instalado, configurações de Windows, correções nos componentes existentes e contrato de prefabs para arte. [Evidências e limites da validação](docs/validation/SPRINT01.md). Os sistemas `GameFlowManager`, `RunManager`, `RunState`, `RoomFlowController`, `BoonSystem`, `MetaProgressionService` e `SaveService` continuam **planejados**. Placeholders não representam arte aprovada.

---

## Abrir e testar
0. Após clonar ou trocar para a branch de arte, executar `git lfs install` e `git lfs pull` para obter os FBXs e PNGs reais antes de abrir o Unity.
1. Usar **Unity 6000.6.6f1**, com Windows Build Support. Adicionar a raiz deste repositório no Unity Hub; não criar outro projeto nem copiar conteúdo da v1.
2. Aguardar a importação dos pacotes fixados: **URP 17.6.0**, **Input System 1.20.0**, **Test Framework 1.8.0**. Active Input Handling já usa o Input System novo.
3. Abrir `Assets/Scenes/Aethel_FirstArena.unity` e entrar em Play. **WASD/setas** movem; **Space/Shift** executam dash; **mouse esquerdo/J** atacam. Gamepad: stick esquerdo, botão South para dash e West para ataque.
4. No Test Runner (`Window > General > Test Runner`), executar EditMode e PlayMode. Consultar o relatório para distinguir testes automatizados de revisão manual.
5. **Echoes of Aethel > Validate saved arena** valida referências sem criar conteúdo. **Build Windows (preserve content)** produz `Builds/Windows/EchoesOfAethel.exe` e verifica SHA256 de cenas, prefabs e metas. Build de desenvolvimento x64/Mono/D3D11; sem dependência de IL2CPP nesta sprint.
6. Rodar o executável para avaliação manual. O modo opcional `-aethel-smoke -logFile <caminho>` simula movimento/dash, grava `arena-runtime.png` na pasta do executável e encerra com código 0/1. Não é benchmark de performance.

O gerador **Create First Arena (one time)** é uma ação inicial explícita: recusa sobrescrever a cena e protege prefabs de personagens existentes. Nunca é chamado por importação, compilação ou build. Não o executar para integrar arte: editar os prefabs e a cena existente.

## Integração Meshy Pro / Blender

Seguir [ART_INTEGRATION.md](docs/engineering/ART_INTEGRATION.md). Modelos e texturas em `Assets/Art/`; personagens em `Assets/Prefabs/Characters/`; ambiente em `Assets/Prefabs/Environment/`. Substituir somente o child visual, preservando raiz física, VisualRoot, sockets, origens de dano e GUIDs. `AethelArtBindings` mostra os pontos de integração no Inspector. O shader original `Aethel/Painted URP` não exige assets pagos.

Os oito pacotes recebidos em 19/10/2026 têm inspeção e redução de malhas em [docs/validation/assets](docs/validation/assets). O [Lobo agora tem rig e seis clips FK iniciais, com telegraph ligado à IA](docs/validation/wolf/README.md); Bastião continua com caminhada apenas. Integração técnica não significa aprovação de arte ou animação. O piso tem prefab separado para posicionamento autoral; o chão físico atual da arena é preservado.

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
- Git LFS is enabled for exported FBXs and art PNGs; use `git lfs pull` after cloning.
- Work in focused PRs; validate playability and visual quality in actual Unity Game view.
- Preserve original lore from v1 as reference only, after selective review. No bulk copying of legacy runtime.
