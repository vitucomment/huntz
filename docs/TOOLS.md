# Ferramentas recomendadas para Huntz

Revisto em 2026-10-07. Inventário de capacidade, não pedido para instalar novas integrações.

## Núcleo do fluxo atual

| Ferramenta | Estado observado | Uso recomendado |
|---|---|---|
| Unity 6000.6.4f1 + URP 17.6 | Base do jogo 3D isométrico | Engine, renderização, física 3D e build. A direção não volta ao protótipo 2D. |
| MCP for Unity 10.3.0 | Pacote embutido; conectado e usado nesta sessão | Inspecionar Editor/Console, executar o builder graybox 3D, validar cena e conferir Build Settings. A cena ativa fica em STATUS; instruções em UNITY_MCP.md. |
| Novo Input System 1.20 | Ativo | WASD/setas, Shift, mouse/J, esquiva e interação no piloto 3D. Suporte a gamepad ainda não está configurado. |
| Cinemachine 6.6.0 | Instalado; não usado pelo piloto atual | O piloto usa uma câmera ortográfica fixa com follow pequeno em código. Considerar Cinemachine se zoom, limites, blending ou composição dinâmica virarem requisito. |
| ProBuilder 6.1.2 | Instalado; não usado pelo builder atual | Disponível para blockout e modelagem manual. O asteroide de teste usa mesh gerada pelo Editor para manter sua geração repetível. |
| PixelLab MCP | Configurado, fora do fluxo ativo | Era usado para pixel art 2D. Não usar os modelos/folhas antigos como base para o jogo 3D nem consumir créditos enquanto arte 3D estiver pausada. |
| Higgsfield | Conector disponível, fora do fluxo ativo | Não iniciar geração 3D ou consumir créditos até o dono reabrir a etapa de arte e definir o resultado desejado. |
| Git local | Inicializado; sem remoto | Versionar cenas, scripts, prefabs, arte, ProjectSettings e Packages. O .gitignore omite cache e build local. Configure UnityYAMLMerge para cenas/prefabs antes de colaboração em paralelo; avalie Git LFS se os arquivos-fonte de arte crescerem ou o remoto exigir. |
| Python + Pillow | Gerador 2D aposentado em Tools/build_huntz_art.py | Não executar para o jogo atual; gera somente sprites 2D históricos. |

## ECC (@ecc)

ECC é um conjunto global de skills e práticas para agentes, não um pacote de runtime Unity. Skills particularmente úteis neste projeto:

- living-docs-governance: manter STATUS, mapa, handoff e documentos sem fatos duplicados.
- agent-harness-construction e rules-distill: regras curtas, verificáveis e consistentes para agentes.
- codebase-onboarding: orientar agentes novos no mapa e estado do projeto.
- intent-driven-development e plan-orchestrate: dividir uma feature grande em entregas verificáveis.
- architecture-decision-records: registrar uma decisão quando ela mudar arquitetura.
- documentation-lookup e search-first: conferir docs oficiais antes de usar APIs ou pacotes em evolução.
- git-workflow: proteger mudanças locais e manter commits compreensíveis.
- design-system: útil ao reformular HUD, menus e linguagem visual.
- delivery-gate: verificar critérios antes de declarar uma entrega pronta.

O servidor ECC disponível nesta sessão é Chrome DevTools: serve para páginas web e performance de um futuro site/web build, não para a cena Unity. A skill repo-scan disponível é um instalador que solicita revisão de uma ferramenta separada; não é scanner pronto, por isso não foi instalada nem usada.

## Arte, interface e divulgação

| Ferramenta | Classificação |
|---|---|
| PixelLab MCP | Fora do fluxo de produção atual. Se a arte 3D for reaberta, reavaliar a ferramenta para uma tarefa adequada; não portar personagens 2D. |
| PixelLab Pixelorama/Aseprite | Ferramentas da direção pixel-art aposentada; não são dependências nem ferramenta de produção do jogo 3D. |
| Figma (plugin local disponível) | Opcional para fluxo de menus, HUD e folhas de referência editáveis. Não é necessário para sprites ou cenas Unity. |
| Higgsfield | Conector disponível; geração suspensa no ciclo atual de motor graybox. |
| ImageGen | Não usar enquanto a etapa de arte 3D estiver adiada. |
| Node REPL MCP | Pode orquestrar pequenas tarefas JavaScript; não deve virar dependência do gameplay. |
| Browser / Computer Use / Chrome DevTools | Úteis para site, documentação web ou build WebGL; sem papel no ciclo principal do jogo desktop. |
| MagicPath / tldraw (disponíveis no diretório, não instalados no projeto) | Podem mapear fluxo de salas, menus e arquitetura visual em quadros interativos. São opcionais; Figma já cobre os fluxos de interface sem adicionar outra superfície. |

O marketplace local do projeto disponibiliza Figma e Higgsfield. O plugin GitHub está disponível no diretório de plugins, mas não está instalado nem há remoto neste repositório. Ele passa a ser útil após criar um remoto para issues, revisão de PR e CI.

## Frameworks Unity

| Tecnologia | Decisão |
|---|---|
| URP | Manter o Universal Renderer existente; o piloto graybox usa materiais Lit e meshes 3D. |
| `CharacterController`, colliders 3D e `Animator` | Base nativa do movimento e do rig atual. O piloto usa Avatar Generic e bones Transform; malha skinning e Avatar Humanoid final dependem do modelo de personagem. |
| Cinemachine | Já instalado, opcional. A câmera atual é ortográfica, isométrica e acompanhada por script simples; não há requisito ainda para substituir esse fluxo. |
| ProBuilder | Já instalado, opcional para blockout editável. Não é dependência de runtime e o builder atual gera meshes diretamente. |
| 2D Animation 16.0.1 | Continua instalado por causa dos assets e do piloto 2D preservados. Não faz parte do rig 3D ativo. |
| Input System | Manter; controla o piloto 3D. |
| Addressables, ECS/DOTS, Visual Scripting, Timeline, navegação por NavMesh | Sem necessidade demonstrada na fatia. Não adicionar até um requisito de conteúdo/performance/editor provar que resolvem um problema. |

Nesta limpeza saíram do manifesto os pacotes sem uso no protótipo: Unity Assistant/Sentis, AI Navigation, Unity Version Control, Unity Pipeline experimental, Timeline, uGUI, Visual Scripting e integração Rider. O novo Input System, URP, Cinemachine, ProBuilder, 2D Animation legada, integração Visual Studio, Unity MCP e Test Framework requerido pelo MCP permanecem. Nenhum pacote foi adicionado nesta troca para 3D.

## Não prioritários agora

Apps de hospedagem Hostinger, email/calendário, planilhas, documentos, chat corporativo, blockchain e plugins de sites não ajudam a compilar ou jogar o protótipo. Reconsidere apenas se surgir um fluxo específico, como site do jogo, comunidade, telemetria ou distribuição.

## Fontes oficiais

- [MCP for Unity — repositório e instalação](https://github.com/CoplayDev/unity-mcp)
- [PixelLab — guia de uso e ferramentas](https://www.pixellab.ai/docs/ways-to-use-pixellab) e [integração MCP](https://www.pixellab.ai/mcp)
- [PixelLab — catálogo de ferramentas MCP](https://api.pixellab.ai/mcp/docs)
- [ECC — repositório e catálogo de skills](https://github.com/affaan-m/ECC)
- [Unity — Tilemap isométrico no Unity 6.6](https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/tilemaps/isometric-tilemap/create-isometric-tilemap)
- [Unity — Sprite Resolver / Sprite Library](https://docs.unity3d.com/Packages/com.unity.2d.animation@16.0/manual/index.html)
- [Unity — integração de controle de versão e Smart Merge](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/project-configuration/version-control)
