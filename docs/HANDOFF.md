# Handoff — Huntz

Atualizado em 2026-10-09.

## Retorno mais recente do dono — 2026-10-09

- O dono concluiu uma Hunt inteira do início ao fim, incluindo encontros e morte. Confirmou que a morte o levou de volta à MainLand. Essa passagem foi testada pelo dono no Play Mode.
- Movimento, corrida, esquiva, ataque, monstros e dano também foram validados pelo dono. A penalidade de XP por morte fica para uma decisão de design posterior; XP ainda não existe no piloto.

## Pedido e diagnóstico atual

Huntz agora é um RPG de ação isométrico 3D inspirado no gênero de Diablo/Path of Exile. A antiga direção de personagem e sprites 2D foi aposentada; não a use como base para gameplay, rig ou arte. A produção de arte 3D continua pausada; evoluir o motor e o graybox com Unity e ferramentas já instaladas.

O único fluxo ativo é o piloto 3D. Os arquivos 2D que ainda estão no repositório são históricos e não devem orientar o design novo.

## Plano de evolução e retomada

Arte final e geração paga seguem pausadas; a meta desta fase é trocar graybox por graybox melhor estruturado e contratos de gameplay reutilizáveis, sem reconstruir a cena inteira a cada iteração.

1. **Combate corpo a corpo — base integrada.** A espada graybox está montada no `WeaponSocket`; `HuntzArpg3DWeaponGeometry` define base/ponta e uma cápsula de contato. O golpe tem preparação, impacto e recuperação com o evento alinhado à pose de impacto; dano é consultado apenas nesse evento e cada alvo recebe um acerto por golpe. O Unity MCP confirmou rig válido e, em Play Mode controlado, um alvo a 1 m recebeu dano e outro a 3 m não. O dono já validou o combate durante a Hunt completa. Próximo refinamento: observar visualmente a trajetória no Play Mode com input real, calibrar o raio/alcance e adicionar feedback de impacto quando a geometria estiver aprovada.
2. **Kit de geometria graybox — próxima fatia.** Criar módulos reutilizáveis de piso, parede baixa/alta, pilares, cobertura e plataformas, cada um com apresentação e colisão ajustadas; validar escala em relação ao guerreiro, largura de passagem, linhas de visão da câmera e oclusão do personagem. Usar esses módulos para dar silhuetas diferentes às salas e não alterar a conectividade A1–C3.
3. **Legibilidade da Hunt.** Variar arenas por função (combate, saque, chefe), adicionar pontos de referência e rotas legíveis, e conferir portais/gates, espaço de esquiva e alcance corpo a corpo em cada arranjo. Só então considerar a geometria graybox apta a servir de proxy para produção visual.
4. **Progressão e qualidade da fatia.** Definir XP/perda por morte com o dono antes de implementar; em seguida consolidar equipamento/persistência e validar build standalone. A Hunt completa e o retorno após morte já foram manualmente confirmados pelo dono.

### Incremento de arma e hitbox — 2026-10-09

- A espada foi detalhada em lâmina, ponta, guarda, empunhadura, anéis e pomo e permanece filha do socket da mão direita. Isso é uma representação visual graybox, não um item selecionável pelo inventário.
- A cápsula de contato usa os transforms da base e ponta da lâmina, respeitando escala, e é desenhada ao selecionar o objeto no Editor. O controlador consulta essa cápsula no callback `OnWeaponImpact`, em vez de aplicar dano com uma esfera de alcance genérica.
- As curvas do clipe `Attack` e seu evento agora compartilham o tempo normalizado de impacto (31% de um clipe de 0,68 s, isto é, 0,2108 s); as poses foram ajustadas para que a lâmina avance no frame do golpe.
- Spot-check pelo Unity MCP: `ValidateRig` passou; com a pose de impacto aplicada, um alvo próximo perdeu 1 HP e outro além do alcance permaneceu intacto. A sonda usou o callback de gameplay e a pose exata, não uma partida automatizada ou um teste de input/animação quadro a quadro.
- O Editor voltou ao Edit Mode, a cena ativa segue salva/limpa e é a única cena habilitada em Build Settings. Sem build ou testes automatizados nesta etapa.

## Estado funcional de base — primeira Hunt jogável 3D, 2026-10-08

- A cena salva `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity` tem `HuntzArpg3DHuntDirector` no root `Generated 3D ARPG Graybox`. O menu `Huntz/Prototype 3D/Install Hunt Vertical Slice` adiciona somente esse sistema e salva a cena; o builder principal anexa o componente em futuras reconstruções.
- Interagir com o Portal HZ inicia Hunt 01 Fácil, com mapa lógico conectado 3×3 (A1 entrada, C3 chefe final), gates nas salas adjacentes e revisita de nós já limpos. Tipos não visitados ficam ocultos no minimapa. Inimigos bloqueiam as saídas até serem derrotados.
- O diretor sorteia uma sala de saque e um chefe surpresa por run. O baú e os encontros concedem fragmentos provisórios; o chefe final encerra e retorna à MainLand. Morrer na Hunt retorna à base depois de 1,4 s, recupera vida/vigor e mantém fragmentos.
- A Unity recompilou sem erros C#. A cena foi instalada pelo Editor MCP e salva. Na primeira validação, a Hunt ainda não havia sido percorrida; o dono confirmou depois a conclusão ponta a ponta e o retorno à MainLand após morrer, conforme o registro mais recente no topo deste handoff.
- Limites conhecidos: todos os nós reutilizam a arena graybox; só a Fácil está ligada. Não há XP/níveis, persistência de carteira, loot equipável, lojas, stash, atributos ou habilidades funcionais. Fragmentos existem somente enquanto o jogador está na sessão.

### Resultado da validação manual da Hunt

- O dono percorreu a Hunt do início ao fim e verificou que morrer retorna à MainLand. Não há relato novo de problemas no fluxo. A perda de XP permanece fora até a regra ser decidida.
- A geometria ainda precisa de uma avaliação orientada a conteúdo: salas distintas, espaços de manobra, colisores e legibilidade isométrica. Essa é a próxima etapa, antes de chamar o graybox de proxy confiável para arte final.

## Incremento mais recente — rig humanoide 3D, 2026-10-08

- O rig agora tem `LeftToeBase` e `RightToeBase`, com a bota separada em calcanhar e antepé. Walk/Run incluem o rolamento de saída do apoio e a elevação dos dedos no balanço; cada clip tem 61 bindings e 33 amostras por ciclo.
- `HuntzArpg3DRig` serializa referências estáveis à raiz do esqueleto, quadril, tronco, cabeça, mãos, pés, dedos e `WeaponSocket`. `ValidateRig` checa ossos requeridos, socket na mão direita, controller e Avatars.
- Dois Avatars são salvos: `Animation/HuntzHumanoid3D_TransformCurves.asset` fica ativo como Generic para manter os clips atuais; `Animation/HuntzHumanoid3D.asset` contém o mapeamento Humanoid para retargeting futuro. Ao ativar o Humanoid, as curvas Transform atuais não acompanharam o ciclo corretamente. Converta os clips antes de trocar o Avatar do Animator.
- `Huntz/Prototype 3D/Refresh Humanoid Rig Prefab` atualiza só o prefab compartilhado. `Huntz/Prototype 3D/Refresh Locomotion Animations` atualiza os clips e o BlendTree. O builder da cena não foi chamado.
- Play Mode: 4/4 rigs da cena válidos; Walk exibiu rotação de coxa, joelho, pé e `ToeBase` em diferentes pontos do ciclo. Console final sem erros ou avisos. Editor retornou ao Edit Mode; cena 3D limpa e única cena de Build Settings. Sem build ou testes automatizados.
- A passada deve ser revisada visualmente em escala normal de jogo. O piloto ainda não tem `SkinnedMeshRenderer` ou modelo importado.

## Base de locomoção consolidada — 2026-10-08

- O refresh foi executado com a ponte Unity MCP na cena `Isometric3DARPGPilot`; nenhum builder de cena foi chamado. Foram confirmados os caminhos de joelho/pé na hierarquia e corrigidos no builder.
- Walk e Run repetem e têm 53 curvas cada, incluindo quadril, coxas, joelhos, pés, braços e tronco. Cada ciclo tem 33 amostras. O BlendTree contém Idle/Walk/Run com `GaitRate` como parâmetro de velocidade.
- O ciclo usa passada completa com apoio e balanço, flexão de joelho no balanço e rotação compensatória do pé durante o apoio. A corrida agora usa `GaitRate` de 1,2 na velocidade máxima, reduzindo o descompasso que fazia os pés passarem pelo chão rápido demais.
- No Play Mode, uma entrada temporária pelo Input System acionou movimento: Walk chegou a peso 1 com `MoveSpeed=1,00` e `GaitRate=1,30`; Shift mudou para Run com peso 1, `MoveSpeed=1,55` e `GaitRate=1,20`. Foram observados deslocamento, troca do estado, rotação das pernas e compensação do pé. Não foi criada/executada uma suíte de testes.
- A compilação C# terminou sem erros. Console final após o Play Mode retornou zero erros/avisos; durante um refresh anterior o MCP registrou um aviso interno `[WebSocket] Unexpected receive error: WebSocket is not initialised`. Editor final em Edit Mode, cena 3D ativa e sem alterações salvas. Nenhuma build foi gerada.
- Esta verificação cobre o rig geométrico graybox. Próximo passo: revisar visualmente a passada na escala normal da Game View e ajustar com base na leitura do dono; o placeholder procedural não equivale à animação final de um personagem 3D rigado.

## Incremento atual — piloto ARPG isométrico 3D, 2026-10-07

- Criada a cena `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`, com MainLand-asteroide, área de treino, portais, NPCs, obstáculos, dois Ash Slimes e Guardião de treino.
- O player usa `CharacterController` no plano XZ, Animator, espada no socket, ataque com impacto disparado por Animation Event, esquiva, dano/morte, HUD e interação. Ao andar, vira na direção do movimento; o mouse só atualiza a direção no início de cada golpe, que fica travada até o fim da animação.
- O controlador usa aceleração/desaceleração, Shift para corrida, vigor com regeneração, ataque mantido com repetição e buffer, deslocamento reduzido durante ataques e esquiva direcional que cancela o golpe. A câmera acompanha o jogador e aceita zoom suave pela roda do mouse; a HUD apresenta barras de vida e vigor.
- O builder 3D agora gera trilha até o portal, telhados inclinados, janelas, portal visível e marco astral na vila. A produção continua graybox; nenhum asset 2D antigo nem serviço de geração foi usado.
- O humanoide tem Avatar Generic válido salvo como `.asset`, ossos Transform nomeados e seis clips (Idle, Run, Attack, Dodge, Hurt, Death). Perfis alteram cores nos slots Skin/Cloth/Leather/Metal/Hair/Accent. Ainda não há SkinnedMeshRenderer ou malha importada.
- As animações estavam paradas porque o builder passava um root motion inexistente ao `BuildGenericAvatar` e tentava salvar o resultado como `.avatar`. Agora usa root motion vazio, salva em `.asset` e o prefab atual foi reparado; os quatro Animator da cena apontam para o Avatar válido. O Play Mode confirmou `Run` com peso 1 e pose de perna alterada; o estado/clip `Attack` também alterou o braço.
- A revisão posterior de 2026-10-08 validou deslocamento real com WASD e Shift pelo Input System, além de Walk/Run e cadência; veja a seção de continuação atual acima. Mira por mouse, esquiva, dano, interação, zoom, colisões e portais ainda precisam de partida manual. Não foi gerada build ou suíte automatizada.
- O Unity MCP e `ProjectSettings/EditorBuildSettings.asset` confirmam somente `Isometric3DARPGPilot.unity` habilitada na build; `HuntzVerticalSlice.unity` e a cena 2D HumanoidRigPilot ficaram preservadas fora dela.
- O Editor está em Edit Mode com a cena 3D ativa. Nenhuma geração/cobrança de PixelLab ou Higgsfield ocorreu.

## Próximos passos

1. Revisar visualmente a passada em escala normal de jogo; validar vigor, roda de zoom, ataque mantido, cancelamento por esquiva, colisões, NPCs e portal ida/volta.
2. Validar manualmente a mira travada no início do ataque com mouse; ajustar aceleração, distâncias de esquiva e zoom com base na partida.
3. Validar o fluxo de rig com um modelo importado quando o dono liberar a etapa de arte; preservar bone names/socket e mapear `SkinnedMeshRenderer`.
4. Revisar a primeira build standalone depois que os controles em Play Mode forem aprovados.

## Histórico do protótipo 2D preservado

## Histórico 2D — piloto de rig e ambiente graybox

- Criados `HuntzHumanoidRig`, `HuntzHumanoidRigPart`, `HuntzHumanoidAppearance` e `HuntzHumanoidAppearanceProfile`. O contrato separa corpo, cabeça, cabelo, face, torso, membros, armadura e acessório em SpriteRenderers presos à hierarquia Transform.
- `Assets/Huntz/Prototype/HumanoidRigPilot` contém prefab, Sprite Library, duas paletas de placeholder e AnimatorController com Idle/Walk/Attack/Dodge/Hurt/Death em oito direções. A arma se conecta ao `WeaponSocket` na mão e Attack expõe um Animation Event de impacto.
- `HuntzRigPilotController` usa Input System e `Rigidbody2D.linearVelocity`; o evento de impacto consulta alvos na direção travada. Espaço esquiva, C troca a paleta e E interage com NPCs/portais.
- O contato do slime graybox dispara Hurt e dano; esquiva oferece invulnerabilidade temporária, Death bloqueia movimento e R reinicia o ator. Essas rotas estão implementadas, mas precisam de validação manual no Play Mode.
- `Huntz/Prototype/Build Graybox Pilot Environment` criou uma MainLand isométrica, borda e obstáculos 2D, Ana/Bruno como manequins rigados, mercado/oficina cenográficos, portal para uma área de treino e alvos de combate. Tudo é placeholder de geometria/cor.
- A ponte MCP do Unity respondeu. `refresh_unity` concluiu compilação sem erros C#; o Console retornou somente o aviso interno `[WebSocket] Unexpected receive error: WebSocket is not initialised`. O pacote `com.unity.2d.animation` 16.0.1 já estava instalado; não foi adicionado pacote.
- A Build Settings foi lida via MCP: `HuntzVerticalSlice.unity` continua a única cena de build. A nova cena não substituiu a fatia nem foi adicionada à build.
- Captura MCP do Play Mode mostra o HUD do piloto, a ação `Attack / NorthEast` e feedback do portal. Isso não é validação completa de movimento nas oito direções, dano, esquiva, colisões ou ida/volta; nenhuma suíte automatizada foi criada/executada.
- Não houve geração de arte nem consumo de créditos neste incremento. PixelLab/Higgsfield ficam fora do fluxo até o dono reabrir a etapa de arte.

## Histórico 2D — espada e ataque 8D

- O jogador carrega `StarterSword` através de `HuntzWeaponDefinition` e `HuntzEquippableWeapon`; pode equipar uma definição ou sprite e somar bônus de dano, alcance e largura.
- `Hero_Attack_8D_6F.png` fornece seis quadros sem arma embutida nas oito vistas a 7,14 fps. East é substituído pela folha `Hero_Attack_East_12F.png`, com 12 quadros a 14 fps. Ambos duram cerca de 0,85 s.
- O jogador trava a direção; ao entrar no quadro de índice 2 (seis quadros) ou 4 (East), `HuntzDirectionalAnimator.FrameChanged` dispara uma vez a pose de impacto, o arco visual e a consulta de dano. A pose da espada dura 16% do clip, acompanhando a seção de contato.
- Originais, manifesto, recipe e quadros extraídos estão em `Assets/Huntz/Art/Source/Characters/Hero/AttackCycle`. O importador `Tools/import_huntz_animation.py` monta Source/Final. No Editor, use `Huntz/Animation/Import Hero Attacks 8D` e `Huntz/Combat/Import Starter Sword`; não execute `HuntzSliceBuilder`.
- O refresh compilou sem erros; `IsHeroAttackSetImported`, `IsHeroAttackEastImported` e `IsStarterSwordImported` retornaram verdadeiro. `HuntzAnimationTools.ValidateScene` retornou zero. Um hit temporário em SouthEast registrou frame 2, impacto resolvido, espada/arco visíveis e vida 2→1. O alvo foi destruído e o Play Mode encerrado sem salvar a cena.
- O Editor agora está em Edit Mode, na cena `HuntzVerticalSlice`. O Console final não tinha erros nem avisos. Um aviso interno do WebSocket do MCP apareceu durante refresh, mas não impediu compilação nem importação.
- A articulação corporal além do lunge ainda é sutil; revise a leitura da espada e pontos de mão em escala de jogo. Dodge/Hurt/Death continuam arte provisória.
- PixelLab ficou sem trial após cinco gerações usadas nesta continuação; US$ 0 pagos. O AutoSprite de Higgsfield retornou 404 ao tentar buscar a referência do PixelLab. O conector não aceita um caminho de arquivo local pela via suportada; não foram usados créditos Higgsfield (saldo 84). A folha foi concluída sem geração paga/trial usando o pixel-art workbench do PixelLab.

## Histórico 2D — base de caminhada de 2026-10-06

- Walk redesenhado no PixelLab: oito quadros × oito direções, 14 fps. Originais, referências, jobs/seeds/prompts e manifestos em `Art/Source/Characters/Hero/WalkCycle`. Montagem nearest 2×, quadros 208 × 192, folha 1664 × 1536, apoio em y=93 do canvas fonte.
- A fonte antiga NorthWest olhava para a direita. O primeiro resultado foi rejeitado e preservado só como histórico em Source; a caminhada usa `NorthWest_Corrected`. Idle NorthWest também foi substituído por quatro quadros novos. Não houve espelhamento.
- `Tools/import_huntz_animation.py` baixa, confere, alinha e monta a arte. Suporta uma folha inteira ou substituir direções sobre uma folha base. Produz contato/GIF/hashes em Library antes de `--apply`.
- `Tools/build_huntz_art.py` preserva a caminhada e o Idle revisados. `--only hero-walk` restaura somente a folha Walk; o algoritmo de passos por recortes de Idle foi removido.
- `HuntzDirectionalAnimator` mantém fase ao virar, expõe ação/frame/velocidade no Inspector e suporta direção `None` para criaturas e efeitos. Todos os sprites animados da cena e dos prefabs foram vinculados explicitamente a renderer/conjunto. Nove conjuntos, 81 clips; componentes Legacy removidos.
- Player usa velocidade física para Walk e conserva o vetor de ataque/esquiva durante a ação. Durações de ações vêm dos assets. Slime para ao morrer; chefe retoma sua fase após Hurt.
- `HuntzAnimationTools` vincula/valida cena e prefabs. A build usa validação estrutural. `HuntzAnimationPreview` oferece reprodução, scrub e linha de pivô no Editor.
- HUD corrigido: estilos GUI não persistem como objetos vazios entre recargas; texturas/ícones são serializados e foram vinculados via MCP.
- Cena/prefabs foram atualizados e salvos via Unity MCP. O builder principal **não** foi executado; nenhuma reconstrução da vila.

## Verificação do piloto 2D (histórico)

- Refresh e compilação C# no Editor; cena sem scripts ausentes e sem erros de validação.
- Movimento/parada nas oito direções pelo Input System, com todos os novos ciclos até F07. North foi repetido em área livre: no primeiro trajeto a colisão com o portal parava o corpo e selecionava Idle.
- Capturas reais de passos em `Assets/Screenshots/Hero_Walk_East_Runtime_*.png`. HUD final em `Huntz_Walk_Foundation_Ready.png`.
- Ataque e esquiva pelo Input System até F05/F03; Hurt e Death por `TakeHit` até F01/F07. Isso verifica transições, não o combate completo.
- Portal acionado por E do Input System após posicionar o personagem perto dele: entrou na sala 0, dois slimes, ciclos Idle F00–F03 e Move F00–F05 observados.
- Dispositivos/callbacks temporários removidos; runInBackground restaurado. Nenhuma suíte de testes foi criada ou executada.
- Console final do Play Mode sem erros/avisos. Houve mensagens nativas de GC handle numa recarga intermediária; não reapareceram na conferência final.
- Build Windows x64 **0.1.3**, `Builds/HuntzPrototype_0.1.3_WalkFoundation/Huntz.exe`, 106,49 MB, zero erros e dois avisos: `Hidden/Core/DebugOccluder` e `Hidden/Core/DebugOcclusionTest` não compatíveis com URP. O executável não foi aberto para uma partida.
- Editor foi deixado fora do Play Mode, com HuntzVerticalSlice salva e como única cena de build.

## Próximo trabalho da fase 2D (histórico)

1. No piloto, conferir manualmente WASD em oito direções, colisões/limites da ilha, interação dos NPCs/lojas, portal ida/volta, esquiva, troca de paleta e impacto/dano nos alvos.
2. Ajustar escala e ordem de desenho da espada/socket. O graybox usa partes rígidas presas aos ossos; não usa `SpriteSkin` ponderado.
3. Só depois da validação integrar esse contrato ao guerreiro e às demais entidades da cena de build. Manter a fatia principal intacta enquanto o piloto não estiver aprovado.
4. Não gerar arte neste ciclo. Atualizar STATUS/ART_PIPELINE/HANDOFF conforme o resultado manual e não duplicar fontes da verdade em PROJECT_MAP.

## Ferramentas, custo e continuidade na fase 2D (histórico)

- A ponte Unity MCP em `http://127.0.0.1:8080/mcp` respondeu nesta continuação. A cena principal foi aberta e confirmada sem alterações antes de criar o piloto. Estado final do Editor: Edit Mode, cena ativa `Assets/Huntz/Prototype/HumanoidRigPilot/Scenes/HumanoidRigPilot.unity`; a única cena de Build Settings continua `HuntzVerticalSlice`.
- Para importar só Walk: `Huntz/Animation/Import Hero Walk`. Para regenerar conjuntos dos .anim: `Huntz/Build Directional Sprite Sets`. Para validar: `Huntz/Animation/Validate Active Scene`.
- Para importar/rever Attack: `Huntz/Animation/Import Hero Attacks 8D`, `Huntz/Animation/Import Hero Attack East` e `Huntz/Animation/Preview`. Para item: `Huntz/Combat/Import Starter Sword`.
- `Huntz/Animation/Bind Active Scene and Character Prefabs` altera as referências da cena e dos prefabs animados; salvar a cena depois. Não executar o builder principal para atualizar animações.
- Para criar o piloto inicial, use `Huntz/Prototype/Create Humanoid Rig Pilot` somente se a pasta ainda não existir. Para reconstruir apenas o grupo de ambiente cinza gerado e completar o slot Body do prefab, use `Huntz/Prototype/Build Graybox Pilot Environment`.
- Saldo PixelLab atual: zero trial e US$ 0,00 pagos. Higgsfield: 84 créditos não usados porque AutoSprite não aceitou a URL de referência (404).
- `HuntzEquippableWeapon` recebe o `HuntzDirectionalAnimator` para ocultar a camada se algum conjunto ainda estiver usando os clips fallback antigos com lâmina embutida; no conjunto Attack 8D importado, a camada permanece visível.
- Git tinha extensa reorganização staged e alterações unstaged antes desta revisão. Elas foram preservadas; não houve commit, push ou reset. Checkpoint anterior: `accccd5`; sem remoto configurado.
- A cena antiga HuntzIsoRPG está preservada fora do projeto em `C:/Users/victo/Documents/Huntz/Recovery/HuntzIsoRPG_2026-10-06/`; não reintroduzi-la em Assets. A cena ativa do Editor é o piloto; a fatia `HuntzVerticalSlice` segue a única cena da build.
