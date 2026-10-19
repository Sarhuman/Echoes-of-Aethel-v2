# Integração de arte — contrato da Sprint 01

Os oito pacotes Meshy recebidos em 19/10/2026 têm modelos e materiais integrados em prefabs. O Lobo recebeu [rig e seis clips FK iniciais com telegraph](../validation/wolf/README.md). A aprovação artística continua pendente, incluindo animações de combate do Bastião e revisão autoral da arena. A [primeira inspeção](../validation/assets/README.md) registra o estado anterior ao rig.

## Entrega Meshy Pro → Blender → Unity

- `Assets/Art/Models/Source/`: arquivos de autoria e referência. Armazenar `.blend` arquivado como `.blend.zip` para evitar conversão automática dependente de Blender em cada máquina/build.
- `Assets/Art/Models/Exported/`: FBX exportado manualmente; escala em metros, Y para cima, frente local +Z. Aplicar transformações no Blender, conferir escala e posição dos pés na origem.
- `Assets/Art/Materials/`, `Textures/`, `Animations/`, `Shaders/`: separar materiais, texturas pintadas, clips e shaders. Não embutir materiais duplicados em cada FBX.
- Git LFS está habilitado para FBXs exportados e PNGs de arte. Executar `git lfs install` e `git lfs pull` antes de abrir um clone novo.
- Não há importador automático que modifique cenas, prefabs, rigs ou regras de combate.

## Prefabs de personagens

`Assets/Prefabs/Characters/PF_Bastiao.prefab`: raiz com CharacterController, AethelPlayerController, AethelHealth e AethelArtBindings. `VisualRoot/Model_Bastiao` contém o Animator Humanoid, com root motion desligado. WeaponSocket está na RightHand; ShieldSocket na LeftHand. HitOrigin permanece diretamente em VisualRoot, independente dos ossos/animações, e GroundCheck permanece na raiz. O motor gira somente VisualRoot; colisão e coordenadas de gameplay continuam independentes da arte.

`AethelLocomotionVisual` lê velocidade do CharacterController, dash e health existentes; não move objetos nem aplica dano. Reproduz somente o clip Walking fornecido. Parado, durante dash ou após morte, segura o primeiro frame desse clip: não representa animações Idle/Dash/Death prontas. Ao trocar o child do rig, reassociar Animator em AethelArtBindings e AethelLocomotionVisual e reconectar os sockets aos ossos equivalentes; não refazer código de gameplay.

`Assets/Prefabs/Characters/PF_LoboEtereo.prefab`: raiz com CharacterController, AethelWolfAI, AethelHealth e AethelArtBindings. `VisualRoot` mantém BiteOrigin, AethelWolfVisual e AttackTelegraph; Model_LoboEtereo contém skin de 24 ossos e Animator Generic/AC_LoboEtereo, sem root motion e com AlwaysAnimate. GroundCheck permanece na raiz. Alvo do lobo e câmera do Bastião são referências da instância da cena; o prefab independente não referencia objetos externos. Ao instanciar um lobo fora desta arena, chamar `Configure(playerTransform)`.

AethelWolfVisual lê progresso de windup/recuperação, velocidade e health; não controla combate. Windup segue o timer existente, Bite aparece no término e as linhas não têm collider. Ao trocar o child do rig, reassociar Animator no presenter e em AethelArtBindings, mantendo BiteOrigin fora dos ossos. Os clips nunca aplicam dano por Animation Events. O Inspector mostra fase, aviso e progresso durante Play.

Substituir apenas o child `Model_...`, preservando VisualRoot, sockets e origens. Preservar ou reassociar AethelLocomotionVisual ao novo Animator. Ao receber outro rig, conectar sockets aos ossos pela edição do prefab e atualizar as referências de AethelArtBindings. Não adicionar controladores alternativos, Rigidbody dinâmico ou colliders aos meshes visuais. Ajustar CharacterController na raiz somente após revisão do volume de gameplay desejado, sem copiar automaticamente o tamanho do modelo.

Um prefab final pode substituir o prefab de origem mantendo componentes, campos e GUID do asset de prefab existente. Conferir os overrides da instância; não apagar/recriar o `.meta` para atualizar um modelo.

## Prefabs de ambiente

`PF_BosquePilar_01`, `PF_BosqueRocha_01`, `PF_BosqueArvore_01` em `Assets/Prefabs/Environment/` usam as malhas reduzidas recebidas. O child visual não contém collider. `Collision` mantém os colliders da fundação, sem alteração; conferir manualmente o encaixe da arte com seus volumes antes de aceitar a arena visual. `PF_BosquePiso_01` é um módulo separado, com mesh e BoxCollider em Collision, ainda não posicionado na arena. Para alinhar sua superfície em y=0, posicionar a raiz aproximadamente em y=-0,125.

## Inspector e validação em jogo

1. AethelArtBindings exibe referências de Animator, VisualRoot, sockets, GroundCheck, origem de dano e collider, e informa falta de rig/controller. GroundCheck é um ponto reservado para integração; o motor usa `CharacterController.isGrounded`.
2. AethelPlayerController expõe câmera, hit origin e attack layers. Câmera da instância deve ser a IsometricCamera. No prefab sem instância, a resolução por Camera.main é o fallback existente.
3. AethelWolfAI expõe target, visual root, BiteOrigin, bite radius e attack layers. Conferir o raio de mordida no tamanho real do rig.
4. Dano pertence somente a `AethelPlayerController.Strike` e à conclusão do windup de `AethelWolfAI`, via `AethelHealth.TakeDamage`. Não adicionar Animation Events que causem dano. Um alvo com vários colliders recebe somente uma aplicação por golpe do jogador.
5. Conferir layers após importar: máscara deve incluir os alvos pretendidos e excluir efeitos/cenário quando as layers definitivas forem criadas. A máscara atual conserva o comportamento da fundação.
6. Testar idle/movimento/dash/ataque/windup/mordida/hit/morte; i-frames, stamina, cooldown e paredes; comparar cena antes/depois de fechar/reabrir/build.

## Materiais

`Aethel/Painted URP` oferece albedo pintado, tint, cor de sombra e faixas de luz, com sombra direcional, luzes locais e fog. É um ponto de partida original, sem pacote pago. Os oito materiais MT_*_Painted usam os albedos fornecidos. Metallic/Roughness/Normal permanecem preservados, mas não são consumidos por esse shader; não usar Roughness como Smoothness sem conversão. Imports: até 2048 px, mipmaps, compressão, sem leitura CPU; albedo sRGB, máscaras lineares e normal como Normal Map. O baseline Windows usa Forward/D3D11, render scale 1, sombras direcionais a 30 m, duas cascatas e atlas 1024. Perfil de performance sustentada no Xeon/RX 580 continua pendente.

Os menus `Art/Configure supplied assets`, `Audit supplied assets` e `Integrate supplied prefabs (explicit)` são ferramentas explícitas de preparação. A integração inicial recusa repetir a troca sobre o Bastião já integrado. Não executar esses menus ao abrir/clonar o projeto pronto. Não há hooks de importação/compilação/build para refazer prefabs ou cenas. `tools/art/optimize_meshy.py` requer Blender e fontes locais apenas para preparar novas exportações, nunca para jogar ou gerar build.

## Próximos sistemas

Manter os contratos de [ROGUELITE_ARCHITECTURE.md](ROGUELITE_ARCHITECTURE.md): GameFlowManager orquestrará RunManager e RoomFlowController; RunState será dado puro; BoonSystem consumirá eventos de combate; MetaProgressionService/SaveService não dependerão de Animator ou prefabs. AethelHealth continua autoridade de morte. Nesta sprint esses sistemas permanecem planejados, sem classes vazias. Primeiro validar três salas autorais; procedural por seed virá depois.
