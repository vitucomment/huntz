# Huntz

RPG de ação isométrico 3D, com combate em tempo real. A produção de arte está pausada; o piloto usa meshes e materiais graybox da Unity. Os modelos e sprites da antiga direção 2D não fazem parte da identidade visual nem do pipeline atuais.

## Abrir e jogar

- Abra o projeto no Unity Hub com Unity 6000.6.4f1.
- A cena principal e única cena de build é `Assets/Huntz/Prototype/Isometric3D/Scenes/Isometric3DARPGPilot.unity`.
- Entre em Play Mode para jogar o piloto.
- Controles: WASD ou setas para mover; segure Shift para correr; mouse mira; rode a roda do mouse para ajustar o zoom; segure clique esquerdo ou J para ataques repetidos; Espaço esquiva na direção do movimento (ou na direção atual quando parado); E interage; C alterna o visual graybox; R retorna ao início.
- Corrida e esquiva usam vigor, exibido junto da vida na HUD. O piloto atual usa teclado e mouse; suporte a controle ainda não está configurado.

## Cena graybox

O piloto oferece uma MainLand em forma de asteroide, NPCs de apresentação, portal para uma área de caça, inimigos de treino, obstáculos e um personagem 3D com `Animator` e socket de arma. A câmera acompanha o guerreiro e permite zoom suave pela roda do mouse. A vila inclui uma trilha de pedra até o portal, fachadas com telhados e janelas e um marco astral, tudo feito com geometria e materiais da Unity. A validação no Editor não substitui uma sessão manual de jogo e não existe build standalone aprovada.

`Huntz/Prototype 3D/Build Isometric ARPG Pilot` regenera a cena graybox e os assets dentro de `Assets/Huntz/Prototype/Isometric3D`; confira o conteúdo antes de reconstruí-lo. Não use `HuntzSliceBuilder`, que pertence à antiga cena 2D.

## Fonte da verdade

- `docs/STATUS.md`: implementação comprovada e próximos passos.
- `docs/PROJECT_MAP.md`: cena e código ativos.
- `docs/GDD.md`: visão do jogo e propostas ainda não confirmadas.
- `docs/ART_PIPELINE.md`: contrato 3D atual e estado da produção de arte.
- `docs/TOOLS.md`: plugins, MCPs e frameworks avaliados.
- `docs/HANDOFF.md`: contexto curto para a próxima sessão.
- `AGENTS.md`: regras de trabalho para todos os agentes.

O repositório Git local não tem remoto configurado.
