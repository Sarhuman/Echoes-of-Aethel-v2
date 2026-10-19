# Contratos de prefabs — v2 Unity URP

**Estado:** blueprint a ser materializado no Unity Editor. **Não existem prefabs reais garantidos por este documento.**

## Princípio
**Root = lógica, física e identificação; VisualRoot = aparência intercambiável.** Um FBX não deve carregar `AethelPlayerController`, `AethelHealth` ou `AethelWolfAI`. O GameObject raiz é o prefab de gameplay; modelos importados são seus filhos.

### PF_Bastiao.prefab
```text
PF_Bastiao [root, CharacterController, AethelPlayerController, AethelHealth]
  VisualRoot [Transform local (0,0,0); orientação padrão]
    SK_Bastiao [SkinnedMeshRenderer, Animator/Humanoid Avatar]
      [Rig bones / animated sockets at appropriate bones]
  GroundCheck [marcador opcional: pés]
  HitOrigin [marcador: centro/direção do golpe]
```
**Sockets:** `WeaponSocket` e `ShieldSocket` devem ficar sob ossos adequados do rig (mão direita / antebraço esquerdo), não soltos como filhos independentes de `VisualRoot`; alternativa é bind explícito de bone e socket pelo adaptador de apresentação. Trocar rigs exige novo bind.

### PF_LoboEtereo.prefab
```text
PF_LoboEtereo [root, CharacterController, AethelWolfAI, AethelHealth]
  VisualRoot [local (0,0,0)]
    SK_LoboEtereo [SkinnedMeshRenderer, Animator Generic]
  GroundCheck [marcador opcional]
  BiteOrigin [marcador de focinho; bind ao osso da mandíbula/cabeça quando animado]
```
`AethelWolfAI.Configure(playerTransform)` deve resolver o alvo após spawn. **Não persistir referência serializada ao player de uma cena específica em um prefab reutilizável.**

### PF_BosquePilar_01 / Rocha / Arvore
```text
PF_Bosque... [root, sem script de player/inimigo]
  VisualRoot
    Mesh_* [MeshFilter + MeshRenderer]
  ColliderRoot
    PrimitiveCollider [BoxCollider/CapsuleCollider/etc.]
```

## Matriz de configuração

| Componente | Referência | Situação nos scripts iniciais |
|---|---|---|
| `AethelPlayerController` | `visualRoot` | **Já existe**, serializado e opcional |
| `AethelPlayerController` | `cameraTransform`, `attackLayers` | **Já existem**; `attackLayers` requer configuração coerente |
| `AethelPlayerController` | Animator, GroundCheck, HitOrigin, WeaponSocket | **Ainda não implementados**; adicionar somente com uso efetivo e defaults seguros |
| `AethelWolfAI` | target via `Configure(Transform)` | **Já existe**; usar bind após spawn |
| `AethelWolfAI` | Animator, BiteOrigin | **Ainda não implementados**; adição incremental futura |
| `AethelIsometricCamera` | target via `Configure(Transform)` | **Já existe**; câmera deve buscar ator instanciado |
| `AethelHealth` | mesmo root de player/inimigo | **Já existe**; não adicionar Health duplicado |

**Regra:** não converter todos os marcadores em campos C# somente para atender uma tabela. Criar interface/adapter de apresentação se necessário, sem tornar rigs/Animator dependências para protótipo de lógica.

## Hierarquia, orientação e referências
- Não misturar componente com nó na representação: `CharacterController` é componente do root, não filho da hierarquia.
- `VisualRoot.localPosition = Vector3.zero` ao importar, e corrigir posição do modelo sob ele, conforme seu pivot de pé.
- CharacterController e sistema de movimento não podem ser duplicados; não adicionar Rigidbody dinâmico ao mesmo ator sem uma decisão de migração.
- Prefabs não precisam referenciar objetos de uma cena. Alvos de câmera e lobo devem ser atribuídos por composição/instanciação.
- Não usar `MeshCollider` de alta complexidade em árvore/ruína sem caso de uso/profiling.
- Layers `Player`, `Enemy`, `Environment` serão criadas/registradas pela configuração do projeto, com máscaras verificadas no Inspector.
- Nunca sobrescrever prefabs em cena por rotina de build ou regeneração.

## Validar antes do merge
1. Prefab Mode: componentes no root e IDs estáveis, referências obrigatórias sem `Missing`.
2. Trocar modelos mantendo os scripts/colisores.
3. Animator: Idle/Run/Attack/Dash/Hit/Death sem quebras; locomotion de root motion explicitamente testada.
4. Lobo: telegraph e mordida com relação espacial coerente e sem hit fantasma.
5. Câmera: `Configure` segue player instanciado, não referência fixa de cena.
6. Reabrir Unity/scene/build: nenhum objeto/prefab alterado por gerador.
7. FPS, colisões, impacto de combate e orientação verificados pelo usuário em Windows.
