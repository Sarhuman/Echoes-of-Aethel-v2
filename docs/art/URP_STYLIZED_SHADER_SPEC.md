# URP — especificação do shader painterly/toon

**Estado:** projeto de shader; **não** contém um `.shadergraph` implementado, e não garante compatibilidade sem versão URP fixada.

## Meta
Aproximar **princípios** visuais de Hades original (2020): silhueta forte, separação de planos, controle autoral de contraste, luz quente/fria, superfícies estilizadas. Nunca recriar shaders, símbolos, paletas ou assets proprietários de terceiros.

## Shader Graph / material
- `_BaseMap` / `_BaseColor`: albedo hand-painted, variação cromática intencional.
- `_NormalMap` quando contribuir para leitura; moderar detalhamento.
- `_RampTex` / `_ShadowTint`: transição quantizada de iluminação (2–4 bandas como ponto de teste).
- `_RimColor`, `_RimPower`, `_RimStrength`: realce controlado principalmente nos atores.
- `_EmissionMap` e `_EmissionColor`: olhos do Lobo e marcas de Éter, intensidade controlada.
- Outlines: optar por técnica URP compatível após medir custo/qualidade; não impor pós-processo/Renderer Feature antes do profiling.

## Advertências técnicas
1. Shader Graph Lit padrão **não** oferece automaticamente acesso direto ao `MainLight` para uma toon ramp arbitrária. Acesso explícito à direção/atenuação/sombra requer solução compatível com versão do URP (p.ex. Custom Function HLSL + includes de Lighting ou abordagem aproximada baseada em normal/posição). Verificar no Editor; não prometer sombras corretas sem teste.
2. Não depender de Built-in Standard shader. O projeto-alvo é Universal 3D URP e deve conter pacote URP/asset URP válidos antes de criar o grafo.
3. Emissão não deve tornar o Lobo ou os perigos ilegíveis; rim não deve criar um contorno permanente saturado.
4. Priorizar materiais URP/Lit e texturas painterly que **já renderizem corretamente** antes de adicionar toon avançado. Um shader experimental quebrado não bloqueia o vertical slice.
5. Preservar shader fallback/documentar versão e opção de desabilitar outline/rim em hardware-alvo.

## Critérios de aceitação
- Capturas 1080p da Game View com atores, piso e ruínas sob luz variada.
- Nenhuma superfície rosa, sem flicker na câmera fixa, sem regressão de contraste no telegraph.
- Desempenho comparativo shader padrão vs shader estilizado no build Windows RX 580.
- Materiais convertidos de forma explícita, sem sobrescrever padrões globais.
