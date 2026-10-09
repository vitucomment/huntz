# Pipeline de arte e contrato de assets

## Fluxo 3D ativo — arte pausada

- Huntz é um jogo 3D isométrico. A antiga direção de sprites pixel-art foi aposentada; não reutilize seus personagens, folhas ou animações como referência ou fonte de assets 3D.
- O protótipo ativo está em `Assets/Huntz/Prototype/Isometric3D`. Até o dono reabrir produção de arte 3D, use somente primitivas, materiais e meshes graybox gerados por ferramentas do Editor da Unity.
- O contrato humanoide usa `Animator` com Generic Avatar e uma hierarquia Transform de ossos: `Hips`, `Spine`, `Chest`, `Neck`, `Head`, ombros, braços, mãos, pernas, pés e `WeaponSocket` na mão direita.
- `HuntzArpg3DRigPart` identifica slots de pele, tecido, couro, metal, cabelo e acento. `HuntzArpg3DAppearanceProfile` guarda paletas; `HuntzArpg3DAppearance` aplica cores sem instanciar materiais.
- Idle/Run/Attack/Dodge/Hurt/Death são clips do Animator. O evento do Attack chama o impacto da arma e sincroniza o dano.
- As partes cinza atuais são meshes rígidos filhos dos ossos. Não há `SkinnedMeshRenderer`, pesos de vértices, animação importada ou arte final. Ao importar o modelo, preserve a hierarquia/nomenclatura ou atualize o mapeamento do rig antes de usar as animações.
- A captura do piloto e a cena aparecem em STATUS. Todas as seções seguintes são arquivo histórico do pipeline 2D aposentado; não são instruções para gerar, importar ou validar conteúdo do jogo atual.

## Pastas do fluxo 2D preservado

- Assets/Huntz/Art/Final: arte integrada e referenciada por cenas/prefabs. A pasta não significa que a qualidade visual já seja definitiva.
- Assets/Huntz/Art/Source: fontes necessárias para reconstruir saídas geradas.
- Assets/Huntz/Art/Reference: estudos visuais que não são usados em runtime.
- Assets/Huntz/Animations: clips sincronizados por personagem/estado.
- Assets/Huntz/Resources/Huntz/Animations: conjuntos de sprites para animação dos personagens, criaturas e efeitos em runtime.
- Assets/Huntz/Prefabs: objetos visuais e de gameplay reutilizáveis.

Não colocar estudos ou imagens de referência na pasta Final. Toda imagem usada no jogo precisa permanecer acompanhada do arquivo .meta correspondente.

## Conteúdo 2D legado já integrado

- Guerreiro: Idle (4 quadros), Walk (8), Attack (6 quadros em sete vistas e 12 em East), Dodge (4), Hurt (2), Death (8). Attack agora tem corpo sem arma em todas as vistas; a espada equipada é uma camada separada. Dodge, Hurt e Death ainda usam arte provisória.
- NPCs: Ana e Bruno, Idle em oito direções.
- Ash Slime e Slime King: Idle, Move, Hurt, Death.
- Guardião da Fenda: Idle, Charge, Pulse, Recover, Hurt, Death.
- Portal HZ, Núcleo da Fenda, efeitos de espada, pisos, props compartilhados e elementos de HUD.
- Prefab Warrior e prefabs de inimigos, props e salas são os pontos de integração reutilizáveis.

O guerreiro atual é uma folha completa por estado/direção. Ele ainda não troca partes de aparência em runtime.

## Espada equipada e contrato de ataque 2D — 2026-10-07 (legado)

- `Art/Source/Shared/Weapons/manifest.json` registra a geração PixelLab da `StarterSword.png`; a saída de jogo está em `Art/Final/Shared/Weapons/StarterSword.png`. O importador do Editor configura sprite único, PPU 64, Point, sem compressão, pivô `(0.16, 0.18)` e cria `Resources/Huntz/Weapons/StarterSword.asset` como definição equipável.
- `HuntzEquippableWeapon` cria uma camada de arma no visual do guerreiro e expõe `Equip(HuntzWeaponDefinition)` e `Equip(Sprite, ...)`. O jogador carrega a espada de treino por padrão. O sprite do corpo não contém uma segunda espada durante Attack.
- East usa 12 quadros a 14 fps; as outras vistas usam seis quadros a 7,14 fps. Canvas 104 × 96, PPU 64 e pivô de pés uniforme. O ciclo dura 0,84–0,86 s e o contato corporal é curto; a espada independente dá a maior parte da leitura do arco. Fontes e quadro-a-quadro estão em `Art/Source/Characters/Hero/AttackCycle`.
- `attack_8d_workbench_manifest.json` e `Hero_Attack_8D_6F_Workbench_Recipe.json` preservam fontes, oito vistas, ordem/tempo dos quadros, frame de impacto e desenho do PixelLab workbench. `Hero_Attack_8D_6F_Source.png` e `Art/Final/Characters/Hero/Sheets/Hero_Attack_8D_6F.png` são folhas de seis quadros; East permanece em `Hero_Attack_East_12F.png`.
- `Tools/import_huntz_animation.py` extrai folhas em grade do workbench, revisa e aplica Source/Final. O menu `Huntz/Animation/Import Hero Attacks 8D` fatia as folhas, atualiza os clips e o conjunto runtime sem reconstruir a cena. O postprocessor agenda a importação quando os arquivos são importados.
- `HuntzDirectionalAnimator.FrameChanged` sinaliza a entrada do quadro de impacto. Direção travada, espada, arco e consulta única de dano usam esse evento comum: quadro de índice 2 nas folhas de seis quadros e índice 4 em East. A pose da espada mantém contato por 16% da duração do clip.
- No Unity 6000.6.4f1, as duas folhas e `StarterSword` foram importadas, C# compilou sem erros e a validação da cena retornou zero. No Play Mode, o golpe SouthEast acertou um Ash Slime temporário: vida 2→1, impacto resolvido, espada e arco visíveis. Play Mode encerrou sem salvar a cena.
- PixelLab usou cinco gerações trial nesta continuação e o saldo foi a zero (US$ 0,00 pagos). A folha 8D foi montada sem gerações usando o pixel-art workbench gratuito.
- Higgsfield AutoSprite foi tentado como alternativa; retornou 404 ao buscar a imagem de referência hospedada pelo PixelLab. O conector não aceita um caminho local para transferência suportada. Nenhum dos 84 créditos Higgsfield foi consumido. O arquivo final ficou no projeto, mas a arte corporal tem lunge discreto e merece revisão visual futura.

## Reprodução e revisão dos frames 2D (legado)

Os clips em `Assets/Huntz/Animations` mantêm as curvas `SpriteRenderer.m_Sprite` como fonte dos frames e os metadados de velocidade/loop. `Huntz/Build Directional Sprite Sets` extrai essas curvas e grava os nove conjuntos em `Assets/Huntz/Resources/Huntz/Animations`. Hero tem 48 clips; Ana e Bruno têm oito cada. Criaturas e efeitos usam `direction = None`. O menu não reconstrói a cena.

`HuntzDirectionalAnimator` atribui cada frame diretamente ao `SpriteRenderer`. Cena e prefabs mantêm referências explícitas ao conjunto e ao renderer; a busca por Resources é somente uma compatibilidade para instâncias antigas. O runtime dos sprites usa esse componente, incluindo portal, slimes, chefe e espada. Os `.anim` continuam sendo dados de importação; não adicione um componente Legacy concorrente ao renderer.

`Huntz/Animation/Preview` permite escolher ação/direção, reproduzir, reiniciar e percorrer os quadros em escala inteira. A linha marca o pivô. Confira movimento das pernas, alternância do apoio, continuidade do loop, silhueta e arma. Contar frames diferentes ou observar nomes F00/F01 no Inspector não comprova uma caminhada visualmente legível.

`Huntz/Animation/Bind Active Scene and Character Prefabs` vincula os conjuntos à cena aberta e aos prefabs animados, sem recriar a MainLand. Salve a cena após usar esse menu. `Huntz/Animation/Validate Active Scene` verifica referências explícitas, sequências completas, dimensões, pivô, PPU e filtro Point. A build executa a mesma validação. Isso detecta problemas estruturais, mas a aprovação visual continua necessária.

O guerreiro preserva a fase da caminhada ao virar e ajusta sua reprodução pela velocidade física. Ataque/esquiva travam sua direção durante a ação. Tempos visuais de ataque, dano e morte vêm dos conjuntos, evitando cortar os últimos quadros ao trocar a arte.

## Contrato de importação de pixel art (legado)

Use as especificações existentes em Assets/Huntz/Editor/HuntzAnimationAssetBuilder.cs antes de importar folhas novas. As escalas variam por família; não aplique um único PPU a todos os sprites. Preserve:

- oito vistas nomeadas de forma consistente: South, SouthWest, West, NorthWest, North, NorthEast, East e SouthEast;
- a mesma grade, dimensões de quadro e pivô nos pés para partes que serão compostas;
- filter Point, transparência e compressão sem perdas para pixel art;
- leitura clara de silhueta, ataque, perigo, dano e estados terminais em escala real de jogo.

## Humanoide modular 2D com rig — piloto graybox 2026-10-07 (legado)

As referências PixelLab estão em Art/Reference/Characters/HumanoidStandard/PixelLab_Base. Elas ajudam a comparar forma e proporção, mas não são sprites finais nem um personagem montado.

O piloto do motor está em `Assets/Huntz/Prototype/HumanoidRigPilot`. `Huntz/Prototype/Create Humanoid Rig Pilot` cria a primeira versão do prefab/Animator; `Huntz/Prototype/Build Graybox Pilot Environment` monta a ilha, as colisões, os NPCs, os portais e a área de treino dentro da cena isolada. Essa cena não pertence a Build Settings e não substitui a fatia jogável atual.

O prefab usa `Animator` sobre uma hierarquia Transform: `Rig/Pelvis`, tronco/cabeça, braços com antebraço/mão e pernas com canela/pé. Os slots de sprite ficam em `HuntzHumanoidRigPart`; `HuntzHumanoidAppearanceProfile` escolhe rótulos e tintas para `SpriteLibrary`/`SpriteResolver`. `WeaponSocket` fica na mão direita e recebe o item equipado. A composição por partes rígidas mantém cada sprite nítido enquanto acompanha os ossos; não usa malha deformável `SpriteSkin` neste primeiro contrato.

O Animator do graybox cria Idle, Walk, Attack, Dodge, Hurt e Death para as oito orientações. Idle/Walk fazem loop; Attack inclui `AnimationEvent_WeaponImpact`, que sincroniza o item visual com a consulta direcional de dano. O slime placeholder também causa dano por contato; Dodge fornece uma janela curta de invulnerabilidade, Death bloqueia controle e `R` reinicia o piloto. As partes atuais são retângulos de placeholder gerados pelo próprio Editor e duas paletas de teste, sem arte final ou geração externa. `WASD` move, `J` ataca, `Espaço` esquiva, `C` troca a paleta, `E` interage com portais/NPCs e `R` reinicia após a morte. O ambiente usa sprites simples, `Rigidbody2D`, colliders 2D e `EdgeCollider2D` na borda da ilha.

O pacote instalado é `com.unity.2d.animation` 16.0.1. Sprite Library/Resolver já participam da prova de variação. Ainda faltam revisar a ordem frente/costas em escala de jogo, testar manualmente todas as oito direções e decidir se alguma peça se beneficia de `SpriteSkin` ponderado. Não crie variantes de personagens nem gere arte antes dessa validação do contrato.

Variações masculinas/femininas podem compartilhar pose e pipeline quando a silhueta permitir. Personagens animalescos compartilham ferramentas e estados, não precisam compartilhar anatomia ou sprites.

## Geração e revisão de pixel art (legado)

Tools/build_huntz_art.py é o gerador determinístico atual e escreve em Art/Final. As fontes do guerreiro, Ash Slime e Guardião da Fenda estão em Art/Source/Characters. Antes de regenerar, leia as saídas que a ferramenta escreve e revise o diff; ela pode substituir PNGs existentes. Para corrigir somente a animação de morte do guerreiro, use `python Tools/build_huntz_art.py --only hero-death`; sem essa opção, o comando gera o pacote inteiro.

### Caminhada do guerreiro — revisão de 2026-10-06

- `Art/Source/Characters/Hero/WalkCycle/manifest.json` registra jobs, seeds e prompts do PixelLab. A referência foi reduzida com nearest para 104 × 96; os oito quadros gerados de cada direção voltam a 208 × 192 com escala inteira 2×. A folha tem 1664 × 1536, PPU 128 e 14 fps.
- O índice zero retornado pelo serviço é a referência parada; somente os índices 1–8 entram na caminhada. Os originais ficam em Source. A montagem alinha o apoio em y=93 no canvas 104 × 96, próximo ao pivô de pés da Unity, sem deslocar o personagem horizontalmente a cada quadro.
- O primeiro NorthWest foi rejeitado porque a fonte antiga olhava para a direita. `NorthWest_Corrected` contém a geração que olha à esquerda, sem espelhamento. `idle_nw_manifest.json` substitui somente a linha noroeste do Idle, preservando as demais.
- `python Tools/import_huntz_animation.py Assets/Huntz/Art/Source/Characters/Hero/WalkCycle/manifest.json --download` prepara folha, GIF e hashes em `Library/HuntzArtReview`. Revise visualmente e repita com `--apply` para gravar Source/Final; depois use `Huntz/Animation/Import Hero Walk` no Editor. Para o Idle, use seu manifesto; dimensões e recortes existentes são preservados no refresh.
- `Tools/build_huntz_art.py --only hero-walk` copia a fonte revisada. A reconstrução completa preserva a caminhada e o Idle revisados; não recria passos deslocando pedaços de Idle.
- Foram consumidas 20 gerações trial (incluindo correção de vista e Idle); saldo final consultado: 12 trial e US$ 0,00. Nenhuma compra. Higgsfield não foi usado nesta revisão.

A revisão visual de Dodge/Hurt/Death continua pendente, inclusive Northwest. O corpo dos ataques 8D mantém as poses base do guerreiro e desloca o root de forma discreta; confira a leitura em escala de jogo e refine a articulação corporal quando necessário. Não considerar todos os assets definitivos só porque o runtime avança os frames.

PixelLab serve para criar bases e conjuntos coerentes de pixel art. Defina proporção, canvas, paleta, vista, pose, pivô e animação antes da geração. Confira o saldo antes de jobs que consumam créditos; não compre saldo ou assinatura sem autorização explícita. Faça o acabamento em ferramenta de pixel art quando for necessário controle por pixel.

Higgsfield também pode ser testado como fonte de pixel art, ao lado do PixelLab. Trate cada saída como rascunho até conferir grade de pixels, transparência, paleta, dimensões de quadros, oito direções, pivô e leitura em movimento na Unity. AutoSprite foi tentado nesta continuação, mas não conseguiu buscar a URL de referência do PixelLab (404); não foram consumidos créditos. Saldo consultado: 84 créditos. A folha 8D atual foi feita no PixelLab pixel-art workbench, sem geração paga/trial.
