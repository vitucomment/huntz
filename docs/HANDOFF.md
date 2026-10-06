# Handoff — Huntz

**Última atualização:** 2026-10-06 · **Por:** Codex
**Próximo agente:** leia `AGENTS.md` primeiro, depois este arquivo. Atualize este documento ao final da sua sessão.

## Resumo

O dono do projeto (Victor) quer fazer **Huntz, um RPG isométrico 2D** na Unity, desenvolvido com agentes de IA
(Claude Code e Codex) controlando o Editor via MCP. O trabalho evoluiu da exploração inicial para uma prova de conceito
vertical isolada:

1. Configurou-se a ponte agente ↔ Unity Editor (MCP for Unity + rede WSL).
2. Criou-se uma demo de plataforma 2D para demonstrar as capacidades.
3. Criou-se um **conceito jogável de RPG isométrico** — base de referência para o jogo.
4. Em `Assets/HuntzSlice/`, a prova avançou para combate, rota bifurcada, chefe e ciclo de sessão MainLand → hunt →
   retorno, com recompensa e melhoria temporárias.

Nada disso é arte ou arquitetura final: é um protótipo para validar visão e fluxo de trabalho.

## O que foi feito

### 1. Infraestrutura
- Projeto Unity criado em `C:\Users\victo\Documents\Huntz\My project` (template 3D URP, Unity 6000.6.4f1).
- Pacote **MCP for Unity 10.3.0** instalado no projeto.
- `C:\Users\victo\.wslconfig`: adicionado `networkingMode=mirrored` (backup `.wslconfig.bak`) para o WSL acessar o
  servidor em `localhost:8080`.
- Claude Code: servidor `unityMCP` registrado (HTTP, escopo local de `/home/victor/workspace/workspace/Huntz`).
- Codex: **ainda não configurado** — instruções em `docs/UNITY_MCP.md`.
- Detalhes e receitas: `docs/UNITY_MCP.md`.

### 2. Demo de plataforma 2D (referência, não é o jogo)
- Cena `Assets/Scenes/Huntz2DDemo.unity`, gerada por `Assets/Editor/HuntzDemoBuilder.cs`
  (menu **Huntz → Build 2D Demo Scene**).
- Scripts em `Assets/Scripts/`: `PlayerController2D` (coyote time, jump buffer, pulo variável), `CameraFollow2D`,
  `Coin`, `Goal`, `GameManager` (HUD `OnGUI`, R reinicia).
- Testado: compila, jogador cai e repousa no chão, sem erros.

### 3. Conceito de RPG isométrico (foco)
Cena `Assets/Scenes/HuntzIsoRPG.unity` (build index 1), gerada por `Assets/IsoRPG/Editor/HuntzIsoBuilder.cs`
(menu **Huntz → Build Isometric RPG Concept**).

| Sistema | Implementação |
|---|---|
| Mapa | grade 26x26 de tiles diamante (grama, grama escura, trilha, praça, lago, areia), flores decorativas |
| Profundidade | `YSort` — ordem de desenho pela posição Y (passa atrás de árvores/casas) |
| Colisão | árvores (borda da floresta fecha o mapa), pedras, casas, poço, baús, água (`PolygonCollider2D` em losango) |
| Jogador | `IsoPlayer` — movimento 8 direções (em espaço de tela), ataque em arco na direção, i-frames de 1s com piscar, knockback, morte = respawn na vila |
| Inimigos | `IsoEnemy` — slimes vagam perto de casa, perseguem (aggro 3, leash 6), dano por contato, flash/atordoamento/knockback ao apanhar. 5 no acampamento NE, 2 no lago, **Slime Rei** (10 HP, dano 2) |
| Progressão | XP por kill, nível (XP necessário = nível × 4), +2 HP máx por nível, +1 dano a cada nível par |
| Itens | `IsoChest` — 2 baús (2 e 3 poções); Q cura 5 HP |
| NPCs | `IsoNPC` — Anciã Ana (missão) e Ferreiro Bruno (dicas), caixa de diálogo |
| UI | `IsoGame` — HP/XP/nível/poções, prompt "[E] …", mensagens, diálogo, vitória ao matar todos (tudo em `OnGUI`) |
| Arte | Sprites pixel art finais compartilhados em `Assets/HuntzSlice/Art/Final/`, com prefabs em `Assets/IsoRPG/Prefabs/` |

**Verificado:** compila sem erros; Play mode sem exceções; jogador nasce na posição esperada; slime causou dano e
knockback no jogador num teste (contato/dano funcionam); screenshots conferidos.
**Não verificado:** entrada do teclado (combate pelo jogador, diálogo, baús, poções) — o MCP não simula teclas.
Primeira coisa a fazer na próxima sessão: pedir ao dono para jogar e reportar.

### Bugs corrigidos durante a sessão
- Slimes do lago nasciam dentro do alcance de aggro do spawn do jogador → movidos para longe.
- `IsoGame.InDialogue` ficava `true` após domain reload (array null virava vazio) → `[System.NonSerialized]` +
  checagem de índice.
- Efeito de golpe (`Slash`) aparecia no editor → renderer desabilitado no builder.

## Estado atual / pontos abertos

- A visão inicial do jogo e propostas de mecânicas foram organizadas em [`docs/GDD.md`](GDD.md), a partir de `Anotações.txt`. O documento distingue o que veio das anotações das recomendações ainda não validadas.

### Prova de conceito vertical — sala de combate

- Conteúdo novo e isolado em `Assets/HuntzSlice/`; o builder fica em `Editor/HuntzSliceBuilder.cs` e usa o menu **Huntz → Build Vertical Slice Room** para salvar `Assets/HuntzSlice/Scenes/HuntzVerticalSlice.unity`.
- A cena tem uma arena de tiles isométricos 13×13 com quatro limites físicos, o jogador, dois Ash Slimes, câmera ortográfica, HUD de vida e instruções de controle. Os sprites são placeholders gerados pelo builder em `Assets/HuntzSlice/Art/`.
- O jogador move-se em oito direções projetadas no espaço isométrico, ataca na última direção de movimento com **J/clique esquerdo** e esquiva com **Espaço** (0,16 s de deslocamento e 0,42 s de invulnerabilidade). Ao receber um golpe, há piscar visual e redução de vida; ao chegar a zero, o personagem volta ao início da arena.
- Os inimigos perseguem o jogador e exibem uma área circular vermelha pulsante por 0,78 s antes de atacar o ponto marcado. Vida, velocidade e tempos ficam ajustáveis nos componentes.
- A compilação após a última alteração terminou sem erros nem avisos C# novos. O Console manteve os erros `NoSubscription` conhecidos do Unity AI e um aviso `[WebSocket] Unexpected receive error: WebSocket is not initialised` do MCP. A remoção da captura também gerou um aviso do AssetDatabase sobre `Assets/Screenshots.meta`; a pasta e o `.meta` foram removidos e não há captura restante.
- O MCP abriu a cena em Play Mode e permitiu inspecionar hierarquia, câmera e HUD; a execução foi encerrada e os dois inimigos continuam presentes na cena salva. A captura temporária e sua pasta foram removidas. Durante parte do fluxo, `editor/state` indicou `stale_status`; no fechamento, mostrou `ready_for_tools=true`, sem compilação nem domain reload pendentes.
- **Falta validar manualmente:** movimento, direção do ataque, esquiva/i-frames e resposta aos sinais dos inimigos. O MCP não envia teclado ou mouse. Para testar, abra `HuntzVerticalSlice.unity`, entre em Play, mova com WASD/setas, ataque com J ou clique esquerdo e pressione Espaço quando a área vermelha aparecer.
- A cena nova não foi adicionada ao Build Settings; os três índices de build existentes foram mantidos.

### Prova de conceito vertical — rota de salas da hunt (Prompt 2)

- A mesma cena `Assets/HuntzSlice/Scenes/HuntzVerticalSlice.unity` agora contém uma rota fixa de seis salas, definida em
  `Assets/HuntzSlice/Editor/HuntzSliceBuilder.cs`: Entrada → Bifurcação → (desvio opcional Ninho de cinzas, com
  fragmento) ou Trilha principal → Santuário (cura na primeira visita) → Câmara final. A bifurcação e o caminho principal
  permitem voltar a salas já visitadas.
- Cada sala fica guardada na hierarquia e só uma é ativada por vez. As passagens aparecem depois que a sala é limpa;
  salas limpas mantêm o estado, não recriam inimigos, e as recompensas/cura de primeira visita não são repetidas. A sala
  final encerra a rota de teste com uma mensagem indicando que o chefe fica para uma etapa futura.
- O HUD mostra vida, sala atual, inimigos restantes, fragmentos e um mapa que revela salas conforme a visita. Controles:
  WASD/setas, J/clique esquerdo, Espaço, E perto de uma passagem.
- Corrigido o registro de salas runtime em `HuntzSliceHunt`: dicionário/lista/array de runtime são explicitamente não
  serializados e reconstituídos, evitando consulta a estado ausente após reload. Jogador e hunt também restabelecem seus
  singletons na ativação/entrada em Play; o HUD busca componentes ativos como fallback. O reset de inimigos ativos evita
  contagem residual entre execuções.
- **Verificado:** compilação final terminou; o builder salvou novamente a cena em Edit Mode; o Play Mode abriu na Entrada
  com dois inimigos, HUD/mapa visíveis e Console com 0 erros/avisos no último check. Captura de inspeção em
  `Library/HuntzSliceCaptures/stage2-verified-start.png` (não é arte final). A cena anterior `HuntzIsoRPG` e os Build
  Settings não foram alterados.
- Em uma execução anterior, a inspeção MCP encontrou `CurrentRoomId=5`, `RouteComplete=true` e 0 inimigos. Como o MCP
  não simula teclado e não sabemos se houve input manual do dono durante a sessão, isso fica registrado como estado
  observado, não como teste automatizado da travessia completa.
- **Ainda requer teste manual:** derrotar os Ash Slimes, aproximar-se e usar E nas passagens, testar o desvio/recompensa,
  retornar sem respawn, conferir a cura do Santuário e concluir a rota. No momento o Editor está em Play Mode na sala
  inicial, pronto para esse teste.
- A primeira tentativa de executar o builder ocorreu durante uma transição de saída do Play Mode e o Unity a rejeitou;
  após o Editor ficar ocioso, o builder foi executado em Edit Mode e confirmou o salvamento. Não é necessário tratar isso
  como erro atual do projeto.

### Prova de conceito vertical — chefe final (Prompt 3)

- A rota e o combate da fase 2 foram confirmados em `Assets/HuntzSlice/`; o protótipo antigo em `Assets/Scenes/` e
  `Assets/IsoRPG/` não foi sobrescrito. O builder da slice agora coloca o **Guardião da Fenda** apenas na Câmara final.
- O chefe tem 18 HP provisórios e alterna dois padrões de área, em vez de só acumular vida/dano:
  - **Investida:** trava a direção do jogador, mostra um corredor laranja/vermelho e um aro no ponto final por 1,1 s;
    depois avança pelo corredor. Resposta esperada: sair lateralmente ou esquivar para fora da faixa.
  - **Pulso:** mostra um círculo/aro laranja de 2 unidades de raio por 1,2 s; depois causa dano dentro da área.
    Resposta esperada: sair do círculo antes do pulso.
  - Entre padrões, há 0,9 s de recuperação. O HUD do chefe mostra nome, HP atual/máximo e estado/instrução do padrão.
- **Regras deliberadamente provisórias na fase 3:** cada golpe do chefe causa 2 de dano; vencer concede `1 × Núcleo da
  Fenda`; morrer encerra a expedição sem a recompensa do chefe. O retorno à MainLand e o reset para nova hunt foram
  adicionados na fase 4 abaixo. Nenhum desses números/regras foi promovido a decisão do GDD, e não há perda de XP.
- As telas de vitória e derrota foram verificadas por chamadas de teste MCP aos métodos de dano (não por teclado). As duas
  notificações visuais foram inspecionadas na Câmara final; a faixa da investida foi ativada diretamente para a captura,
  e o círculo do pulso foi observado em execução. Isso valida apresentação/transições de estado, **não** valida combate
  manual, acerto real da investida, esquiva/i-frames nem balanceamento.
- Para inspecionar o chefe, uma chamada MCP runtime temporária entrou diretamente na sala 5; a chamada não foi salva na
  cena e não valida o percurso pelas passagens.
- **Verificado:** compilação após a última edição; builder salvou `HuntzVerticalSlice`; Play Mode via MCP abriu na Entrada
  com dois inimigos e um único root de sala ativo; inspeção runtime da Câmara final reportou chefe com 18/18 HP e
  `EnemiesRemaining=1`; Console sem erros/avisos próprios de `HuntzSlice`. Podem aparecer o aviso de WebSocket do MCP e
  `NoSubscription` do Unity AI, ambos já conhecidos e externos à slice. O Play Mode foi deixado na Entrada.
- Capturas temporárias de inspeção ficaram em `Library/HuntzSliceCaptures/` (não são assets do jogo):
  `phase3-charge-visual-1.png`, `phase3-result-win.png`, `phase3-result-loss.png` e `phase3-boss-hud-final.png`.
- **Teste manual pendente:** fazer a rota até a Câmara final, observar a faixa/aro da investida e o círculo do pulso,
  esquivar ou sair das áreas antes do impacto, atacar o chefe com J/clique, confirmar barra/recuperação/vitória e repetir
  ficando nas áreas para confirmar derrota. Instruções para Victor: comece na Entrada; derrote os dois Ash Slimes; use E
  na passagem; na Bifurcação escolha a Trilha principal (o Ninho de cinzas é opcional); atravesse o Santuário até a
  Câmara final. Controles continuam WASD/setas, J/clique esquerdo, Espaço para esquivar e E para passagem.

### Prova de conceito vertical — MainLand e ciclo de sessão (Prompt 4)

- A cena isolada continua em `Assets/HuntzSlice/Scenes/HuntzVerticalSlice.unity`. O builder agora está disponível em
  **Huntz → Build MainLand + Hunt Loop**; o antigo menu **Build Vertical Slice Room** permanece como alias. A geração
  sobrescreve somente essa cena da slice; não altera os protótipos em `Assets/Scenes/`/`Assets/IsoRPG/` nem o
  `ProjectSettings/EditorBuildSettings.asset` (os três cenários originais continuam listados).
- A cena reúne dois roots alternáveis: uma MainLand compacta isométrica (malha procedural 9×9, borda física, Portal HZ,
  mesa de preparação e cristais de referência) e o ambiente da hunt, inicialmente oculto. Jogador e controlador ficam
  fora desses roots para preservar estado durante as trocas. Aproximar-se do Portal HZ ou da mesa e pressionar **E**
  inicia uma hunt ou tenta comprar a melhoria, respectivamente.
- O recurso único é o **Núcleo da Fenda**, sem inventário ou loja geral: o desvio Ninho de cinzas concede 1 e vencer o
  chefe concede 1. A mesa gasta 1 para comprar uma única melhoria de teste de **+1 dano**. Saldo e melhoria existem
  apenas no runtime desta sessão; não há save/load. Núcleos do desvio já coletados ficam no saldo ao morrer; a morte não
  concede o núcleo do chefe. Valores e regras são temporários e não alteram `docs/GDD.md`.
- Vitória ou morte retorna automaticamente à MainLand, restaura o jogador a 8/8 HP e mostra um resumo da tentativa:
  resultado, salas visitadas, núcleos ganhos e saldo. O Portal pode iniciar outra expedição; estados, posição/HP dos
  slimes e estado/posição/HP do chefe são reinicializados. A melhoria comprada continua ativa entre hunts enquanto a
  sessão runtime estiver aberta.
- **Verificado pelo MCP:** recompilação solicitada e Editor pronto; menu do builder executado e cena salva; Play Mode
  iniciado na MainLand com 8/8 HP, Portal/mesa visíveis e HUD/controles legíveis. Uma rotina `execute_code` percorreu as
  passagens por chamadas de runtime, limpou salas via dano direto e venceu o chefe: confirmou bifurcação (desvio e rota
  principal), retorno com `+2` núcleos (desvio + chefe), compra por 1 núcleo, dano efetivo 1→2, nova hunt com dois
  inimigos restaurados e retorno à MainLand após morte, mantendo saldo e melhoria. Captura visual do resumo após derrota:
  `Library/HuntzSliceCaptures/phase4-result-mainland.png` (fora de `Assets/`). A rotina terminou com `MainLand=true`,
  ambiente da hunt inativo, 1 núcleo, dano 2 e jogador 8/8. Depois reiniciei o Play Mode; estado atual deixado para
  teste manual: MainLand ativa, saldo 0, dano base 1, sem melhoria, jogador 8/8.
- **Limite do teste:** a rotina valida estados/transições por código e teleportes MCP, não uma partida por teclado. A
  compilação/Console não mostrou erros C# nem mensagens próprias de `HuntzSlice`; `NoSubscription` do pacote Unity AI é
  ruído externo já conhecido. Movimento real, alcance da interação com E, combate do jogador, esquiva, telegráficos e
  sensação do ciclo ainda precisam de confirmação manual.
- **Teste manual recomendado:** para uma sessão limpa, pare e inicie Play Mode novamente. Mova com WASD/setas ao Portal
  HZ e pressione E; derrote os dois slimes e use E nas passagens. Na Bifurcação, escolha Ninho de cinzas, limpe-o para
  obter 1 núcleo, volte e siga Trilha principal → Santuário → Câmara final. Observe os dois avisos do Guardião, esquive
  ou saia das áreas e ataque com J/clique durante a recuperação. Ao vencer, confirme retorno/resumo; vá à mesa e
  pressione E para gastar 1 núcleo e ver o dano mudar de 1 para 2; volte ao Portal e confirme o bônus na hunt seguinte.
  Para testar derrota, permaneça nos avisos até HP zero e confirme retorno/resumo sem núcleo do chefe. O MCP não
  automatizou esses controles.

- O projeto **não está em git**. Recomendado: `git init` + `.gitignore` Unity (ignorar `Library/`, `Temp/`, `Logs/`,
  `UserSettings/`, `*.csproj`, `*.slnx`) antes da próxima rodada de mudanças.
- Pacote `com.unity.ai.assistant` gera erros `NoSubscription` no Console; pode ser removido se incomodar (confirmar com o dono).
- `Assets/TutorialInfo/` e `SampleScene` são sobras do template.
- O builder **sobrescreve** a cena isométrica a cada execução — qualquer edição manual na cena será perdida.
- Existe uma pasta `/home/victor/workspace/workspace/Huntz/Ui` no WSL de conteúdo/relação desconhecida com o jogo — perguntar.

## Entrega do kit de arte e da build jogável — 2026-10-06

- Cena de inicialização: `Assets/HuntzSlice/Scenes/HuntzVerticalSlice.unity` (primeira em Build Settings). Build atual em
  `Builds/HuntzPrototype_v0.1.0/Huntz.exe`; pacote Windows x64 completo na mesma pasta.
- Loop incluído: MainLand, Portal HZ, mesa de preparação e melhoria, seis salas com bifurcação, Ash Slimes, Guardião da
  Fenda, recompensa, derrota/vitória e retorno. O estado da sessão é temporário e não há save/load.
- Controles: WASD/setas para mover, J ou clique esquerdo para atacar, Espaço para esquivar, E para interagir e Q para
  usar poção. Na cena `HuntzIsoRPG`, E interage com NPC/baú e Q usa poção.
- Arte final em `Assets/HuntzSlice/Art/Final/`: guerreiro com Idle/Walk/Attack/Dodge/Hurt/Death em oito direções,
  NPCs, Ash Slime, Slime Rei, Guardião da Fenda, terrenos, props, telegráficos, VFX e ícones do HUD. Animações em
  `Assets/HuntzSlice/Animations/`; prefabs da fatia e do mapa isométrico em suas pastas `Prefabs/`.
- Cena `HuntzIsoRPG` reconstruída e aberta em Play Mode: aldeia, terreno, casas, poço, árvores, pedras, baús, guerreiro,
  NPCs e slimes usam sprites; captura `Library/HuntzCaptures/isometric-village-final.png`. MainLand está em
  `Library/HuntzCaptures/final-mainland-preview.png`.
- Verificação: compilação Unity sem erros C# próprios; ambas as cenas abriram em Play Mode; estados de salas, chefe,
  recompensa, melhoria, retorno e reinício foram percorridos via chamadas MCP. Build Windows x64 terminou com sucesso,
  0 erros e avisos de pacotes/shaders externos ao código do jogo.
- A jogada de teclado/mouse ainda precisa de uma pessoa: Unity MCP não injeta entradas. Ela serve para sentir o ritmo do
  combate e conferir controles, sem bloquear a entrega do protótipo.
- O guerreiro da fatia é uma folha completa por ação/direção. A montagem em camadas quadro a quadro de pele, cabelo,
  roupa e equipamento e variações selecionáveis é a próxima expansão do padrão humanoide; não está incluída na arte atual.

## Próximos passos sugeridos

1. Abrir `Builds/HuntzPrototype_v0.1.0/Huntz.exe` e jogar uma sessão completa; enviar feedback de escala, câmera e ritmo.
2. Iniciar o controle de versão Git antes de uma rodada maior de level design.
3. Expandir o Humanoid Standard para sprites sincronizados de cabelo, pele, roupa e equipamento, com pivôs e rótulos de
   pose compatíveis; validar as camadas no prefab antes de criar várias aparências.
4. Trocar o layout gerado por builder por Tilemap isométrico + prefabs quando o level design manual começar.
5. Planejar persistência e novos sistemas somente depois de validar este loop curto.

## Registro de sessões

| Data | Agente | Resumo |
|---|---|---|
| 2026-10-04/05 | Claude Code | Setup MCP + WSL; demo plataforma 2D; conceito RPG isométrico; docs de handoff |
| 2026-10-05 | Codex | Health check do MCP via HTTP: instância `My project` conectada, Unity 6000.6.4f1, cena `HuntzIsoRPG`, compilação concluída e Console sem erros/avisos; última leitura de estado reportou `stale_status`. |
| 2026-10-05 | Codex | Leitura de `Anotações.txt` e criação de `docs/GDD.md` com visão consolidada, propostas de mecânicas, escopo de primeiro marco e decisões em aberto. |
| 2026-10-05 | Codex | MCP iniciado no Unity e verificado novamente: endpoint HTTP respondeu 200, uma instância `My project` conectada, `ready_for_tools=true`, sem compilação pendente, cena `HuntzIsoRPG` ativa e Console com 0 erros/avisos. |
| 2026-10-05 | Codex | Implementada a primeira sala jogável isolada em `Assets/HuntzSlice/` e salva como `HuntzVerticalSlice`; protótipos e Build Settings preservados. Scripts compilaram sem erros/avisos próprios, cena aberta em Play Mode e inspecionada via MCP. Controles aguardam teste manual; `editor/state` ficou stale temporariamente e terminou pronto para ferramentas. |
| 2026-10-06 | Codex | Prompt 2: rota fixa de seis salas, passagens após combate, retorno sem respawn, desvio com recompensa, santuário, sala final e mapa/HUD. Corrigida inicialização do estado runtime e resiliência dos singletons. Compilação, reconstrução e início em Play Mode verificados; Console final limpo. Travessia completa ainda depende de confirmação manual. |
| 2026-10-06 | Codex | Prompt 3: chefe Guardião da Fenda na Câmara final, investida em corredor e pulso circular telegráficos, HUD de chefe, fim por vitória/derrota e Núcleo da Fenda como recompensa temporária. Compilação/build/Play e estados de resultado inspecionados via MCP; nenhuma entrada de teclado foi simulada; combate/esquiva ainda dependem de teste manual. |
| 2026-10-06 | Codex | Prompt 4: MainLand procedural com Portal HZ/mesa, retorno com resumo em vitória ou morte, Núcleo da Fenda como recurso único de sessão e melhoria +1 dano. Compilação, builder e Play Mode conferidos; transições completas validadas por chamadas runtime MCP, sem simular teclado. Teste manual de controle/interação/combate pendente. |
| 2026-10-06 | Codex | PixelLab MCP reconectado e validado. Gerado `Huntz Humanoid Standard v1 - Base` em oito direções; PNGs baixados e Unity AssetDatabase atualizado em `Assets/HuntzSlice/Art/Drafts/HumanoidStandard/PixelLab_Base/`. O grupo Idle tem quatro quadros concluídos para South; faltam sete direções. O trial limita a uma geração de animação simultânea, então as demais precisam ser enfileiradas sequencialmente. Duas gerações usadas, 38 restantes. Detalhes e IDs em `docs/ASSET_BACKLOG.md`. |
| 2026-10-06 | Codex | Criada a cena separada `Assets/HuntzSlice/Scenes/HumanoidStandardPreview.unity` e o menu **Huntz → Build Humanoid Standard Preview** para comparar as oito direções em escala de jogo, com filtro Point, 64 PPU e pivô nos pés. Screenshot em `Library/HuntzSliceCaptures/humanoid-standard-preview-1.png`; a fatia principal e os Build Settings não foram alterados. |
| 2026-10-06 | Codex | Kit de arte final integrado à fatia vertical e ao protótipo isométrico, controlador direcional e prefabs reutilizáveis criados, ambas as cenas abertas em Play Mode, Build Settings reorganizados com a fatia em primeiro e build Windows x64 gerada. Jogada por teclado continua manual; detalhes e limites no bloco “Entrega do kit de arte e da build jogável”. |
