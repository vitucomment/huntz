# Huntz — plano de assets e animações

> **Estado em 2026-10-06:** o kit artístico do protótipo e a integração jogável foram concluídos. Este documento mantém o histórico e o contrato para extensões. A entrega atual está resumida em [`PROTOTYPE_ASSET_PLAN.md`](PROTOTYPE_ASSET_PLAN.md) e [`HANDOFF.md`](HANDOFF.md).

Este documento organiza a substituição dos placeholders visuais do protótipo por assets reutilizáveis. As quantidades de frames são propostas iniciais para orientar a produção; não mudam o GDD nem as regras de gameplay.

## Escopo

### Protótipo jogável — escopo atual

- Personagem jogável.
- Ash Slime.
- Guardião da Fenda.
- Piso da MainLand e das salas da hunt, limites e passagens.
- Portal HZ e efeitos do portal.
- Mesa de preparação/bigorna.
- Cristal/Núcleo da Fenda.
- Efeitos de ataque, investida, pulso, dano e cura.
- HUD da MainLand e expedição: painéis, barras, mapa, ícones e resumo.

Também fazem parte da entrega os assets usados pelo protótipo `HuntzIsoRPG`: árvores, pedras, casas, poço, baús, NPCs, água, seis variações de terreno, slimes e Slime Rei. A demo de plataforma permanece apenas como referência. O plano de execução e os critérios estão em [`PROTOTYPE_ASSET_PLAN.md`](PROTOTYPE_ASSET_PLAN.md).

## Convenções iniciais

- Pixel art está confirmada como direção visual.
- Preservar a escala existente como ponto de partida: tile isométrico de 64×32 px a 64 PPU.
- Sprites de personagens e objetos altos usam pivô na base; personagens ficam alinhados pelos pés.
- Todas as células de uma folha de animação de um mesmo personagem usam o mesmo tamanho e o mesmo pivô.
- Importação prevista: Sprite, modo Multiple para folhas, Filter Mode Point e compressão desligada.
- Guardar arte final em pasta separada dos PNGs temporários gerados pelo builder.

## Padrão de personagens modulares

A direção escolhida é produzir personagens em 8 direções completas e montar variações a partir de partes compatíveis, em vez de desenhar cada personagem final como uma imagem única. O primeiro padrão será `Humanoid Standard v1 — Warrior`, com proporção humana média, armadura leve e espada.

- Partes iniciais: corpo/base de pele, cabelo traseiro, cabelo frontal, rosto, roupa superior, roupa inferior, mãos/luvas, calçados e equipamento/arma.
- Cada conjunto de partes usa a mesma grade de quadros, dimensões de célula, pivô nos pés, escala e nomes de pose. As direções são S, SW, W, NW, N, NE, E, SE.
- Nome de pose: `<Ação>_<Direção>_<Quadro>` (por exemplo, `Walk_SW_03`); cada slot resolve esse mesmo rótulo para seu próprio sprite.
- A ordem de desenho pode mudar por direção; cabelo e arma podem precisar de partes dianteiras e traseiras separadas.
- Variações de cabelo, tom de pele e roupa usam o mesmo contrato de poses. Cada roupa ainda precisa de sprites próprios para as ações e direções que cobre.
- Modelos femininos podem começar como variações compatíveis do padrão humanoide; proporções ou roupas que alterem a silhueta ganham um conjunto corporal próprio alinhado ao mesmo contrato.
- Criaturas animalescas terão anatomia e sprites próprios, mas podem reutilizar a organização de estados, nomes de ações, importador e ferramentas de prefab.
- Evitar fragmentar cada pixel ou membro em uma peça. Criar slots separados onde o jogador realmente poderá escolher ou equipar algo.

Escolha confirmada: montagem quadro a quadro por camadas, sem deformação por ossos na primeira etapa. `com.unity.2d.animation` 16.0.1 foi instalado para Unity 6000.6 e será usado com `Sprite Library`/`Sprite Resolver`: as bibliotecas organizam sprites por categoria e rótulo, e variantes podem herdar uma biblioteca-base. Cada slot visual terá seu próprio `SpriteResolver`; uma aparência seleciona bibliotecas/overrides para corpo, cabelo, roupa e equipamento, enquanto os clips usam rótulos de pose sincronizados. A deformação por ossos continua opcional. A primeira validação será quadro a quadro para preservar pixel art nítida.

### Ações previstas

Contagens são metas iniciais por direção e serão refinadas após validar a montagem modular.

| Arquétipo | Ações previstas em 8 direções | Observações |
|---|---|---|
| Humanoid Standard | Idle (4), Walk (6), Attack (6), Dodge (4), Hurt (2), Death (8) | Um controlador e um vocabulário de poses; aparência composta por corpo, cabelo, roupas e equipamento. |
| Ash Slime | Idle (4), Move (6), Lunge (4), Hurt (2), Death (6) | Animação própria de criatura; compartilhar a estrutura de assets entre slimes da mesma silhueta. |
| Guardião da Fenda | Idle (4), Walk (6), Charge (6), Dash (5), Pulse (6), Recover (4), Hurt (2), Death (8) | Arte de chefe própria; manter telegráficos de investida e pulso legíveis. |
| Portal HZ | Loop (8) | Um loop reutilizável entre cenas, com cor/intensidade ajustável. |
| Cristal/Núcleo | Idle/Brilho (4), Coleta (4) | Loop reutilizável para cristais iguais, com variação de cor. |

## Reuso e direção

- Fazer oito direções desenhadas; não usar espelhamento como substituto das diagonais.
- Um estado de animação escolhe poses sincronizadas de todas as partes visuais do personagem.
- Aparências compatíveis compartilham nomes de pose, pivô, escala e controlador. Variações de anatomia podem compartilhar estados sem compartilhar os mesmos quadros.
- Reutilizar efeitos por material/cor quando isso não esconder áreas de perigo.

## Integração no Unity

1. Fechar a ficha do `Humanoid Standard v1`: grade, escala, pivô, poses, slots de aparência e ordem de desenho por direção.
2. Montar uma prova jogável com Idle e Walk em 8 direções, incluindo pelo menos duas opções de cabelo, pele e roupa.
3. Validar a troca de aparência no prefab e o alinhamento durante movimento antes de criar os demais clips.
4. Produzir Attack, Dodge, Hurt e Death para o padrão validado; depois estender o conjunto às classes humanoides compatíveis.
5. Produzir Ash Slime, Guardião, portal, cristal e efeitos com seus próprios arquétipos e animações.
6. Criar sprites e prefabs dos objetos estáticos reutilizáveis e substituir as formas do cenário.
7. Integrar o builder da fatia e os builders isométricos aos sprites finais e prefabs reutilizáveis.
8. Conferir as duas cenas no Unity e colocar `HuntzVerticalSlice` como primeira cena da build.

## Estado do primeiro teste

O primeiro passe de arte foi concluído e já está integrado: `Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/` contém folhas de Idle, Walk, Attack, Dodge, Hurt e Death em oito direções. Os clips estão em `Assets/HuntzSlice/Animations/Hero/` e são controlados por `HuntzDirectionalAnimator`; o prefab `Assets/HuntzSlice/Prefabs/Warrior.prefab` usa esse visual durante o combate e o movimento.

A prova `BodyBase_Idle_8D` continua em Drafts: a extração de transparência deixou franja colorida e não foi importada como arte final. O guerreiro da build é uma folha completa por ação/direção; cabelo, pele, roupa e equipamento ainda não estão separados em camadas selecionáveis. Essa etapa amplia o padrão `Humanoid Standard` depois do feedback do protótipo e não está incluída nesta build.

## Ferramentas

- Unity MCP está conectado e oferece operações para importar texturas, criar prefabs e montar clips/controllers.
- PixelLab MCP está conectado. A conta trial mostrou 34 gerações restantes no inventário de 2026-10-06; conferir o saldo antes de enfileirar qualquer geração.
- Referência gerada: `Huntz Humanoid Standard v1 - Base`, ID `fb1bcad7-baed-4119-b229-379c222daf7c`, oito rotações, canvas final 92×92 px, câmera high top-down. As vistas foram baixadas em `Assets/HuntzSlice/Art/Drafts/HumanoidStandard/PixelLab_Base/` e revisadas visualmente; são uma referência monolítica, ainda sem camadas nem prefab.
- Preview Unity criado em `Assets/HuntzSlice/Scenes/HumanoidStandardPreview.unity`; menu **Huntz → Build Humanoid Standard Preview** reconfigura as oito imagens como sprites Point/Uncompressed, 64 PPU, pivô nos pés, e mostra cada direção sobre um tile da slice. A cena é independente e não foi adicionada aos Build Settings.
- O personagem `Huntz Humanoid Standard v1 - Base` (`fb1bcad7-baed-4119-b229-379c222daf7c`) é uma referência neutra provisória, não a aparência aprovada do protagonista. O grupo Idle registrado pelo serviço contém quatro quadros em oito direções; esses sprites ficam em Drafts e não substituem a folha do guerreiro aprovada.
- Uma imagem do Ash Slime foi gerada como referência visual preliminar. Ela não substitui a folha de animação aprovada.
