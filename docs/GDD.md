# Huntz — visão do jogo e proposta de design

**Versão:** 0.3 · **Atualizado:** 2026-10-07
**Base:** notas originais preservadas em docs/historico/Anotações_originais.txt.
**Estado:** proposta de produto. O dono considera a fatia vertical jogável validada, mas as regras abaixo que não aparecem em docs/STATUS.md continuam sendo planos ou sugestões.

**Direção confirmada pelo dono em 2026-10-07:** RPG de ação isométrico 3D, combate em tempo real inspirado no gênero de Diablo/Path of Exile, guerreiro inicial e personagens montados sobre rig humanoide reutilizável. A produção de arte está adiada; o piloto usa apenas geometria graybox da Unity.

## Conceito

Huntz é um RPG de ação isométrico sobre um guerreiro que morreu no mundo real e foi escolhido pela Guilda para lutar por toda a eternidade. Da MainLand, um asteroide suspenso no espaço-tempo, o jogador atravessa portais para caçar criaturas que invadem e corrompem diferentes universos. A apresentação de jogo passa a usar mundo, câmera, personagens, cenário e física 3D.

Cada hunt é uma expedição por salas conectadas. O jogador escolhe rotas, enfrenta combates, procura recompensas opcionais e tenta chegar ao chefe final. Ao retornar, usa os recursos obtidos para ajustar seu personagem e se preparar para hunts mais perigosas.

## Pilares de experiência

1. **Explorar realidades instáveis:** cada universo apresenta tema visual, criaturas, perigos e chefes próprios.
2. **Escolher o próprio risco:** avançar direto ao objetivo ou desviar para salas desconhecidas em busca de recompensas.
3. **Montar uma build flexível:** combinar classe, atributos, habilidades e equipamentos sem exigir uma classe específica para cada item.
4. **Ler e responder ao combate:** em perspectiva isométrica, ataques, áreas de perigo e oportunidades de defesa precisam ser visíveis e compreensíveis.

## Ciclo principal

1. Na MainLand, conversar com NPCs para comprar, guardar, fabricar ou aprimorar itens e distribuir pontos.
2. Consultar o painel do Portal HZ e escolher universo, hunt e dificuldade disponíveis.
3. Entrar na hunt, explorar suas salas e decidir a cada bifurcação entre uma rota curta e salas opcionais.
4. Derrotar o chefe na sala final e concluir a expedição.
5. Voltar à MainLand com as recompensas, fortalecer a build e desbloquear desafios seguintes.

**Direção recomendada:** combate em tempo real, coerente com o protótipo isométrico existente. A câmera deve manter o jogador e os perigos legíveis; ataques inimigos importantes podem usar avisos no chão antes de atingir.

## MainLand e universo

### MainLand

A MainLand é a base segura do jogador: um asteroide flutuando no espaço-tempo. Ela concentra o Portal HZ, lojas, armazém e serviços de progressão. O espaço pode crescer com a história, mas a primeira versão deve manter poucos NPCs e funções claras.

Serviços previstos nas anotações:

- **Painel do Portal HZ:** seleção das hunts liberadas.
- **Lojas:** equipamentos e consumíveis.
- **Armazém:** guardar e retirar equipamentos e materiais.
- **Treinador da Guilda:** distribuir pontos e evoluir habilidades.

**Sugestão:** dar uma função narrativa curta a cada NPC e manter os serviços acessíveis sem diálogos longos. Isso faz da base um lugar reconhecível sem atrasar o retorno ao combate.

### Universos e hunts

O plano prevê **5 universos**, com **5 hunts em cada universo**. Cada universo tem uma identidade visual e temática própria; a sequência de hunts conduz o jogador de desafios mais baixos aos mais altos. O painel indica o nível mínimo e a dificuldade antes da entrada.

Cada hunt oferece três tamanhos de mapa:

| Dificuldade | Grade de salas | Intenção sugerida |
|---|---:|---|
| Fácil | 3×3 | Aprender padrões, rota curta e risco moderado |
| Média | 4×4 | Mais desvios, encontros variados e recompensa maior |
| Difícil | 6×6 | Expedição longa, elites e maior potencial de recompensa |

Os tamanhos acima vêm das anotações. Recomenda-se que a dificuldade também altere a força e composição dos encontros, a quantidade de salas especiais e as recompensas — e não apenas o tamanho do mapa.

## Estrutura de uma hunt

### Mapa e navegação

O mapa da hunt é uma grade lógica de salas; cada sala, quando jogada, é um espaço de combate construído em perspectiva isométrica. Essa separação permite variar rotas sem exigir que todo o cenário de combate seja gerado do zero.

- A entrada **I** fica no canto superior esquerdo e a saída **F** no canto inferior direito.
- As salas são conectadas às vizinhas ortogonais; o jogador só pode avançar para uma sala adjacente.
- Depois de limpar uma sala, o jogador pode seguir ou voltar. Salas já limpas continuam sem inimigos durante aquela expedição.
- O mapa deve sempre conter pelo menos uma rota possível de I até F.
- A sala F contém o chefe final. Salas opcionais podem conter baús, desafios ou chefes surpresa.

**Sugestão de informação ao jogador:** mostrar a geometria e as conexões conhecidas, mas esconder o tipo e a recompensa das salas ainda não visitadas. O conteúdo é revelado ao entrar. Assim, a escolha de direção tem risco, sem depender de adivinhação sobre como o mapa funciona.

### Tipos de sala sugeridos

- **Combate:** encontro padrão; as saídas abrem quando todos os inimigos são derrotados.
- **Elite:** inimigos mais perigosos, com chance melhor de materiais ou equipamento.
- **Recompensa:** baú ou escolha entre consumíveis, moeda e materiais.
- **Desafio:** armadilhas, ondas de inimigos ou objetivo opcional com limite de tempo.
- **Chefe opcional:** encontro difícil com recompensa própria; não substitui o chefe final.
- **Chefe final:** encontro obrigatório na sala F, encerra a hunt quando vencido.

As anotações indicam que recompensas extras e chefes surpresa mudam de posição a cada geração. Recomenda-se variar a posição dentro de regras de geração: proteger I e F, garantir um caminho até o final e impedir que salas opcionais apareçam em caminhos sem saída inacessíveis.

### Geração procedural

Para manter as hunts variadas e justas, a geração pode sortear primeiro a topologia e os tipos de sala, e depois selecionar arenas isométricas feitas à mão a partir de conjuntos compatíveis. O procedural decide **a sequência e a combinação**; arenas e encontros são peças revisadas, em vez de geometria inteiramente aleatória.

Cada configuração deve passar por regras de validação: rota de I a F, chefe final alcançável, nenhuma sala especial inacessível e quantidade mínima/máxima de salas de cada tipo. A semente da geração pode ser guardada durante a expedição para que voltar a uma sala mantenha o mesmo conteúdo.

### Morte e retorno

Regra definida nas anotações: morrer em uma hunt transporta o personagem para a MainLand e reduz sua experiência, mas não reduz seu nível.

**Regra recomendada para o primeiro protótipo:** a perda de experiência é limitada à barra do nível atual; nunca remove experiência acumulada de níveis anteriores. Para evitar uma punição dupla, itens e materiais já coletados permanecem com o jogador. Consumíveis usados continuam gastos. O painel deve explicar a perda antes da entrada.

## Personagem e progressão

O jogador escolhe uma entre quatro classes. Os equipamentos não são bloqueados por classe ou nível; os requisitos são atributos mínimos.

| Classe | Identidade de combate sugerida | Atributo de foco |
|---|---|---|
| Guerreiro | Aguentar pressão, bloquear e interromper inimigos | Força |
| Arqueiro | Atacar à distância, marcar alvos e reposicionar-se | Agilidade |
| Mago | Controlar áreas e combinar efeitos elementais | Inteligência |
| Assassino | Mobilidade, ataques críticos e dano concentrado | Agilidade e Sorte |

As identidades são propostas, não restrições: habilidades e equipamentos devem permitir variações dentro de cada classe.

### Atributos

- **Força:** dano físico e acesso a equipamentos pesados.
- **Agilidade:** velocidade de ataque, mobilidade e requisitos de armas ágeis.
- **Inteligência:** poder mágico e eficiência de efeitos elementais.
- **Sorte:** chance de efeitos especiais e oportunidades de saque.

O plano concede 3 pontos de atributo por nível. **Sugestão de equilíbrio:** Sorte não deve aumentar dano e qualidade de saque sem limites ao mesmo tempo; use retornos decrescentes ou limites para impedir que um único atributo supere todos os outros.

### Habilidades

Cada classe tem até 8 habilidades: 5 ativas e 3 passivas. Cada habilidade pode receber até 10 pontos. As passivas são liberadas nos níveis 10, 15 e 20. O plano prevê 1 ponto de habilidade por nível e nível máximo 80.

Há um pequeno ajuste de contagem a decidir: do nível 1 ao 80 existem 79 subidas de nível. Para que o personagem tenha exatamente 80 pontos no nível 80 e consiga maximizar as 8 habilidades, recomendo conceder 1 ponto inicial no nível 1 e mais 1 a cada nível seguinte.

**Sugestões para as habilidades:**

- Cada ativa deve oferecer uma função clara — dano, mobilidade, defesa, controle ou área — e continuar útil quando recebe pontos.
- A evolução de nível pode melhorar números e, em marcos, alterar a habilidade de forma perceptível. Evite que dez pontos sejam apenas aumentos pequenos de dano.
- As cinco ativas podem ser equipadas simultaneamente; as passivas ficam sempre ativas depois de aprendidas. Um serviço na Guilda pode permitir redistribuir pontos, idealmente com custo moderado para apoiar experimentação.
- A barra de habilidades deve funcionar com teclado e controle, com avisos de cooldown e custo visíveis.

## Equipamentos e itens

### Espaços e armas

O personagem pode equipar elmo, armadura, arma principal, arma secundária e artefato cósmico. Armas de duas mãos ocupam os dois espaços de arma.

Tipos de arma previstos: espada de uma mão, espada de duas mãos, adaga, cajado, varinha, escudo, orbe protetor e arco. A adaga pode ser usada em cada mão. Recomenda-se definir explicitamente o arco como arma de duas mãos e explicar no inventário quais combinações bloqueiam a arma secundária.

### Requisitos e escala por atributo

Os equipamentos têm raridades Normal, Incomum, Raro, Lendário e Divino. Cada equipamento nomeado tem identidade e raridade fixas: uma espada específica não aparece como cópias aleatórias de raridades diferentes. Os requisitos de atributo podem envolver um ou dois atributos.

Para tornar previsível a escala descrita nas anotações, uma fórmula inicial pode ser:

`valor efetivo = valor-base + coeficiente × max(0, atributo − requisito mínimo)`

Para itens ligados a dois atributos, cada atributo contribui com seu próprio coeficiente. Os coeficientes e limites precisam ser balanceados no jogo; a fórmula acima é apenas uma proposta transparente, não um valor final.

### Propriedades e aprimoramento

As armas podem oferecer poder de ataque, velocidade, chance e dano crítico; escudos e orbes podem oferecer bloqueio, defesa, resistência elemental e HP; armaduras oferecem defesa, bloqueio, resistência e HP; elmos podem oferecer chance e dano crítico. Os elementos previstos são fogo, gelo, terra e raio.

Artefatos cósmicos concedem efeitos especiais e podem vir de drops, fabricação ou lojas. Recomenda-se que cada artefato mude uma decisão de build — por exemplo, melhorar uma habilidade, favorecer um elemento ou recompensar um estilo de risco — além de simplesmente aumentar atributos.

Equipamentos podem ser aprimorados de +1 a +15 com Núcleos de Aprimoramento. Derreter itens gera fragmentos; 100 fragmentos formam um núcleo completo.

**Sugestões de economia:**

- Tornar os aprimoramentos garantidos no primeiro sistema, com custo crescente de núcleos. Falhas que destroem ou rebaixam o equipamento adicionariam frustração e dificultariam o balanceamento inicial.
- Dar marcos visíveis em +5, +10 e +15, como um bônus adicional ou efeito visual, para que a progressão tenha momentos perceptíveis.
- Informar antes de derreter um item quanto material será recebido e confirmar itens equipados ou favoritos.
- Usar bônus fixos e coeficientes limitados para que raridade e aprimoramento não multipliquem o crescimento de atributos sem controle.

## Chefes, recompensas e dificuldade

Cada universo pode ter criaturas, efeitos ambientais e um chefe temático que ensina uma mecânica própria. O chefe final deve testar habilidades aprendidas ao longo da hunt, com sinais claros para ataques de área, janelas de defesa e oportunidades de contra-ataque.

**Sugestão de recompensa:** garantir ao menos uma recompensa temática ao vencer o chefe, além de moeda e materiais. Salas opcionais podem aumentar a quantidade ou oferecer escolhas, para que explorar seja atraente sem ser obrigatório para concluir.

As três dificuldades podem compartilhar o tema e as arenas, mas variar composição dos inimigos, modificadores, duração da rota e recompensa. Evite depender apenas de aumentar HP e dano: isso tende a tornar a dificuldade mais demorada sem exigir novas decisões.

## Protótipo ativo

O protótipo jogável ativo está em `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`. Ele apresenta uma MainLand graybox, rig humanoide 3D, câmera isométrica ortográfica, movimento responsivo, corrida e esquiva com vigor, ataques sincronizados pelo Animator, dano, NPCs e portais. A cena agora também tem um primeiro ciclo de Hunt: Portal HZ, mapa lógico 3×3 Fácil, encontros, rotas entre salas, uma sala de saque aleatória, um chefe surpresa, chefe final e retorno à MainLand. `docs/STATUS.md` registra o que foi implementado e o que ainda precisa ser validado jogando.

Nesta primeira implementação, as nove salas reusam a arena graybox da Hunt; portais transportam o guerreiro entre nós adjacentes e a HUD mostra o mapa. O portal inicia diretamente a Hunt 01 Fácil. Médio (4×4), Difícil (6×6), seleção de destino, arenas próprias e persistência da expedição ainda não estão disponíveis. Fragmentos de fenda são uma carteira provisória em memória: encontros dão 1, a sala de saque dá 3, o chefe surpresa dá 3 e o chefe final dá 5. Eles sobrevivem à morte durante a sessão; não há XP, níveis ou salvamento persistente para aplicar a perda de experiência das anotações.

O personagem e sprites da antiga direção 2D estão aposentados como base para o jogo. Os arquivos antigos permanecem no repositório somente por histórico e não entram na cena, no rig ou no pipeline atuais. A cena 3D é graybox, não representa arte final nem valida por si só o balanceamento, a hunt completa, progressão ou personagens importados.

### Controles implementados no piloto

- WASD ou setas movem relativo à câmera; aceleração e desaceleração suavizam a resposta.
- Shift segura a corrida e consome vigor, que se regenera após uma breve pausa.
- A roda do mouse aproxima ou afasta a câmera ortográfica suavemente; o zoom fica limitado para manter o guerreiro e o espaço de combate legíveis.
- Clique esquerdo ou J atacam na direção do mouse registrada no início de cada golpe; segurar repete ataques. Em movimento, o guerreiro vira para a direção percorrida e mantém a orientação do golpe até a animação terminar. O jogador ainda pode se mover durante o golpe, com velocidade reduzida.
- Espaço esquiva na direção pressionada; sem direção, usa o deslocamento/facing atual. A esquiva cancela o golpe, gasta vigor, tem recarga e oferece uma janela curta de invulnerabilidade.
- E interage, C alterna aparência graybox e R reinicia. O piloto ainda não tem mapeamento de controle/gamepad.

A próxima prova é jogar o ciclo novo no Play Mode: limpar encontros, usar saídas, revisitar salas, abrir o baú, encontrar o chefe surpresa, derrotar o chefe final, sair pelo portal e morrer numa tentativa. Depois disso, ajustar combate e arenas e começar progressão persistente. Lojas, armazém, atributos, habilidades, classes e itens equipáveis descritos nas anotações seguem no roteiro; os NPCs atuais ainda são pontos de interação provisórios.

O plano de 5 universos e 25 hunts é visão de longo prazo, não escopo da fatia atual. Primeiro valide uma hunt curta com rotas, encontros, chefe e recompensas legíveis; só então aumente a grade e a quantidade de conteúdo.

## Direções confirmadas

- RPG de ação isométrico 3D com câmera fixa/ortográfica no piloto.
- Guerreiro como primeiro personagem jogável.
- Humanos 3D compartilham uma hierarquia de ossos, Avatar/Animator e socket de equipamento; a rotação do modelo cobre as direções do mundo.
- Corpos, roupa, cabelo, armadura e acessórios devem poder variar em materiais/partes sobre o mesmo contrato de rig, quando isso mantiver proporções e silhuetas compatíveis.
- A antiga cena 2D e seus assets não são referência de design; ficam somente no arquivo histórico. A nova cena 3D é a única entrada de Build Settings.
- A arte gerada está pausada. A aparência atual é geometria provisória construída dentro do Editor da Unity.

## Decisões ainda abertas

1. O jogo será exclusivamente single-player ou terá modo cooperativo?
2. O esquema final de controles e suporte a gamepad; o piloto usa movimento WASD com orientação pelo deslocamento e mira pelo mouse ao iniciar ataques, ainda a validar manualmente com entrada do jogador.
3. Quais recompensas ficam permanentes ao morrer e qual é a punição exata de experiência?
4. Haverá recursos diferentes por classe, mana universal ou habilidades limitadas por cooldown?
5. Como as quatro classes diferem em habilidades e função quando compartilham equipamentos?
6. Quais sistemas da visão longa pertencem ao primeiro jogo completo: geração procedural, economia, aprimoramento, crafting e número de universos?
