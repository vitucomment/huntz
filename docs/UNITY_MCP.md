# MCP for Unity — uso no projeto Huntz

O pacote está embutido em Packages/com.coplaydev.unity-mcp, versão 10.3.0. O cliente Codex tem a conexão UnityMCP configurada fora do repositório; não copie nem publique configuração pessoal ou tokens.

## Iniciar

1. Abra o projeto atual em Unity 6000.6.4f1.
2. No Editor, abra Window > MCP for Unity.
3. Se o Bridge estiver parado, clique Start Bridge.
4. Confirme que o cliente Codex reporta o projeto atual conectado antes de editar.

O endereço local esperado é http://127.0.0.1:8080/mcp. No piloto 3D de 2026-10-07, a ponte respondeu e compilou sem erros C#. O último refresh registrou o aviso interno `[WebSocket] Unexpected receive error: WebSocket is not initialised`; a validação da cena retornou zero problemas e o Play Mode não registrou avisos de gameplay. A cena ativa do Editor e a única cena de Build Settings são `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`. Consulte `docs/HANDOFF.md` para o estado da validação e limites do teste manual.

## Rotina do agente

- Leia o estado do Editor e confirme a instância/projeto corretos.
- Antes de mutações, espere a compilação terminar e inspecione a cena atual.
- Após mudanças de scripts no disco, peça refresh_unity, aguarde is_compiling = false e leia o Console por erros C#.
- Antes de chamar menus, leia o caminho disponível e confirme o que será sobrescrito. `Huntz/Prototype 3D/Build Isometric ARPG Pilot` recria a cena e os grayboxes gerados em `Assets/Huntz/Prototype/Isometric3D`; confirme antes de sobrescrevê-los. O menu legado `Huntz > Build MainLand + Hunt Loop` regrava `Assets/Huntz/Scenes/HuntzVerticalSlice.unity` e pertence ao fluxo 2D antigo; não o use para atualizar a cena 3D.
- Use o MCP para cenas, prefabs e serialização; não edite arquivos YAML de cenas manualmente.
- Capturas do MCP vão para Assets/Screenshots; essa pasta é ignorada pelo Git e não deve ficar acumulando.

O MCP pode inspecionar runtime, mas não envia WASD, Shift, E, Espaço, J, roda do mouse ou clique ao jogo. A partida completa exige teclado/mouse manual. Para o piloto 3D: WASD/setas movem, Shift corre, mouse mira, roda do mouse ajusta o zoom, clique esquerdo/J mantidos atacam, Espaço esquiva, E interage, C alterna o visual graybox e R reinicia.
