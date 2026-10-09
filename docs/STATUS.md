# Estado do projeto

Atualizado em 2026-10-09. Este é o registro do que está integrado e do que foi realmente conferido.

## Validação manual relatada pelo dono — 2026-10-09

- O dono informou que, no Play Mode, testou e validou andar, correr, esquiva, ataque, monstros, dano e morte. Este registro vem do relato do dono; não houve uma nova partida nesta sessão.
- A Hunt completa de ponta a ponta continua pendente, conforme a seção de validação abaixo.

## Refinamento sequencial do piloto 3D — 2026-10-08

- O trabalho agora avança por etapas com verificação no Unity antes de começar a seguinte: física e locomoção; rig e animação; combate; ambientes e fluxo de Hunt; controles e interface; build e desempenho. A produção de arte continua pausada.
- Etapa ativa — chão e colisão: o builder passou a preparar uma malha de colisão própria e um filho `Collision` nas ilhas, separado do renderer. O menu `Huntz/Prototype 3D/Refresh Island Collision` atualiza a cena existente sem rodar o construtor completo.
- Unity 6000.6.4f1 recompilou o builder sem erros ou avisos. A migração criou `MainlandIsland_Collision` e `HuntIsland_Collision`; cada ilha tem um filho `Collision` com `MeshCollider`, e o objeto visual não mantém collider. A cena foi salva.
- A verificação manual no Play Mode confirmou apoio no chão da MainLand e da Hunt, bloqueio lateral pela parede da oficina e pelo pilar de treino, os dois sentidos do portal e os limites das malhas por raycast. O Console terminou com zero erros e avisos; a cena está limpa no Edit Mode e continua como única entrada de Build Settings.
- WASD/corrida/parada/rotação e a interação E ainda precisam de validação com o Game View realmente em foco. A instância estava sem foco do sistema operacional; um evento sintético mostrou a tecla W pressionada no dispositivo, mas o `InputAction` não recebeu valor. Isso não demonstra falha do controlador, então a etapa 2 continua bloqueada até a passagem manual de controles. Nenhuma build ou suíte automatizada foi executada.

## Incremento mais recente — Hunt vertical slice 3D — 2026-10-08

- `HuntzArpg3DHuntDirector` foi integrado à cena ativa pelo menu do Editor `Huntz/Prototype 3D/Install Hunt Vertical Slice`; o builder principal também anexa o diretor em futuras reconstruções. Não foi reconstruída a cena inteira.
- Portal HZ agora inicia a Hunt 01 Fácil. A expedição cria uma grade lógica conectada 3×3, com entrada A1 fixa, chefe final C3, saídas apenas para salas adjacentes e possibilidade de voltar a salas já limpas. A HUD identifica sala e inimigos e mostra os nove nós; tipos não visitados ficam ocultos.
- Cada sala de combate cria slimes de cinzas. Uma sala de saque e um chefe surpresa são sorteados em posições distintas a cada expedição. Limpar a sala libera os portais. O chefe final concede fragmentos e encerra a expedição, retornando à MainLand.
- O baú de saque, os encontros e os chefes concedem fragmentos de fenda. A carteira fica no personagem durante a sessão e os fragmentos coletados permanecem após morte; morrer na Hunt retorna à MainLand após breve pausa e restaura vida/vigor. O protótipo ainda não tem XP/níveis para representar a perda de XP anotada.
- A Hunt reutiliza a mesma arena graybox em cada nó e cria os portais no runtime. As dificuldades Média/Difícil, arenas próprias por sala, save/load, seleção pelo Portal HZ, progressão, lojas/armazém funcionais e itens equipáveis continuam pendentes.
- Unity 6000.6.4f1 recompilou os scripts sem erros C# nem avisos de código. O Console registrou um aviso interno do MCP (`WebSocket is not initialised`), sem bloquear atualização ou gravação da cena. A cena foi salva e segue como entrada de Build Settings.
- A Hunt ainda não foi percorrida manualmente de ponta a ponta no Play Mode; colisões e equilíbrio dos encontros novos precisam dessa validação. Nenhuma build standalone ou suíte automatizada foi executada.

## Revisão mais recente — rig humanoide 3D — 2026-10-08

- O rig graybox agora tem `LeftToeBase` e `RightToeBase` como articulações próprias. Cada bota foi dividida em calcanhar e antepé; Walk/Run animam o impulso na saída do apoio e a elevação dos dedos durante o balanço. Os dois clips passaram de 53 para 61 bindings, mantendo 33 amostras por ciclo.
- `HuntzArpg3DRig` serializa e expõe referências para a raiz do esqueleto, quadril, tronco, cabeça, mãos, pés, dedos e `WeaponSocket`. `ValidateRig` confere ossos, socket na mão direita, Animator Controller e os Avatars.
- O builder salva dois Avatars: `HuntzHumanoid3D_TransformCurves.asset` é o Generic ativo, necessário para as curvas diretas dos clips atuais; `HuntzHumanoid3D.asset` tem um mapeamento Humanoid válido para retargeting futuro. A validação ao vivo mostrou que as curvas de pose atuais não acompanham corretamente o ciclo quando o Humanoid fica ativo, então a troca fica pendente da conversão dos clips.
- O menu `Huntz/Prototype 3D/Refresh Humanoid Rig Prefab` atualiza somente o prefab compartilhado e seus Avatars; não reconstrói nem salva a cena. Walk/Run e o prefab foram atualizados pelo Editor.
- Play Mode confirmou 4/4 rigs da cena válidos, espada no socket direito, Walk selecionado e variação de pose nas coxas, joelhos, pés e `ToeBase` ao longo do ciclo. Console após a conferência: zero erros e avisos.
- Editor voltou ao Edit Mode; a cena 3D permaneceu limpa e é a única cena de Build Settings. Não foi gerada build nem executada suíte automatizada. A leitura visual da passada em escala normal continua pendente.

## Revisão mais recente — locomoção humanoide 3D — 2026-10-08

- O diagnóstico confirmou quatro falhas: o `Locomotion` misturava só Idle/Run; `Run.anim` não repetia; as curvas de joelho e pé apontavam para caminhos que não existiam no rig; e a cadência de corrida estava rápida demais para a velocidade do corpo.
- `Huntz/Prototype 3D/Refresh Locomotion Animations` foi executado no Editor. Walk e Run agora repetem e têm 53 bindings cada, com 33 amostras por ciclo e curvas ligadas à hierarquia real das pernas, pés, braços e tronco. O BlendTree contém Idle/Walk/Run nos limites 0/1/1,55 e usa `GaitRate` para a velocidade do estado.
- O ciclo separa apoio e balanço: a perna percorre toda a passada durante o apoio, o joelho flexiona no balanço e o pé compensa a rotação de coxa/joelho para se manter nivelado. A taxa de corrida foi reduzida de 1,8 para 1,2; a animação continua seguindo a velocidade horizontal efetiva do `CharacterController`.
- Play Mode validado com entrada do Input System: andando, `MoveSpeed=1,00`, `GaitRate=1,30`, Walk com peso 1 e velocidade horizontal aproximada de 4,8; correndo com Shift, `MoveSpeed=1,55`, `GaitRate=1,20`, Run com peso 1 e velocidade aproximada de 7,4. A leitura de pose confirmou ângulos alternados nas pernas e compensação do pé durante o apoio.
- Unity recompilou os scripts sem erros C#. A cena 3D de build ficou aberta em Edit Mode e sem alterações salvas; nenhum builder de cena foi executado. Console final após o Play Mode: zero erros e avisos. Durante um refresh anterior apareceu um aviso interno do WebSocket MCP, sem bloquear o trabalho.
- A validação é do rig graybox procedural. Ela confirma que os ciclos entram, repetem e articulam o personagem durante deslocamento; ainda falta revisar a leitura visual em escala normal de jogo e substituir a animação de placeholder quando houver um modelo 3D final rigado. Nenhuma suíte automatizada ou build foi executada.

## Direção vigente — piloto 3D isométrico

- Huntz é agora um RPG de ação isométrico 3D. A antiga identidade pixel-art 2D foi aposentada: não serve de base visual, rig ou gameplay para o jogo novo. A produção de arte 3D também segue pausada; este incremento usa só recursos graybox da Unity.
- A única cena em Build Settings é `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`. O conteúdo 2D permanece no repositório apenas como arquivo histórico, fora da build.
- A cena 3D tem MainLand em forma de asteroide, área de treino alcançada por portal, câmera ortográfica com zoom na roda do mouse, praça, trilha de pedra ao portal, lojas com fachadas detalhadas, marco astral, árvores, obstáculos, NPCs, dois Ash Slimes e um Guardião de treino.
- O jogador usa Input System, `CharacterController`, movimento relativo à câmera com aceleração/desaceleração, corrida com Shift e vigor, ataque mantido no mouse/J com repetição, movimento reduzido durante golpes e buffer curto de entrada. Em movimento, o guerreiro vira para a direção percorrida; ao iniciar cada golpe, mira uma vez pelo mouse e mantém essa direção durante o ataque. Espaço faz esquiva direcional cancelável durante o ataque, com custo de vigor, recarga e invulnerabilidade curta. E interage, C alterna o visual de teste e R reinicia; a HUD mostra barras de vida e vigor.
- O humanoide graybox usa `Animator`, Generic Avatar para os clips com curvas Transform, mapeamento Humanoid separado para retargeting futuro, ossos Transform com nomes convencionados, aparência por slots de material e `WeaponSocket` na mão. Idle/Run/Attack/Dodge/Hurt/Death têm clips do Animator; o Animation Event de ataque sincroniza arma e dano.
- A composição atual prende primitivas rígidas aos ossos. `SkinnedMeshRenderer`, pesos de vértice e modelo final importado ainda não foram integrados. Os NPCs e alvos são placeholders.
- O Unity MCP compilou os scripts sem erros. Foi corrigido o builder do Avatar: antes ele recebia um nome de root motion inexistente e salvava em uma extensão que não era importada como asset nativo; agora usa root motion vazio e `.asset`. O prefab atual e seus quatro Animator na cena apontam para `HuntzHumanoid3D` válido.
- A revisão de 2026-10-08 enviou movimento e corrida pelo Input System e confirmou Walk/Run com deslocamento real; veja a seção mais recente acima. Attack ainda foi conferido por pose do Animator, sem validar a mira com mouse. Esquiva, dano, interação, zoom, colisões e portais seguem pendentes de validação manual.
- A captura runtime `Assets/Screenshots/Huntz_3D_ARPG_Pilot_Evolved_Final.png` mostra o enquadramento, HUD, trilha, fachadas, portal e marco astral. A cena voltou ao Edit Mode e continua como única entrada de `ProjectSettings/EditorBuildSettings.asset`. Não há build standalone aprovada nem testes automatizados executados. O Console final não mostrou erros nem avisos.

## Próximos incrementos do piloto 3D

1. Concluir a etapa física: compilar, separar as colisões das duas ilhas na cena, confirmar que terreno, obstáculos e NPCs bloqueiam corretamente e conferir aderência ao chão no Play Mode.
2. Consolidar o contrato do rig e das animações: Idle/Walk/Run, oito direções, ataques, esquiva, dano e morte, com arma acompanhando o socket.
3. Refinar o ciclo de combate: preparação, impacto e recuperação sincronizados entre corpo, arma, efeito e dano; validar alvos e colisões de hit.
4. Estruturar mundo e Hunt em peças reutilizáveis: arenas/colliders, encontros, portais, NPCs e transições; validar a Hunt Fácil de ponta a ponta.
5. Separar controles e interface da lógica do personagem, criar configuração de input reutilizável e trocar HUD provisória quando o fluxo estiver estável.
6. Completar persistência e progressão mínima da fatia; só então expandir dificuldades e sistemas de loot/economia.
7. Fazer a revisão final de build, resolução, desempenho e Console. Só reabrir produção de arte quando o dono liberar essa etapa.

## Registro histórico do protótipo 2D

Os incrementos abaixo documentam a fatia 2D e os ensaios de arte anteriores. Permanecem úteis para consulta, mas não definem mais a direção ou a cena de build atuais.

## Incremento de arma e ataque — 2026-10-07

- No código, a espada é um item `HuntzWeaponDefinition` equipável por `HuntzEquippableWeapon`; o jogador cria a camada no visual e carrega a espada de treino. Dano, alcance e largura aceitam modificadores do item.
- Attack agora tem arte corporal sem arma embutida nas oito direções: seis quadros a 7,14 fps nas sete vistas e East com 12 quadros a 14 fps. As durações ficam entre 0,84 e 0,86 s; o evento de impacto ocorre por volta de 0,29 s (quadro 3 nas folhas de seis quadros e quadro 5 em East). O lunge corporal é discreto; o arco da espada carrega a maior parte da leitura do golpe.
- A espada equipada, a pose de impacto, o arco visual e a consulta da área de dano começam no evento do Animator. A pose de impacto da espada permanece durante 16% da duração do clip, junto da seção corporal de contato. O jogador trava a direção e aplica dano uma vez por alvo.
- O Editor importou as oito vistas e a definição da espada. `HuntzAnimationAssetBuilder.IsHeroAttackSetImported()`, `IsHeroAttackEastImported()` e `HuntzWeaponAssetBuilder.IsStarterSwordImported()` retornaram verdadeiro; os clips sete direções têm seis quadros com camada equipada e East tem 12.
- A Unity compilou sem erros. `HuntzAnimationTools.ValidateScene` retornou zero erros. No Play Mode, um alvo temporário de teste recebeu dano de 2 para 1 no frame 2 de SouthEast, com `impactResolved`, espada e arco ativos no mesmo evento. O alvo foi removido ao final, o Play Mode foi encerrado e a cena não foi salva.
- O Console final ficou sem erros ou avisos. Durante o refresh apareceu um aviso de WebSocket interno do MCP, sem impedir importação, compilação ou validação.
- O PixelLab AutoSprite não conseguiu buscar a URL da imagem de referência (404). A folha 8D foi montada com o PixelLab pixel-art workbench sem consumir gerações. Higgsfield manteve 84 créditos; não foi cobrado porque o conector também não aceita os arquivos locais por uma via de transferência suportada.

## Verificação de 2026-10-06

O dono confirmou que o problema ocorria no **Play Mode**, com direção correta mas sem passos visíveis. A hipótese anterior de build antiga não explicava esse relato. Entrada real enviada ao Input System confirmou troca de sprites; a caminhada antiga era composta por recortes deslocados de Idle e tinha movimento pouco legível.

Após a troca de arte:

- Gamepad temporário do Input System acionou movimento e parada em oito direções. Todos os ciclos Walk chegaram a F07; North foi repetido numa área livre porque o portal bloqueava o primeiro trajeto.
- A colisão com o portal interrompeu o deslocamento e selecionou Idle. Capturas em `Assets/Screenshots/Hero_Walk_East_Runtime_*.png` mostram passadas distintas em movimento.
- Ataque e esquiva pelo Input System chegaram a F05 e F03. Dano e morte via `TakeHit` chegaram a F01 e F07. A inspeção de dano usou a API do personagem, sem depender de um golpe de inimigo.
- E enviado pelo Input System, após posicionar o jogador perto do portal, iniciou a sala 0 com dois slimes. Idle F00–F03 e Move F00–F05 dos slimes foram observados.
- Cena salva com zero scripts ausentes e zero erros da validação de animações. Console final do Play Mode sem erros/avisos. Durante uma recarga intermediária houve mensagens nativas de GC handle; elas não reapareceram na conferência final.
- Dispositivos e callbacks temporários foram removidos; nenhuma suíte de testes foi adicionada ou executada.

## Build e limitações

A build Windows x64 0.1.3 foi gerada em `Builds/HuntzPrototype_0.1.3_WalkFoundation/Huntz.exe` (106,49 MB): zero erros e dois avisos de shaders de depuração. `PlayerSettings.bundleVersion` está em 0.1.3. O executável não foi aberto para uma partida nesta revisão. As builds anteriores 0.1.1 e 0.1.2 não contêm esta caminhada.

A arte da fatia atual ainda precisa de acabamento, especialmente consistência da espada e transição entre estados. Attack, Dodge, Hurt e Death ainda usam arte procedural anterior; seu noroeste deriva da fonte antiga e precisa de revisão visual. Reproduzir todos os frames não equivale a aprovar essas poses. O piloto prova troca de camadas/paletas com placeholders, mas não define os visuais finais nem cobre variações de silhueta/sexo/roupa.

Não foi jogada uma hunt completa nesta revisão. Colisões de todos os props, todos os diálogos, equilíbrio do combate e luta completa do chefe não foram validados de ponta a ponta. A inspeção no Editor não comprova uma partida no executável standalone.

## Próximos incrementos do protótipo 2D (histórico)

1. Revisar visualmente a caminhada no Play Mode com o dono; ajustar ritmo, arma e continuidade conforme a leitura em escala de jogo.
2. Rever visualmente os pontos de mão e a ordem de desenho da espada em escala de jogo nas oito direções; aumentar a articulação corporal de Attack além do lunge discreto quando a arte puder ser refinada.
3. Rever e substituir a arte provisória de Dodge, Hurt e Death, inclusive Northwest, usando a prévia e o contrato de importação.
4. Jogar a rota completa, incluindo NPCs, colisões, chefe e retorno à MainLand.
5. Validar no Play Mode o piloto graybox: oito direções, colisões, portal ida/volta, diálogo, esquiva, impacto/dano e troca de paleta.
6. Refinar pivôs/ordem de desenho do rig; só depois integrar o contrato ao guerreiro da cena de build e decidir se o piloto aprovado substituirá a cena atual.

## Decisões e cuidados do protótipo 2D (histórico)

- Guerreiro é o personagem inicial; oito direções completas, sem substituir diagonais por espelhamento.
- O corpo-base humanoide deve crescer com slots de aparência e poses compatíveis; essa integração ainda não existe.
- PixelLab e Higgsfield são opções de criação. Nesta continuação foram usadas mais cinco gerações trial no PixelLab; saldo ficou em zero e US$ 0,00 pagos. Saldo Higgsfield consultado: 84 créditos; AutoSprite não completou a transferência da referência e não consumiu créditos.
- Git possui alterações staged e unstaged anteriores, preservadas. Checkpoint local `accccd5`; nenhum remoto configurado, push ou commit desta revisão.
- A recuperação da cena antiga HuntzIsoRPG fica fora do projeto em `C:/Users/victo/Documents/Huntz/Recovery/HuntzIsoRPG_2026-10-06/`. Não reintroduzir essa cena em Assets.
