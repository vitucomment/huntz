# Huntz — Visão do jogo e mecânicas

**Versão:** rascunho 0.1  
**Base:** `Anotações.txt`, organizado e ampliado para orientar as próximas decisões de design.  
**Gênero definido:** RPG isométrico 2D.  
**Estado:** conceito; as sugestões deste documento ainda precisam ser validadas no protótipo.

## Conceito

Huntz é um RPG isométrico 2D sobre um guerreiro que morreu no mundo real e foi escolhido pela Guilda para lutar por toda a eternidade. Da MainLand, um asteroide suspenso no espaço-tempo, o jogador atravessa portais para caçar criaturas que invadem e corrompem diferentes universos.

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

## Primeiro marco jogável recomendado

Os 5 universos e 25 hunts são um bom destino de conteúdo, mas um volume grande para validar antes de fechar combate, geração e progressão. Um primeiro marco deve provar o ciclo completo com:

- MainLand pequena com Portal HZ, loja, armazém e treinador;
- um universo temático;
- uma hunt com grade 3×3, salas conectadas, uma rota opcional e retorno por salas limpas;
- combate, morte e recompensa funcionando do início ao fim;
- um chefe final e uma sala de recompensa ou elite;
- duas classes contrastantes para validar builds — sugestão: Guerreiro e Mago;
- um conjunto pequeno de equipamentos, requisitos de atributo e aprimoramento básico.

Depois de o ciclo funcionar e ser divertido, o mesmo sistema pode ser ampliado para as dificuldades 4×4 e 6×6, outras hunts e os demais universos.

## Decisões em aberto

Estas escolhas não estão definidas nas anotações e devem ser fechadas antes de detalhar o balanceamento:

1. O jogo será exclusivamente single-player ou terá modo cooperativo?
2. Qual será o estilo visual: pixel art, ilustração HD ou outro?
3. Como o jogador mira e ataca: direção do personagem, cursor, alvo selecionado ou combinação?
4. Quais recompensas ficam permanentes ao morrer? Este rascunho recomenda manter o que foi coletado e perder apenas experiência da barra atual.
5. A raridade será fixa por equipamento nomeado em todos os casos? Este documento assume que sim, como indicam as anotações.
6. Haverá recursos diferentes por classe, mana universal ou habilidades limitadas apenas por cooldown?
7. Qual é o tom da história e o que significa servir à Guilda “por toda a eternidade” para o personagem?

