# Mapa do projeto Huntz

## Projeto ativo

Unity 6000.6.4f1, URP 17.6.0, RPG de ação isométrico 3D. O foco é evoluir a fatia `Isometric3DARPGPilot` para uma base de combate e movimento consistente. Os antigos sprites/modelos 2D são arquivo histórico, não referência para conteúdo novo; a produção de arte 3D está pausada.

## Estrutura

- Assets/Huntz/Animations: clips por personagem e estado.
- Assets/Huntz/Art: sprites e fontes da direção 2D aposentada; não usar no jogo 3D.
- Assets/Huntz/Editor: builders de cena e importação/animação.
- Assets/Huntz/Prefabs: guerreiro, criaturas, props e salas reutilizáveis.
- Assets/Huntz/Prototype/Isometric3D: piloto 3D ativo, materiais graybox, meshes de ilhas, Animator, perfis de aparência, prefabs e cena.
- Assets/Huntz/Prototype/HumanoidRigPilot: arquivo histórico 2D, fora do fluxo atual e sem função de referência para o personagem novo.
- Assets/Huntz/Scenes: cena antiga HuntzVerticalSlice.unity, preservada e fora de Build Settings.
- Assets/Huntz/Resources/Huntz/Animations: conjuntos de frames em runtime para humanoides, criaturas, portal, núcleo e espada.
- Assets/Huntz/Resources/Huntz/Weapons: definições de armas carregadas por `HuntzEquippableWeapon`.
- Assets/Huntz/Scripts: runtime ativo nas classes `HuntzArpg3D*`; scripts `HuntzSlice*`, `HuntzDirectional*` e `HuntzHumanoid*` pertencem ao arquivo 2D.
- Assets/Settings: assets compartilhados do URP.
- Packages: manifest e lock do Unity Package Manager.
- Packages/com.coplaydev.unity-mcp: pacote MCP embutido, versão local 10.3.0.
- ProjectSettings: configuração da Unity e cena de build.
- Tools/build_huntz_art.py: gerador determinístico de arte pixelada legado; não usar enquanto a produção de arte estiver pausada.
- Tools/import_huntz_animation.py: baixa frames de um manifesto, alinha os pés e prepara folhas direcionais ou de uma direção para revisão e importação.
- docs: documentação canônica do jogo e do fluxo.
- plugins: Figma e Higgsfield disponíveis localmente.
- .agents: manifesto do marketplace local de plugins.

Library, Temp, Logs, Builds e UserSettings são estado local/gerado e não fazem parte do código-fonte versionado. Builds fica no disco local, mas é ignorado pelo Git.

## Cena 3D ativa e código

- `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity` é a única cena em `ProjectSettings/EditorBuildSettings.asset`.
- `Assets/Huntz/Editor/HuntzIsometric3DPilotBuilder.cs` oferece `Huntz/Prototype 3D/Build Isometric ARPG Pilot`; ele gera a cena e os assets graybox em `Assets/Huntz/Prototype/Isometric3D`. `Refresh Humanoid Rig Prefab` atualiza o rig/avatar compartilhado sem reconstruir a cena; `Refresh Locomotion Animations` refaz os clips do builder. `Refresh Island Collision` cria uma malha física independente da apresentação e migra a cena ativa sem rebuild; as duas ilhas foram atualizadas e verificadas no Editor.
- O menu não destrutivo `Huntz/Prototype 3D/Install Hunt Vertical Slice` instala o ciclo de Hunt na cena ativa sem reconstruir a MainLand ou os prefabs.
- `HuntzArpg3DPlayerController` usa Input System, `CharacterController`, movimento acelerado, corrida/vigor, ataque mantido e cancelável, esquiva direcional, dano, interação, carteira provisória de fragmentos e HUD. O dano corpo a corpo é resolvido por uma cápsula entre a base e a ponta da arma no evento de impacto, uma vez por alvo por golpe.
- `HuntzArpg3DHuntDirector` inicia a Hunt 01 Fácil pelo Portal HZ, gera um grafo 3×3 de salas conectado, controla encontros e saídas adjacentes, preserva salas limpas durante a expedição, cria sala de saque e chefe surpresa em posições aleatórias, encerra no chefe C3, trata morte/retorno e desenha o mapa da rota.
- `HuntzArpg3DGrayboxKitBuilder` oferece `Huntz/Prototype 3D/Build Graybox Arena Kit` e gera cinco prefabs em `Prototype/Isometric3D/Environment/GrayboxKit`. Cada módulo mantém malha visual em `Presentation` e um `BoxCollider` em `Collision`. `HuntzArpg3DGrayboxArenaBuilder` combina esses módulos num piso 5×5 e nas variantes de combate, saque e chefe; o `HuntDirector` escolhe a variante pelo tipo do nó da Hunt.
- Combate usa quatro coberturas baixas, saque usa quatro paredes baixas e uma placa para o baú, e chefe usa quatro plataformas com pilares. Os layouts têm 25/26/25 peças de piso, respectivamente. Inimigos perseguem usando `CharacterController` e fazem desvio local de coberturas, paredes e pilares.
- Validação de arena no Play Mode via Unity MCP confirmou colliders configurados em todos os módulos, 4 m de esquiva livres nas oito direções, abertura visual de portal de 1,04 m para o personagem de 0,68 m, e linha de câmera até o personagem sem cruzar módulos nas três variantes. A cápsula da espada acertou a 1 m e não acertou a 2 m ou 3 m na pose de impacto.
- `HuntzArpg3DRig`, `HuntzArpg3DRigPart`, `HuntzArpg3DAppearance` e `HuntzArpg3DAppearanceProfile` formam a hierarquia de ossos Transform, slots de aparência e `WeaponSocket`. O componente mantém referências estáveis às juntas principais e valida a estrutura. `LeftToeBase`/`RightToeBase` articulam o antepé.
- `HuntzArpg3DWeaponGeometry` identifica base, ponta e raio de contato da arma e desenha a cápsula no Editor. A espada graybox multipart fica presa ao socket da mão direita; inventário e troca de equipamento ainda não estão implementados.
- `HuntzArpg3DTarget` publica derrotas para a Hunt; `HuntzArpg3DInteractable` aceita handlers de gameplay e continua atendendo os NPCs; `HuntzArpg3DCameraFollow` mantém câmera ortográfica com zoom suave pela roda do mouse.
- O builder 3D gera a vila graybox, fachadas de loja, trilha de lajotas até o portal e um marco astral. Esses elementos usam geometria e materiais nativos; alterações futuras no builder devem ser aplicadas pelo menu do Editor, que regenera a cena e os assets dentro de `Assets/Huntz/Prototype/Isometric3D`.
- O dono validou o percurso completo da Hunt, incluindo o retorno à MainLand após morte. O kit e os três arranjos de arena estão integrados ao fluxo atual; cada sala lógica usa o layout da sua função, sem um desenho único por nó. O próximo marco é uma passagem manual do dono nos três layouts para confirmar sensação de colisão, leitura isométrica e conforto de esquiva com input real. A produção de arte continua pausada.
- O rig graybox usa peças rígidas ligadas aos ossos. O Animator mantém o Avatar Generic para os clips atuais, cujas curvas animam Transform diretamente; `HuntzHumanoid3D.asset` guarda o mapeamento Humanoid separado para retargeting. Os clips ainda precisam ser convertidos antes de ativar esse Avatar. Malha skinning ponderada e importação de modelo final ainda não foram implementadas.

## Arquivo histórico 2D — fora do jogo atual

- `Assets/Huntz/Scenes/HuntzVerticalSlice.unity`, `Assets/Huntz/Editor/HuntzSliceBuilder.cs` e as pastas de sprites/animações são conteúdo aposentado. Não use seus modelos, sprites, rigs ou sistemas como base para novas features 3D.
- O builder monta a MainLand como um asteroide 14 × 14, com estrelas, borda rochosa, praça, casas, poço, Ana, Bruno e colisores sólidos.
- Assets/Huntz/Editor/HuntzAnimationAssetBuilder.cs configura folhas, clips e conjuntos direcionais de sprites.
- Assets/Huntz/Editor/HuntzWeaponAssetBuilder.cs importa a espada de treino e gera sua definição em Resources, sem reconstruir a cena.
- Assets/Huntz/Scripts/HuntzDirectionalAnimator.cs escolhe e avança os frames do `SpriteRenderer`; `HuntzDirectionalSpriteSet.cs` guarda ações com oito direções ou uma sequência `None` para criaturas e efeitos.
- Assets/Huntz/Editor/HuntzAnimationTools.cs vincula cena/prefabs aos conjuntos e valida referências, direções, quadros, pivôs e filtro. A validação também protege a build.
- Assets/Huntz/Editor/HuntzAnimationPreview.cs permite reproduzir e percorrer quadros no Editor, em escala inteira, com linha do pivô.
- Assets/Huntz/Scripts contém movimento, combate, inimigos, chefe, salas, transições e HUD.
- `HuntzSlicePlayer` aplica dano uma vez no evento do quadro de impacto nas oito direções; `HuntzEquippableWeapon` mantém o sprite e os atributos do item equipado. Attack usa corpo de seis quadros nas sete vistas e 12 quadros em East, sem arma embutida; o sprite da espada é uma camada equipada.
- O piloto `HumanoidRigPilot` usa `HuntzHumanoidRig`/`HuntzRigPilotController`, `SpriteLibrary` e `Rigidbody2D`; não é o rig 3D ativo.
- Assets/Huntz/Art/Reference/Characters/HumanoidStandard/PixelLab_Base é estudo de forma; não está integrado à cena.

## Fluxo de evolução

O piloto 3D é a única referência de gameplay. Os arquivos 2D ficam fora do jogo e do fluxo de evolução. Consulte STATUS para o que foi realmente conferido e HANDOFF para retomar o próximo incremento.

## Mapeamento das anotações originais para o piloto 3D

| Intenção das anotações | Estado atual do protótipo |
|---|---|
| MainLand como asteroide e acesso por Portal HZ | Graybox 3D jogável; Portal HZ inicia Hunt 01 Fácil. NPCs aparecem, mas serviços de loja/ferreiro continuam em texto provisório. |
| Hunts por salas conectadas, A1 até C3 | Grafo lógico 3×3 conectado, gates entre vizinhos e retorno a salas limpas. Nós de combate, saque e chefe usam um dos três layouts graybox; cada nó ainda não tem desenho exclusivo. |
| Recompensas e chefes surpresa mudam de sala | Cada run sorteia um baú e um chefe surpresa em salas distintas, fora da entrada e do chefe final. |
| Morrer na Hunt volta para a MainLand e perde XP | Retorno automático e cura implementados; fragmentos já coletados são mantidos. XP/níveis ainda não existem no piloto, então a perda de XP não está implementada. |
| Dificuldades Fácil/Média/Difícil em grades 3×3/4×4/6×6 | Somente Fácil 3×3 está habilitada; demais configurações são roteiro. |
| Guerreiro, arqueiro, mago, assassino, atributos e habilidades | Guerreiro graybox e rig compartilhável existem. Classes jogáveis, XP, níveis, atributos e árvores de habilidades ainda não existem. |
| Equipamento, lojas, armazém e aprimoramento | Espada graybox no socket do rig; inventário, loot equipável, economia, stash, crafting e upgrades ainda não existem no fluxo 3D. |
| Cinco universos e 25 Hunts | Visão de longo prazo; o piloto só apresenta uma Hunt graybox de treino. |

## Ferramentas do protótipo 2D (legado)

Os menus `Huntz/Animation/Import Hero Walk`, `Huntz/Animation/Import Hero Attack East`, `Huntz/Animation/Import Hero Attacks 8D`, `Huntz/Build Directional Sprite Sets`, `Huntz/Combat/Import Starter Sword`, `Huntz/Animation/Bind Active Scene and Character Prefabs` e `Huntz/Animation/Validate Active Scene` atualizam ou verificam animação/equipamento sem reconstruir a cena. O menu de bind altera também os prefabs animados; salve a cena depois. O builder principal continua reservado a uma reconstrução deliberada do cenário.

A MainLand 2D antiga tem praça, mercado/oficina cenográficos, poço e moradores Ana/Bruno com diálogo. O controlador usa Input System e física 2D; a velocidade física orienta a caminhada. Esses dados descrevem somente `HuntzVerticalSlice` e `HumanoidRigPilot`, não a cena ativa 3D.
