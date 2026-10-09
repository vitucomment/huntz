# AGENTS.md — regras para agentes do Huntz

## Antes de editar

1. Leia este arquivo, docs/STATUS.md, docs/PROJECT_MAP.md e o handoff atual.
2. Confira git status e abra somente os arquivos relevantes à tarefa.
3. Trate docs/GDD.md como visão e propostas. Uma mecânica descrita como ideia não está implementada nem aprovada só por estar escrita.
4. Mantenha o trabalho alinhado ao pedido mais recente do dono. Preferências e permissões explícitas persistem entre sessões.

## Direção já definida

- Huntz é um RPG de ação isométrico 3D, com combate em tempo real na linha de Diablo/Path of Exile.
- A cena ativa de build é `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`. O conteúdo 2D antigo é arquivo histórico fora da build; não reutilize seus modelos, sprites, animações ou contratos visuais no jogo 3D.
- Humanoides 3D usam uma hierarquia de ossos nomeados, Animator e socket de equipamento. A malha skinned final e a integração de personagens produzidos ainda estão pendentes.
- A produção de arte está adiada. Use apenas geometria e materiais provisórios da Unity até o dono reabrir essa etapa.
- A base Unity é 6000.6.4f1 com URP 17.6 e o novo Input System.

## Regras de implementação

- C# e comentários de código em inglês; texto apresentado ao jogador em português brasileiro.
- Use `UnityEngine.InputSystem`; não use a API antiga `UnityEngine.Input`. O piloto ativo usa movimento no plano XZ, `CharacterController` e colliders 3D. Código 2D arquivado pode continuar com `Rigidbody2D.linearVelocity`, mas não deve alimentar features novas.
- A fórmula `world = ((gx - gy) * 0.5, (gx + gy) * 0.25)`, tile lógico 1 × 0.5, pertence ao conteúdo 2D legado; não a aplique ao piloto 3D.
- No rig 3D, mantenha o chão em y=0, a raiz do personagem junto aos pés e os nomes dos ossos estáveis. Regras Point/compressão desligada se aplicam à arte pixelada legada.
- Preserve arquivos .meta e seus GUIDs ao mover assets. Não edite cenas ou prefabs YAML manualmente quando o Unity MCP estiver disponível.
- Prefabs separam apresentação de colisão e gameplay quando isso ajuda a reutilização. Evite abstrações genéricas sem um uso real.
- Não crie scripts patch_* temporários. Transforme uma correção em código/ferramenta reutilizável ou faça a edição diretamente; remova temporários da mesma mudança.

## Unity e cenas

- A cena ativa de build fica em `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`.
- `HuntzIsometric3DPilotBuilder` recria a cena graybox 3D e os assets gerados somente em `Assets/Huntz/Prototype/Isometric3D`; confira a cena antes de reconstruí-la.
- `HuntzSliceBuilder` ainda recria a fatia 2D legada em `Assets/Huntz/Scenes/HuntzVerticalSlice.unity`; não o execute para o fluxo 3D.
- Antes de mudar cenas, prefabs ou dependências, confira o estado do Editor e a cena aberta. Depois de editar scripts no disco, use refresh_unity, aguarde o fim da compilação e leia o Console por erros C#.
- Se a ponte MCP estiver indisponível, faça mudanças no sistema de arquivos que sejam seguras e reversíveis; não afirme que o Editor compilou ou que o jogo foi testado.
- Capturas geradas pelo MCP ficam em Assets/Screenshots e são ignoradas pelo Git.

## Assets, dependências e custo

- Final, Source e Reference têm funções diferentes; consulte docs/ART_PIPELINE.md. Referências não são assets de gameplay.
- Prefira recursos já instalados e APIs nativas da Unity. Antes de adicionar pacotes ou serviços, explique a necessidade, verifique compatibilidade/licença e mantenha a mudança pequena.
- Não iniciar compra, upgrade, assinatura ou gasto pago de PixelLab sem orçamento explícito. Antes de consumir gerações existentes, confira o saldo e concentre cada pedido num resultado verificável.
- Nunca copie tokens, credenciais, arquivos de configuração pessoal ou dados privados para o repositório, documentos, saída de terminal ou commits.
- Não instale plugins ou pacotes só porque estão disponíveis. O inventário e o uso recomendado estão em docs/TOOLS.md.

## Git, segurança e conclusão

- Confira git status antes de começar e antes de terminar. Preserve mudanças do usuário; nunca use reset --hard, clean, force push ou descarte amplo.
- O dono autorizou commits locais e pushes para `origin` neste repositório sem pedir confirmação a cada operação; preserve essa autorização entre sessões.
- Antes de cada push, revise o conteúdo para excluir tokens, credenciais, configurações pessoais, dados privados e arquivos locais ou gerados. Nunca use force push.
- Use uma identidade de commit configurada ou indicada pelo dono; não invente nem mascare autoria.
- A autorização para commits e pushes não inclui publicar builds, enviar mensagens externas ou criar outros recursos externos.
- Não adicione nem execute testes automatizados sem o dono pedir. Faça a verificação do Editor apropriada para a tarefa e informe exatamente o que não pôde ser conferido.
- Ao mudar estrutura, gameplay ou pipeline de arte, atualize apenas os documentos canônicos afetados: STATUS, PROJECT_MAP, ART_PIPELINE ou GDD; atualize HANDOFF para a próxima sessão. Nunca mantenha dois documentos como fontes conflitantes.
- No fim, resuma mudanças, verificação realizada e limitações. Só pergunte quando uma ambiguidade alterar produto/custo ou quando faltar uma decisão necessária; resolva escolhas reversíveis com julgamento e contexto existentes.
