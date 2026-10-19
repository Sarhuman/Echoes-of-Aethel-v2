# Contrato de integração de arte — Meshy Pro → Blender → Unity URP

**Projeto:** Echoes of Aethel v2 · **Status:** especificação para implementação · **Data:** 19/10/2026  
**Referência visual:** Hades (2020), apenas como benchmark de acabamento/legibilidade, com arte original.

## Estrutura oficial

```text
Assets/
  Art/
    Models/
      Source/            # Meshy original e Blender de trabalho (preferir fora do Assets se não for importável)
      Exported/          # FBX definitivo para Unity
    Materials/
    Textures/
    Animations/
    Shaders/
  Prefabs/
    Characters/
      PF_Bastiao.prefab
      PF_LoboEtereo.prefab
    Environment/
      PF_BosquePilar_01.prefab
      PF_BosqueRocha_01.prefab
      PF_BosqueArvore_01.prefab
```

**Pastas e prefabs acima são caminhos-alvo, não afirmações de arquivos já existentes.** O Unity deverá criar seus próprios `.meta`, preservados no Git. Evitar comitar arquivos intermediários enormes ou originais duplicados dentro do Assets: se necessário, manter fonte em repositório/armazenamento próprio e registrar caminho, licença e hash. O contrato exige uma única fonte canônica por asset.

## Responsabilidades
- **Meshy Pro:** gerar base de modelos originais e texturas; exportar manualmente. Não presumir acesso à API Meshy pago separadamente.
- **Blender (versão estável fixada):** corrigir malhas, escalas, UV, retopologia, materiais, pivôs, rigging, animação e LOD, quando necessário. O aumento de cabeça/mãos/armas deve ser **testado em câmera**, não imposto uniformemente.
- **Unity 6 URP:** importar FBX, criar materiais, Animator Controller, prefabs, colliders simplificados, iluminação e integração; preservar scripts/ID e cena.
- **Codex:** adicionar pontos de conexão opcionais aos componentes atuais, com testes, sem criar um segundo player, câmera ou Health.

## Convenções de importação
- Unidade: 1 Unity unit = 1 metro; origem do ator centrada no solo; +Y para cima na Unity; frente do ator coerente com +Z.
- No Blender, aplicar transformações cuidadosamente **antes** de rigging/export, evitando destruir animações; usar orientação de exportação FBX testada para Unity (`Forward: -Z; Up: Y` é ponto de partida, não garantia).
- Rig humanoide do Bastião: Humanoid se avatar mapeia corretamente, senão Generic; Lobo Etéreo: Generic, salvo justificativa técnica documentada.
- Clips mínimos Bastião: Idle, Run, Dash, Attack_01..03, Hit, Death. Lobo: Idle, Walk, Run, Telegraph, Bite, Hit, Death. **Nenhum clip é considerado criado por este contrato.**
- Para locomotion do protótipo com CharacterController, iniciar com root motion **desativado**; manter movimento autoritativo no controlador. Só alterar mediante decisão e testes.
- Os FBX finais ficam em `Assets/Art/Models/Exported/`, com materiais Unity em `Assets/Art/Materials/`, texturas originais `Assets/Art/Textures/`.
- Adicionar Git LFS **antes** do primeiro lote de arquivos grandes; verificar `git lfs install`, `git lfs track`, regras `.gitattributes` e cotas antes de enviar. Não reescrever histórico sem autorização.
- Registre autoria, prompt/base e condições de licenciamento de cada asset. Nunca copiar modelos/visuais distintivos de Hades.

## Integração sem destruição
1. Validar imports e rigs no Unity Editor.
2. Abrir prefab em Prefab Mode; trocar **apenas** o objeto visual do `VisualRoot`.
3. Manter no root componentes de gameplay e CharacterController, independentemente de aparência.
4. Retargetar os sockets de armas/escudo às bones/mãos do rig final; se a rig/bone hierarchy mudar, executar revalidação. Não presumir que a substituição de mesh preserva sockets de um rig incompatível.
5. Reutilizar colliders simples do `ColliderRoot`/root; não fazer física a partir de malha detalhada por padrão.
6. Salvar prefab e cena existentes. Builds não criam ou reescrevem cenas.
7. Validar no runtime da arena e em build Windows; capturas de mockup não contam como evidência.

## Aceitação mínima de cada entrega
- [ ] FBX importa com orientação, escala e pivô corretos.
- [ ] Materiais URP funcionam sem shader rosa ou texturas perdidas.
- [ ] Prefab aplica Animator/Avatar válido e clips executam.
- [ ] Sockets seguem bones durante animação; nenhum deslocamento da arma/escudo.
- [ ] Collider e CharacterController mantêm movimentação, ataque e esquiva.
- [ ] Nenhuma alteração não intencional em `.unity`, `.prefab`, `.meta`.
- [ ] Gameplay legível na câmera fixa, sob luz real da cena.
- [ ] FPS/frame time e RAM/VRAM medidos em build Windows; meta 60 FPS a 1080p é hipótese de qualidade, **não desempenho comprovado**.

## Ordem de produção
1. Bastião com silhueta aprovada em Game View.
2. Lobo Etéreo com animações/telegraph legíveis.
3. Kit pequeno Bosque: pilar, rocha, árvore + piso autoral.
4. Substituição de placeholders e revisão de render/impactos.
5. Somente após aprovação: novas salas, novas criaturas e biomas.
