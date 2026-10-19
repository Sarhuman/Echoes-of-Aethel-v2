<div align="center">

# ECHOES OF AETHEL
### A dark-fantasy isometric action roguelite

**Unity 6 · Universal Render Pipeline · Windows PC · Single-player**

*Every return to the Citadel begins another descent into the mist.*

[Visão do jogo](#visão-do-jogo) · [Estado do desenvolvimento](#estado-do-desenvolvimento) · [Executar](#executar-o-projeto-em-desenvolvimento) · [Roadmap](#próximas-entregas) · [PRs](#pull-requests-e-evidências)

</div>

> [!IMPORTANT]
> **Projeto em desenvolvimento — pré-alfa.** A branch `main` serve de porta de entrada pública. Em **19/10/2026**, o projeto Unity e os assets integrados estão em **pull requests ainda abertos**, não na `main`. Para executar o protótipo, consulte o [PR #15](../../pull/15) e suas dependências. Não há build público final nem aprovação artística da arena.

## Visão do jogo

**Echoes of Aethel v2** é um **action roguelite isométrico** de fantasia sombria, com combate responsivo, salas interligadas, bênçãos temporárias e progressão permanente entre incursões. O projeto usa uma apresentação **2.5D**: personagens e ambientes 3D renderizados em tempo real sob câmera ortográfica isométrica.

O jogo se inspira em **princípios** de legibilidade, contraste, enquadramento e impacto do [Hades original (2020)](https://store.steampowered.com/app/1145360/Hades/), com **mundo, personagens, arte, interfaces e conteúdo originais**.

| Pilar | Direção |
| :--- | :--- |
| **Formato** | Action Roguelite, single-player, offline |
| **Hub** | Cidadela dos Últimos — recursos e desbloqueios persistentes |
| **Primeiro herói** | Bastião do Crepúsculo |
| **Primeiro adversário** | Lobo Etéreo |
| **Primeiro bioma** | Bosque das Névoas |
| **Combate** | Movimento em oito direções, ataque, dash, stamina e i-frames; combos e bênçãos são metas posteriores |
| **Tecnologia** | Unity **6000.6.6f1**, URP **17.6.0**, Input System **1.20.0** nas branches de desenvolvimento |
| **Arte** | Meshy Pro → Blender → Unity; modelos/rigs originais, materiais URP estilizados |
| **Alvo inicial** | Windows PC, 1080p; 60 FPS é objetivo sujeito a profiling, não resultado garantido |

### Core loop projetado

```text
Cidadela dos Últimos (hub)
          ↓
Iniciar incursão / run
          ↓
Salas autorais → combate → escolha de recompensa
          ↓
Bênçãos e combinações temporárias
          ↓
Derrota ou vitória
          ↓
Recursos persistentes / melhorias no hub
          ↺
```

**Atenção:** o diagrama descreve a **visão de produto**. RunManager, gerador procedural, bênçãos, metaprogressão e save definitivo **ainda não compõem um ciclo de jogo completo** nas entregas verificadas. A primeira meta é validar três salas autorais antes de criar seleção procedural por seed.

## Estado do desenvolvimento

**Snapshot do GitHub em 19/10/2026.** Os resultados abaixo são apresentados nos relatórios e descrições dos PRs; não substituem revisão manual independente. Nenhum dos PRs #1, #2, #9, #10–#15 está mesclado na `main` nesta data.

| Área | Situação comprovada nas branches de desenvolvimento | Falta concluir |
| :--- | :--- | :--- |
| **Fundação Unity** | Projeto URP configurado; primeira arena salva; build Windows e smoke tests registrados no [PR #10](../../pull/10) | Integração definitiva e revisão de merge |
| **Controle e combate** | Movimento, dash, stamina, ataque básico, vida, câmera isométrica e IA do Lobo com testes | Combos, especial, cast, ultimate e refinamento da sensação de combate |
| **Assets Meshy/Blender** | Bastião, Lobo, equipamento e parte do Bosque integrados como assets e prefabs no [PR #11](../../pull/11) | Aprovação de silhueta, acabamento, animações e composição de cenário |
| **Lobo Etéreo** | Rig de 24 ossos; seis clips iniciais e correções incrementais de skin/mechas nos [PRs #12–#15](../../pull/15) | Polimento artístico e validação visual de todos os movimentos |
| **Bastião** | Modelo importado, Avatar Humanoid e caminhada descritos no [PR #11](../../pull/11) | Conjunto completo de animações e validação de armas/sockets |
| **Arte da arena** | Shader autoral `Aethel/Painted URP`, materiais, prefabs de ambiente e piso modular registrados | Ambiente final, composição, iluminação e desempenho com assets finais |
| **Sistema roguelite** | [Decisão e arquitetura documentadas no PR #2](../../pull/2) | Runs completas, recompensas, bênçãos, metaprogressão, save e procedural |
| **Estabilidade** | [PR #15](../../pull/15) informa **27/27 EditMode**, **17/17 PlayMode**, build e smoke Windows aprovados; **127** arquivos `.unity/.prefab/.meta` monitorados sem alteração | Playtest prolongado, controle físico, profiling 1080p e avaliação visual |

### Evidências de execução

As capturas abaixo pertencem às **branches de desenvolvimento**. São imagens de execução Unity/Windows, não mockups ou arte-final:

- [Arena no executável Windows — Sprint 01](https://github.com/Sarhuman/Echoes-of-Aethel-v2/blob/feat/sprint01-playable-foundation/docs/validation/windows-runtime.png) — protótipo técnico inicial.
- [Relatório da Sprint 01](https://github.com/Sarhuman/Echoes-of-Aethel-v2/blob/feat/sprint01-playable-foundation/docs/validation/SPRINT01.md) — ambiente, testes, build e limitações.
- [Evidências do Lobo — PR #15](https://github.com/Sarhuman/Echoes-of-Aethel-v2/blob/codex/wolf-local-fur-corrections/docs/validation/wolf-local-fur/README.md) — comparativos, animações e relatórios de regressão.

## Executar o projeto em desenvolvimento

> [!WARNING]
> **Não basta clonar a `main`:** ela não contém ainda o projeto Unity. A sequência abaixo usa a branch mais recente do trabalho do Lobo, **enquanto o PR #15 estiver aberto**. Essa branch incorpora o histórico empilhado de implementação, mas ainda depende de revisão antes do merge.

Requisitos: **Unity Hub + Unity 6000.6.6f1**, módulo Windows Build Support, Git e Git LFS instalados; acesso aos arquivos LFS do repositório.

```bash
git clone https://github.com/Sarhuman/Echoes-of-Aethel-v2.git
cd Echoes-of-Aethel-v2
git lfs install
git fetch origin
git switch --track origin/codex/wolf-local-fur-corrections
git lfs pull
```

1. Abra **a raiz desse repositório** no Unity Hub, usando Unity 6000.6.6f1.
2. Aguarde a importação e confira o Console.
3. Abra `Assets/Scenes/Aethel_FirstArena.unity` e pressione **Play**.
4. Controles de protótipo: **WASD/setas** para mover, **Space/Shift** para dash, **clique esquerdo/J** para atacar; gamepad com stick esquerdo e botões South/West.
5. Execute os testes em **Window → General → Test Runner** (EditMode/PlayMode).
6. Use **Echoes of Aethel → Build Windows (preserve content)** para gerar o executável local; consulte os relatórios de validação antes de interpretar os resultados.

**Não execute novamente “Create First Arena (one time)”** para importar arte: o fluxo de integração preserva cena, prefabs, GUIDs e colisão. Prefabs de personagens são atualizados pelo `VisualRoot`, sem reconstrução automática de cenas ou alteração de dano via Animation Events.

## Arquitetura e pipeline de arte

```text
Meshy Pro ── modelos/texturas ──> Blender ── otimização/rig/animação ──> Unity URP
                                                                       │
Codex ── gameplay, testes, build e integração ──────────────────────────┘
```

- **Gameplay:** `AethelPlayerController`, `AethelHealth`, `AethelWolfAI`, `AethelIsometricCamera`.
- **Prefabs:** `PF_Bastiao`, `PF_LoboEtereo` e kit de objetos do Bosque com **lógica e visual separados**.
- **Arte:** `Assets/Art/Models/{Source,Exported}`, `Materials`, `Textures`, `Animations`, `Shaders`. FBX, imagens e fontes grandes exigem Git LFS conforme `.gitattributes` da branch.
- **Cenas:** persistentes; ferramentas de validação/build não devem recriá-las.

Documentos de referência (ainda em PRs):

- [Direção roguelite e ADR-0001 — PR #2](../../pull/2)
- [Contrato de pipeline de arte — PR #9](../../pull/9)
- [Integração técnica Meshy — PR #11](../../pull/11)
- [Integração de arte da Sprint 01](https://github.com/Sarhuman/Echoes-of-Aethel-v2/blob/feat/sprint01-playable-foundation/docs/engineering/ART_INTEGRATION.md)

## Próximas entregas

1. **Consolidar os PRs empilhados**, revisando conflitos e duplicidade documental sem perder `.meta`, GUIDs nem binários LFS.
2. **Aprovar o Bastião em jogo:** rig/Avatar, Idle, corrida, combo, dash, hit, morte, armas e sockets.
3. **Concluir o Lobo Etéreo:** avaliar telegraph, peso, contato com o chão, mechas e animações no Play Mode.
4. **Refinar uma arena do Bosque:** piso, ruínas, árvore, rocha, iluminação e clareza visual.
5. **Validar build Windows no hardware-alvo:** frame time, colisões, performance, experiência de combate.
6. **Construir a primeira run fixa de três salas:** fluxo, recompensas e retorno ao hub; depois bênçãos, salvamento e geração procedural.

### Pull requests e evidências

> Os PRs de desenvolvimento são **empilhados**, e não alterações independentes que devem ser mescladas em qualquer ordem.

| PR | Assunto | Base imediata |
| :--- | :--- | :--- |
| [#1](../../pull/1) | Fundação inicial | `main` |
| [#2](../../pull/2) | Decisão roguelite e arquitetura | #1 |
| [#9](../../pull/9) | Contratos de arte (linha documental paralela) | #2 |
| [#10](../../pull/10) | Projeto Unity validado e prefabs técnicos | #2 |
| [#11](../../pull/11) | Integração Meshy e modelos | #10 |
| [#12](../../pull/12) | Rig e animações do Lobo | #11 |
| [#13](../../pull/13) | Polimento de patas, skin e ritmo | #12 |
| [#14](../../pull/14) | Garupa, pelo e interseções | #13 |
| [#15](../../pull/15) | Correções locais das mechas do Lobo | #14 |

## Condições de qualidade

- Não considerar mockups, capturas conceituais ou placeholders como prova de gameplay ou aprovação artística.
- Não copiar assets, personagens, cenários ou elementos distintivos de jogos de referência.
- Não duplicar controladores de jogador, câmera, física e vida.
- Não sobrescrever cenas, prefabs ou `.meta` durante builds ou importações.
- Não prometer FPS sem medição real do executável com a arte carregada.
- Toda entrega precisa distinguir **implementado**, **testado**, **pendente** e **aprovado visualmente**.

---

<div align="center">

**Echoes of Aethel v2** · Desenvolvimento independente · Dark Fantasy Action Roguelite

*A primeira incursão começa no Bosque das Névoas.*

</div>
