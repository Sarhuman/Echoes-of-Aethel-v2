# Roadmap de produção — Echoes of Aethel v2

## Política
PRs pequenos, sempre com objetivo, screenshots do runtime quando visual, teste manual reproduzível e plano de rollback. A branch `main` é estável. O PR #1 contém código inicial ainda **não validado no Unity Editor**. Este roadmap não declara features implementadas.

| Ordem | Entrega | Aceitação objetiva | Dependência |
| --- | --- | --- | --- |
| 0 | Importar template Unity 6 Universal 3D, URP/Input System e arena do PR #1 | Sem erros de compilação, cena preservada após build, movimento/dash/ataque/IA testados | PR #1 |
| 1 | **Vertical slice artístico**: Bastião/Lobo com rig, 1 arena pintada | Capturas/vídeo no runtime 1080p; personagens legíveis; sem placeholders na entrega artística | 0 |
| 2 | `RunState`, `RunManager`, `GameFlow` e seed | Testes de transição + determinismo; nenhuma gravação destrutiva de cena | 0 |
| 3 | Três salas autorais encadeadas e escolha de recompensa | Entrar → limpar → escolher → próxima → encerrar → hub; prêmio por sala uma vez | 2 |
| 4 | Sistema de bênçãos (três originais) e combo/dash-cancel | Regras isoladas; efeitos temporários; telegraphs e ataque verificáveis | 1,3 |
| 5 | MetaProgression + SaveService v1 offline | Resultados idempotentes; teste corrupção/recuperação; preservação entre runs | 3 |
| 6 | Seleção procedural de módulos por seed | Catálogo validado, room plan reproduzível, sem sobrescrever cenas | 3,5 |
| 7 | HUD final, feedback de combate, áudio e performance | Build Windows; perfil de frame-time, RAM e VRAM em RX 580; sem bugs bloqueantes conhecidos | 1–6 |

## Fluxo proposto de PRs
- **PR #1:** base Unity/arena (já aberto, draft).
- **PR de direção roguelite:** este conjunto de documentos, empilhado sobre #1 até ele ser mesclado.
- **PR seguinte A:** template URP real + Editor/Windows smoke.
- **PR seguinte B:** run state + testes, sem alterar cena.
- **PR seguinte C:** salas e recompensas, sem procedural ainda.
- **PR seguinte D:** boons / metaprogressão.
- **PR seguinte E:** gerador de plano de salas por seed.
- **PRs de arte separados:** rig/modelos/ambiente, aprovados em runtime.

## Critérios de bloqueio de merge
Erros no Console, destruição de cena durante build, referências Unity faltando, regressão de movimento/dash, runs duplicando recompensas, protótipo mostrado como arte final, ausência de capturas reais em PR visual.

## Primeiro trabalho para Codex
Ao integrar o template URP, corrigir somente bloqueadores de compilação/importação, adicionar validação mínima da arena **sem gerar a cena novamente**, documentar Unity editor exato/Package manifest, validar input, colisões, Lobo e build Windows. Depois entregar a primeira arena visualmente convincente antes de escalar conteúdo.
