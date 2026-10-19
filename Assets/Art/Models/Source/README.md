# Fontes Meshy — 19/10/2026

O manifesto registra os oito ZIPs fornecidos pelo usuário e seus SHA256. Os originais permanecem em Downloads; não são modificados nem importados automaticamente. Não colocar `.blend` solto em Assets: arquivar como `.blend.zip` para evitar uma dependência de Blender na importação/build.

FBXs de jogo ficam em `../Exported`; texturas originais ficam em `Assets/Art/Textures`, com importação comprimida e limite de 2048 no Unity. O FBX original do Bastião mantém o rig e a caminhada; Lobo e machado usam as malhas fornecidas. Escudo, árvore, rocha, pilar e piso têm versões reduzidas pelo Blender 4.5.9, preservando UVs. O script explícito `tools/art/optimize_meshy.py` registra os orçamentos; não há processamento de modelos durante compilação ou build.

Dados de inspeção em `docs/validation/assets/source-inspection.json` e `optimization.json`. O manifesto e os documentos recebidos são proveniência técnica; não são scripts autorizados a alterar o projeto.
