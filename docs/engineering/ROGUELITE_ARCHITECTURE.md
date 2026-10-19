# Arquitetura inicial do roguelite — contratos de implementação

**Status:** design aprovado para orientar implementações; contratos ainda não implementados integralmente.

## Boundaries

```text
GameFlowManager   [Hub | PreparingRun | InRoom | RewardChoice | RunFinished]
      |                          |
      +--> RunManager --------> RoomFlowController
      |        |                  |     |
      |        |                  |     +--> EncounterDirector / DoorController
      |        |                  +--> RoomRewardPresenter
      |        +--> RunState / SeededRoomPlan / ActiveBoons
      +--> MetaProgressionService
                   |
             SaveService (atomic local persistence, versioned)
```

- **RunManager:** inicia uma run com ID e seed, controla estado/encerramento; não instancia prefabs nem lê input.
- **RoomFlowController:** orquestra entrada, limpeza, recompensa e saída; rejeita múltiplas recompensas por sala.
- **RoomPlanGenerator:** algoritmo puro e determinístico `seed + catálogo + regras → plano`. Nenhum `UnityEngine.Random` global; não depende de ordem não determinística de `Dictionary`.
- **EncounterDirector:** coordena spawn e detecção de conclusão; a morte é decidida pelo `AethelHealth` existente até eventual migração controlada.
- **BoonDefinition (ScriptableObject):** ID estável, raridade, condições de elegibilidade, descrição localizada e modificadores. **BoonRuntime:** efeitos ativos por run, sem alterar permanentemente definições.
- **MetaProgressionService:** aplica `RunResult` uma única vez por ID de run concluída. Adota contabilidade idempotente (IDs já processados).
- **SaveService:** grava progresso persistente de forma versionada e atômica. Gravador tem estratégia de recuperação; falhas não duplicam recompensas.
- **UI:** consome eventos/estado; não determina autoridade de dano ou de moeda.

## Máquina de estados

```text
Hub --StartRun--> PreparingRun --Ready--> InRoom
InRoom --EnemiesCleared--> RewardChoice
RewardChoice --RewardCommitted--> InRoom [próxima sala]
InRoom --Defeat/FinalBoss--> RunFinished --RecordOnce--> Hub
```

Transições inválidas devem ser rejeitadas; não gerar prêmio por transição repetida; retorno ao hub não substitui commit de resultado. Cancelamentos e falhas no carregamento devem ter recuperação explícita.

## Identificadores e invariantes
- `runId`: estável para toda incursão; `roomId`: estável dentro da run; `rewardChoiceId`: estável por conjunto de recompensa.
- Cada sala concluída pode registrar **uma** escolha de recompensa, no máximo.
- O mesmo `RunResult` só incrementa progresso permanente uma vez.
- `RunState` não carrega referências de `GameObject`; é serializável com dados puros.
- IDs de bênçãos são estáveis; ao mudar catálogos, migração ou fallback explícito.
- RNG isolado, seed registrada e testes de repetição.
- Procedural escolhe e instancia salas existentes, jamais regrava arquivos `.unity`.

## Política de save
A v2 é offline. Persistir progresso entre runs em `Application.persistentDataPath`; esquema versionado; salvar em arquivo temporário, validar, substituir atomicamente quando suportado e manter backup recuperável. Não prometer atomicidade universal antes da prova por plataforma. Decidir posteriormente se run ativa é retomável; até lá não declarar suporte a suspensão.

## Testes de aceitação mínimos
1. Dada a mesma seed e catálogo ordenado, gerar sempre mesmo plano.
2. Seeds distintas geram planos válidos, sem conexões sem saída não autorizadas.
3. Vencer sala duas vezes não duplica recompensa.
4. Reprocessar resultado de run não duplica metamoeda.
5. Saída/derrota retorna a hub com estado consistente.
6. Bênção de run não persiste no próximo início.
7. Fechar e reabrir preserva metaprogressão; save corrompido falha com aviso e recuperação.
8. Nenhuma fase de build altera `.unity`, `.prefab` ou `.meta`.
9. Movimento, dash, vida e IA existentes do PR #1 não sofrem regressões.

## Integração em etapas
**A:** provar o projeto URP importado e arena compilada. **B:** estados puros e determinismo, com testes edit-mode. **C:** 3 salas fixas e escolha 1/3 recompensas. **D:** bênçãos e metaprogressão; **E:** seleção seeded de módulos; **F:** polishing, UI e produção visual. Não implementar tudo em um PR.
