# AGENTS.md — Huntz (Unity)

Instruções para qualquer agente de código (Codex, Claude Code etc.) trabalhando neste projeto.
Leia também [docs/HANDOFF.md](docs/HANDOFF.md) (estado atual e próximos passos) e
[docs/UNITY_MCP.md](docs/UNITY_MCP.md) (como controlar o Editor via MCP).

## O projeto

- **Jogo:** Huntz — **RPG isométrico 2D** (direção definida pelo dono do projeto).
- **Estado:** fase de conceito. Existe um protótipo isométrico jogável feito com formas geradas por código
  (sem arte final). Há também uma demo de plataforma 2D antiga, mantida só como referência.
- **Engine:** Unity **6000.6.4f1** (Unity 6.6), **URP 17.6** com o *Universal Renderer* (template 3D — não é o 2D Renderer).
- **Raiz do projeto:** `C:\Users\victo\Documents\Huntz\My project` (no WSL: `/mnt/c/Users/victo/Documents/Huntz/My project`).
  O nome "My project" é o padrão do Unity Hub; não renomeie a pasta sem combinar com o dono.
- **Sem controle de versão** ainda (não é repositório git). Recomenda-se `git init` com `.gitignore` de Unity antes de mudanças grandes.

## Estrutura

```
Assets/
  IsoRPG/                    <- protótipo do RPG isométrico (foco atual)
    Scripts/                 runtime: IsoPlayer, IsoEnemy, IsoGame, IsoNPC, IsoChest, IsoInteractable, YSort
    Editor/HuntzIsoBuilder.cs  gera a cena inteira por código (menu Huntz > Build Isometric RPG Concept)
    Art/                     sprites procedurais gerados pelo builder (IsoTile, IsoCube, IsoRoof, Circle, Square, Capsule, Blob)
  Scripts/ + Editor/HuntzDemoBuilder.cs + Art/   <- demo de plataforma 2D (referência; menu Huntz > Build 2D Demo Scene)
  Scenes/
    HuntzIsoRPG.unity        cena principal (build index 0)
    Huntz2DDemo.unity        demo de plataforma
    SampleScene.unity        cena padrão do template
  TutorialInfo/              lixo do template, pode ser removido
```

`CameraFollow2D` (em `Assets/Scripts/`) é compartilhado pelas duas demos.

## Como as cenas são construídas

As cenas **não são editadas à mão**: os builders em `Editor/` criam sprites (PNG gerados em código), GameObjects,
componentes e salvam a cena. Para mudar o mapa/layout do protótipo, edite `HuntzIsoBuilder.cs` e rode o menu de novo
(isso **sobrescreve** `HuntzIsoRPG.unity`). Quando o projeto passar a ter arte e level design manual, esse fluxo deve
ser substituído por prefabs + Tilemap e os builders aposentados.

## Convenções de código

- C#, nomes e comentários de código em **inglês**; texto visível ao jogador em **português (pt-BR)**.
- Estilo: classes pequenas por responsabilidade, `[SerializeField] private` para tunáveis, singletons simples
  (`Instance`) para `IsoGame`/`IsoPlayer`, listas estáticas `Active` para contar entidades (`IsoEnemy.Active`).
- Coordenadas isométricas: `world = ((gx - gy) * 0.5, (gx + gy) * 0.25)`; tile = 1 x 0.5 unidade (64x32 px, PPU 64).
- Profundidade: componente `YSort` define `sortingOrder = -(y + yOffset) * 200 + ordemLocal`. Chão usa -32000,
  decalques -31000. Objetos parados usam `isStatic = true` (calcula uma vez).
- Pivô dos sprites de objetos/personagens fica **nos pés** (base), não no centro.

## Armadilhas conhecidas (já custaram tempo)

1. **Input:** o projeto usa **somente o novo Input System** (`activeInputHandler: 1`). Use `UnityEngine.InputSystem`
   (`Keyboard.current`, `Mouse.current`). `Input.GetKey` lança exceção.
2. **Unity 6 API:** `Rigidbody2D.linearVelocity` (não `velocity`).
3. **Domain reload:** campos privados não serializados de arrays podem voltar como **array vazio em vez de null** após
   recompilar/entrar no Play. Marque estado de runtime com `[System.NonSerialized]` e não use `array != null` como flag.
4. **Erros `NoSubscription` (generators.ai.unity.com)** no Console vêm do pacote Unity AI (`com.unity.ai.assistant`),
   que exige assinatura. Ignore — não são do projeto.
5. A ferramenta de screenshot do MCP grava em `Assets/Screenshots/`; apague a pasta depois de usar.
6. Depois de escrever `.cs` pelo sistema de arquivos, peça recompilação (`refresh_unity`) e espere
   `is_compiling == false` antes de chamar menus/ferramentas, e leia o Console para erros `error CS`.

## Verificação

Não há testes automatizados ainda. O ciclo de verificação usado até aqui:
1. Recompilar e checar o Console (sem erros/warnings novos).
2. Rodar o builder, entrar no Play mode via MCP, ler posições de objetos e o Console, tirar screenshot da Main Camera.
3. O MCP **não simula teclado**: combate e diálogo precisam ser testados à mão pelo dono do projeto
   (ou criar Play Mode tests com `InputTestFixture` do Input System).

## Controles do protótipo isométrico

WASD/setas mover · Espaço/J/clique atacar · E interagir (NPC/baú) · Q poção.
