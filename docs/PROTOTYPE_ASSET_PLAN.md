# Huntz — plano de conclusão do protótipo visual e jogável

**Data:** 2026-10-06  
**Escopo autorizado:** substituir os placeholders das duas cenas de RPG isométrico, integrar assets reutilizáveis e deixar a fatia vertical pronta para abrir e jogar. A demo de plataforma continua apenas como referência.

## Decisão de direção

- O protagonista é um **guerreiro**. A folha já existente `Hero_Idle_8D.png` mostra um guerreiro pixelado com armadura leve e espada; ela é a referência aprovada, não o personagem neutro de `HumanoidStandardPreview`.
- Arte pixelada, projeção isométrica, 8 direções, pivôs nos pés e resolução nítida com Point filtering.
- A cena jogável principal é `HuntzVerticalSlice`: MainLand, seis salas, Ash Slimes, Guardião da Fenda, recompensas, melhoria e retorno.
- `HuntzIsoRPG` também recebe o kit visual compartilhado: seis terrenos, árvores, pedras, casas, poço, baús, guerreiro, NPCs e slimes. A cena de plataforma não entra neste marco.
- O personagem mantém slots visuais separados para corpo, equipamento e efeitos quando isso preserva alinhamento e facilita trocar a aparência. A camada de gameplay e colisões continua independente da arte.

## Inventário visual que precisa virar asset

| Grupo | Assets reutilizáveis | Uso |
|---|---|---|
| Guerreiro | folha de Idle 8D; folhas de Walk, Attack, Dodge, Hurt e Death; prefab e controlador direcional | Jogador nas duas cenas |
| Humanoides | variações de aparência compatíveis com os mesmos pivôs e rótulos | Anciã Ana e Ferreiro Bruno |
| Criaturas | Ash Slime animado e variação Slime Rei; Guardião da Fenda e estados do chefe | Ambas as cenas |
| Terreno | grama, grama escura, trilha, praça/pedra, água, areia, piso da hunt e MainLand | Mapas antigos e arenas |
| Ambiente e props | árvore, rocha, casa, telhado, poço, baú, limite de sala, passagem, portal, bigorna/mesa, cristal/Núcleo | Mapas e ciclo da hunt |
| Efeitos | golpe da espada, aviso circular, faixa de investida, impacto, dano, cura e brilho do portal/cristal | Combate e interação |
| Interface | moldura de painel, barras, ícones de vida/Núcleo e marcadores do mapa | HUD da fatia vertical |

## Fases e critérios de conclusão

1. **Contrato e proteção:** inventariar sprites e mecânicas das cenas, fixar paleta/escala/pivô, manter builders reproduzíveis e salvar versões antes de trocar visuais.
2. **Guerreiro jogável:** importar os 8 direções de Idle, ligar animação à direção e estado; produzir Walk, Attack, Dodge, Hurt e Death; verificar o prefab no movimento e no combate.
3. **Criaturas e chefe:** criar sprites e prefabs de Slime/Ash Slime, Slime Rei e Guardião; incluir ciclos de movimento, ataque, dano e morte. Conservar os avisos de perigo contrastantes.
4. **Cenários e objetos:** gerar texturas pixeladas individuais e prefabs reutilizáveis para o terreno e cada prop do inventário. Usar os mesmos assets onde os dois mapas compartilham o tema.
5. **HUD e integração:** aplicar a paleta e painéis aos HUDs; tirar geração procedural de sprites de dentro dos builders; reconstruir as duas cenas usando os assets finais sem mudar as regras de jogo.
6. **Entrega jogável:** deixar `HuntzVerticalSlice` na primeira posição do Build Settings; confirmar compilação, cena MainLand, movimento, combate, esquiva, interações, bifurcação, chefe, vitória/derrota e retorno. Produzir build local e instruções de controles.

## Regras de produção

- Manter PNGs finais em `Assets/HuntzSlice/Art/Final/` e `Assets/IsoRPG/Art/Final/`; drafts permanecem em `Drafts/`.
- Folhas usam grade uniforme por personagem/ação; cada direção preserva o mesmo pivô nos pés. Sprites importam como Sprite/Multiple, Point, Uncompressed, sem mipmaps.
- Prefabs de jogador, inimigos, NPCs, portal, cristais, baús, árvores e casas guardam aparência/colisão em componentes separados quando necessário.
- Builders de cena devem carregar os assets existentes em vez de recriar PNGs provisórios e sobrescrever o trabalho final.
- A conta PixelLab trial tem 34 gerações no momento do inventário. Priorizar a ferramenta de pixel art sem custo, a arte já gerada e o reuso. Não iniciar tarefas pagas fora do saldo disponível.
- A verificação automatizada não substitui a jogada manual de teclado. O MCP consegue inspecionar estados e transições, mas não envia WASD/J/E/Espaço.

## Estado da entrega — 2026-10-06

- [x] Inventário da fatia vertical e do mapa isométrico anterior.
- [x] Guerreiro confirmado como protagonista; arte de oito direções nos estados Idle, Walk, Attack, Dodge, Hurt e Death.
- [x] Kit visual final criado para terreno, props, criaturas, efeitos e interface em `Assets/HuntzSlice/Art/Final/`.
- [x] Animações, controlador direcional e prefabs reutilizáveis integrados em `Assets/HuntzSlice/Animations/` e `Assets/HuntzSlice/Prefabs/`.
- [x] Cena principal MainLand → hunt → recompensa/melhoria/retorno reconstruída e adicionada como primeira cena de Build Settings.
- [x] Protótipo `HuntzIsoRPG` reconstruído com o kit compartilhado e prefabs em `Assets/IsoRPG/Prefabs/`.
- [x] Compilação e execução das duas cenas conferidas no Unity; screenshots em `Library/HuntzCaptures/`.
- [x] Build Windows x64 produzida em `Builds/HuntzPrototype_v0.1.0/`.
- [ ] Jogada manual por teclado e mouse pelo dono. O MCP não injeta WASD, E, Espaço, J ou clique; por isso, estados e transições foram verificados sem simular a partida completa.

O guerreiro atualmente usa folhas completas sincronizadas de animação. A separação visual real em camadas de cabelo, pele e roupas — necessária para um criador de personagens — permanece uma expansão posterior do padrão `Humanoid Standard`, e não é usada para bloquear a build jogável deste protótipo.
