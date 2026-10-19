# Echoes of Aethel v2 — visão oficial (roguelite)

**Decisão:** 19/10/2026 · **Status:** normativa · **Responsável:** proprietário do projeto

## Premissa
Action roguelite single-player, offline, de combate rápido e salas interligadas, com câmera isométrica fixa e forte **referência de linguagem artística e legibilidade** em [Hades (2020)](https://store.steampowered.com/app/1145360/Hades/). Personagens, histórias, mapas, ícones, efeitos e modelos serão originais. **Não copiar** conteúdo reconhecível, nomes, interfaces ou imagens do jogo de referência.

**Tecnologia:** Unity 6 estável, **Universal Render Pipeline (URP)**, 3D em tempo real com apresentação 2.5D isométrica; PC Windows primeiro; Meshy Pro → Blender → prefabs Unity para arte. Não voltar a Godot ou Unreal durante este ciclo.

## Fantasia e identidade
**Bastião do Crepúsculo** é o primeiro personagem jogável. A **Cidadela dos Últimos** é o hub permanente. O jogador explora incursões ao **Bosque das Névoas** e, em etapas futuras, regiões originais corrompidas pela Bruma Estelar. **Lobo Etéreo** é o inimigo inicial. Vael, o Coração de Éter e a decadência moral podem ser reinterpretados na estrutura de runs; a campanha antiga **não** é automaticamente importada.

## Core loop oficial
```text
Cidadela (hub + melhorias permanentes)
  → escolher arma/caminho de incursão
  → iniciar run com seed e estado efêmero
  → sala: combate → recompensa → escolher próxima sala
  → bênçãos e sinergias durante a run
  → chefe / derrota / retirada permitida pelas regras
  → resultado da run + recursos persistentes
  → Cidadela (novos desbloqueios, diálogo, próxima tentativa)
```

## Divisão entre persistente e temporário
| Entre runs (persistir) | Dentro da run (descartar ao encerrar) |
| --- | --- |
| Desbloqueios de armas e melhorias | Bênçãos e níveis temporários |
| Moeda de metaprogressão validada | Moeda consumível específica da run |
| Conhecimento, progresso narrativo e opções do hub | Salas visitadas, HP e buffs temporários |
| Estatísticas de runs e conquistas próprias | Inimigos, projéteis, condições temporárias |

A conclusão ou derrota de uma run **não deve conceder recursos duas vezes** quando salva ou processada novamente. Separar explicitamente dados `MetaProgression` de `RunState`.

## Combate pretendido
- Movimento 8-direções relativo à câmera, ataque básico em combo de 3 golpes, ataque especial, cast, dash-cancel e golpe de patrono (*Call*) como metas progressivas.
- Ataques legíveis com antecipação, contato e recuperação; os eventos de dano pertencem ao sistema de combate, não ao Animator.
- I-frames com prazos acumuláveis; dash atravessar unidades hostis apenas se a política de colisão explicitamente o permitir. **Não atravessar paredes.**
- Primeiro protótipo conserva os valores atuais como **experimentais**: dash ~0,20s, invulnerabilidade 0,12s, stamina 25 e recarga 1,2s. Não duplicar controladores.

## Bênçãos originais para primeira prova
- **Brasa do Éter:** aplica dano ao longo do tempo no acerto.
- **Arco da Tempestade:** após dash, descarga secundária com limite de alvos e recarga própria.
- **Geada da Bruma:** acumula lentidão; congelamento com teto de frequência/duração em elites e chefes.
Nomes, arte, lore, combinações e balanceamento deverão ser originais. Não replicar literalmente deuses, ícones ou regras de Hades.

## Estrutura inicial de salas
Uma **sequência determinística de 3 salas** precede qualquer algoritmo de geração procedural: sala inicial segura → arena de Lobo Etéreo → sala de recompensa/saída. Após validação de gameplay e arte: salas autorais pré-fabricadas (prefabs/cenas) selecionadas por seed, com tags, conectores e portas. **Não gerar/sobrescrever cenas Unity a cada build**; o procedural instancia conteúdo em runtime.

## O que não é meta do primeiro vertical slice
Mundo aberto, 12 mapas da v1, classes extras, multiplayer, criação automática de modelos finais por script, economia online, sistema de crafting, múltiplos biomas, vídeo cinematográfico final, seis chefes.

## Gate de qualidade
Uma arena com Bastião e Lobo **visualmente aceitáveis em gameplay** deve ser aprovada antes de multiplicar inimigos e salas. Modelos primitivos são exclusivamente engenharia provisória, nunca arte aprovada. Revisão em cena Unity com captura real, FPS e build Windows; testes não são prova de direção artística.
