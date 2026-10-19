# Integração de arte — contrato da Sprint 01

Arte real ainda pendente. As cápsulas e formas geométricas são fixtures técnicas, sem aprovação visual.

## Entrega Meshy Pro → Blender → Unity

- `Assets/Art/Models/Source/`: arquivos de autoria e referência. Armazenar `.blend` arquivado como `.blend.zip` para evitar conversão automática dependente de Blender em cada máquina/build.
- `Assets/Art/Models/Exported/`: FBX exportado manualmente; escala em metros, Y para cima, frente local +Z. Aplicar transformações no Blender, conferir escala e posição dos pés na origem.
- `Assets/Art/Materials/`, `Textures/`, `Animations/`, `Shaders/`: separar materiais, texturas pintadas, clips e shaders. Não embutir materiais duplicados em cada FBX.
- Antes do primeiro FBX/BLEND/texturas grandes: instalar Git LFS e habilitar as regras existentes em `.gitattributes`.
- Não há importador automático que modifique cenas, prefabs, rigs ou regras de combate.

## Prefabs de personagens

`Assets/Prefabs/Characters/PF_Bastiao.prefab`: raiz com CharacterController, AethelPlayerController, AethelHealth e AethelArtBindings. `VisualRoot` contém Animator (root motion desligado), child visual substituível, WeaponSocket, ShieldSocket e HitOrigin. GroundCheck permanece na raiz. O motor gira somente VisualRoot; colisão e coordenadas de gameplay continuam independentes da arte.

`Assets/Prefabs/Characters/PF_LoboEtereo.prefab`: raiz com CharacterController, AethelWolfAI, AethelHealth e AethelArtBindings. `VisualRoot` contém Animator, child visual e BiteOrigin. GroundCheck permanece na raiz. Alvo do lobo e câmera do Bastião são referências da instância da cena; o prefab independente não referencia objetos externos. Ao instanciar um lobo fora desta arena, chamar `Configure(playerTransform)`.

Substituir apenas `Visual_PLACEHOLDER...`, preservando VisualRoot, sockets e origens. Ao receber rig, conectar sockets aos ossos pela edição do prefab e atualizar as referências de AethelArtBindings. Se o Animator precisar estar no rig, mover o componente e atualizar essa referência. Não adicionar controladores alternativos, Rigidbody dinâmico ou colliders aos meshes visuais. Ajustar CharacterController na raiz ao volume de gameplay desejado, sem copiar automaticamente o tamanho do modelo.

Um prefab final pode substituir o prefab de origem mantendo componentes, campos e GUID do asset de prefab existente. Conferir os overrides da instância; não apagar/recriar o `.meta` para atualizar um modelo.

## Prefabs de ambiente

`PF_BosquePilar_01`, `PF_BosqueRocha_01`, `PF_BosqueArvore_01` em `Assets/Prefabs/Environment/` são fixtures de integração. O child visual não contém collider. `Collision` contém um collider simples e separado. Substituir o visual sem alterar a área navegável silenciosamente. Não considerá-los árvores/rochas/pilares finais.

## Inspector e validação em jogo

1. AethelArtBindings exibe referências de Animator, VisualRoot, sockets, GroundCheck, origem de dano e collider, e informa falta de rig/controller. GroundCheck é um ponto reservado para integração; o motor usa `CharacterController.isGrounded`.
2. AethelPlayerController expõe câmera, hit origin e attack layers. Câmera da instância deve ser a IsometricCamera. No prefab sem instância, a resolução por Camera.main é o fallback existente.
3. AethelWolfAI expõe target, visual root, BiteOrigin, bite radius e attack layers. Conferir o raio de mordida no tamanho real do rig.
4. Dano pertence somente a `AethelPlayerController.Strike` e à conclusão do windup de `AethelWolfAI`, via `AethelHealth.TakeDamage`. Não adicionar Animation Events que causem dano. Um alvo com vários colliders recebe somente uma aplicação por golpe do jogador.
5. Conferir layers após importar: máscara deve incluir os alvos pretendidos e excluir efeitos/cenário quando as layers definitivas forem criadas. A máscara atual conserva o comportamento da fundação.
6. Testar idle/movimento/dash/ataque/windup/mordida/hit/morte; i-frames, stamina, cooldown e paredes; comparar cena antes/depois de fechar/reabrir/build.

## Materiais

`Aethel/Painted URP` oferece albedo pintado, tint, cor de sombra e faixas de luz, com sombra direcional, luzes locais e fog. É um ponto de partida original, sem pacote pago. Os materiais da arena usam esse shader. Texturas pintadas, UVs, rig e animações expressivas continuam dependendo da entrega artística. O baseline Windows usa Forward/D3D11, render scale 1, sombras direcionais a 30 m, duas cascatas e atlas 1024. Perfil de performance no Xeon/RX 580 continua sendo um critério de teste, não uma garantia.

## Próximos sistemas

Manter os contratos de [ROGUELITE_ARCHITECTURE.md](ROGUELITE_ARCHITECTURE.md): GameFlowManager orquestrará RunManager e RoomFlowController; RunState será dado puro; BoonSystem consumirá eventos de combate; MetaProgressionService/SaveService não dependerão de Animator ou prefabs. AethelHealth continua autoridade de morte. Nesta sprint esses sistemas permanecem planejados, sem classes vazias. Primeiro validar três salas autorais; procedural por seed virá depois.
