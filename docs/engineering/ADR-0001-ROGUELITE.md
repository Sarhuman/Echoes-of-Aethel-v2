# ADR-0001 — Formato roguelite como direção oficial

**Data:** 19/10/2026  
**Estado:** Aprovada pelo proprietário do projeto.

## Contexto
A versão anterior concentrou sistemas de campanha extensa e trocou engines várias vezes, enquanto o primeiro slice visual permaneceu instável. A v2 nasce como projeto independente e toma o Hades original (2020) como referência artística e de clareza de combate, sem reproduzir conteúdo de terceiros.

## Decisão
- **Action Roguelite**, single-player offline.
- **Hub persistente:** Cidadela dos Últimos.
- **Runs repetíveis:** progressão de salas e recompensas, derrota/vitória com retorno ao hub.
- **Bênçãos temporárias** por run e **melhorias permanentes** no hub.
- **Unity 6 URP**, 3D em tempo real com câmera isométrica ortográfica.
- Primeiro herói: Bastião do Crepúsculo. Primeiro inimigo: Lobo Etéreo. Primeiro bioma: Bosque das Névoas.
- Salas geradas por **seleção determinística de módulos autorais**, depois de validar sequência fixa.
- Sem cópia direta de propriedade intelectual de Hades.

## Consequências
A campanha narrativa em quatro atos da v1 **não é o escopo principal da v2**. Pode fornecer lore, NPCs, motivações e conteúdo autoral após triagem, mas nenhuma importação automática de código, cenas, dados de save ou contratos. Requer novos sistemas `RunManager`, `RoomFlow`, `BoonSystem`, `MetaProgression`, `SaveService`. A arquitetura deve separar dados temporários e persistentes e proibir reconstrução destrutiva de cenas.

## Não decisões
Quantidade definitiva de biomas/bosses, nomes dos patronos, estatísticas finais, economia, lançamento mobile, coop/multiplayer e suspensão de runs ficam para ADRs posteriores. Valores citados são experimentais.
