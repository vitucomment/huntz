# Controle do Unity Editor via MCP

O projeto usa o pacote **MCP for Unity** (CoplayDev, v10.3.0) para que agentes de IA leiam e controlem o Editor:
cenas, GameObjects, componentes, scripts, Console, Play mode, screenshots, menus.

- Pacote (Package Manager → Add from git URL):
  `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
  (a URL **precisa** do `?path=/MCPForUnity` — sem isso a instalação falha)
- Documentação: https://coplaydev.github.io/unity-mcp/

## Ambiente desta máquina

| Item | Valor |
|---|---|
| Unity | Windows, 6000.6.4f1 |
| Agentes | rodam no **WSL2** (Linux) |
| `uv` (exigido pelo servidor) | instalado no Windows: `C:\Users\victo\.local\bin\uv.exe` |
| Rede WSL | `networkingMode=mirrored` em `C:\Users\victo\.wslconfig` (backup em `.wslconfig.bak`) |
| Endpoint | `http://localhost:8080/mcp` (transporte HTTP, escuta só em 127.0.0.1) |

O modo **mirrored** é o que permite ao WSL acessar o `localhost` do Windows. Sem ele (modo NAT padrão), o agente no WSL
não alcança a porta 8080. Mudanças no `.wslconfig` só valem após `wsl --shutdown`.

## Iniciar o servidor (a cada vez que abrir o Unity)

1. Unity → **Window → MCP for Unity**.
2. Transporte **HTTP** → **Start Server** (status verde).
3. Teste do WSL: `curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/mcp` → `406` significa que está no ar
   (406 é esperado para um GET simples; `000` = não está rodando / sem acesso).

O servidor pertence ao projeto aberto: trocar de projeto no Unity derruba a conexão até iniciar o servidor no novo projeto.

## Configurar o cliente

### Claude Code
```bash
claude mcp add --transport http unityMCP http://localhost:8080/mcp            # escopo da pasta atual
claude mcp add --scope user --transport http unityMCP http://localhost:8080/mcp  # todas as pastas
```
Já existe um registro em escopo local para `/home/victor/workspace/workspace/Huntz`.
Reconectar sem reiniciar: comando `/mcp`.

### Codex
**Ainda não configurado.** Adicione ao `~/.codex/config.toml` do ambiente onde o Codex roda
(WSL: `/home/victor/.codex/config.toml`; Windows: `C:\Users\victo\.codex\config.toml`):

```toml
[mcp_servers.unityMCP]
url = "http://localhost:8080/mcp"
```

Versões do Codex que não aceitam `url` (HTTP) podem usar stdio, em que o próprio cliente sobe o servidor Python —
no Windows: `command = "uvx"`, `args = ["--from", "mcpforunityserver", "mcp-for-unity", "--transport", "stdio"]`,
e no Unity selecionar o transporte **stdio**. A janela **Window → MCP for Unity → Configure All Detected Clients**
também pode detectar e configurar o Codex automaticamente.

## Uso pelo agente — receitas que funcionaram

- **Antes de tudo:** ler o recurso `mcpforunity://editor/state` e esperar `data.advice.ready_for_tools == true`
  e `is_compiling == false`.
- **Info do projeto:** recurso `mcpforunity://project/info`. Instâncias abertas: `mcpforunity://instances`.
- **Hierarquia:** `manage_scene` com `action: "get_hierarchy"`.
- **Recompilar após editar .cs pelo disco:** `refresh_unity` (`compile: "request"`, `wait_for_ready: true`), depois
  `read_console` com `types: ["error"]` e `filter_text: "error CS"`.
- **Rodar um builder:** `execute_menu_item` com `menu_path: "Huntz/Build Isometric RPG Concept"`.
- **Play/Stop:** `manage_editor` com `action: "play"` / `"stop"`.
- **Posição de um objeto:** `find_gameobjects` (`search_term: "Player"`) → recurso `mcpforunity://scene/gameobject/{id}`.
- **Screenshot:** `manage_camera` com `action: "screenshot"`, `camera: "Main Camera"`, `include_image: true`
  (não captura `OnGUI`; grava em `Assets/Screenshots/` — apagar depois).
- **Stack trace:** `read_console` com `include_stacktrace: true`, `format: "detailed"`.

### Fallback: chamar o servidor direto por HTTP
Se o cliente MCP do agente estiver desconectado, dá para falar JSON-RPC com o endpoint:

```bash
H='-H Content-Type:application/json -H Accept:application/json,text/event-stream'
curl -s -D h.txt $H http://localhost:8080/mcp -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"cli","version":"1"}}}'
SID=$(grep -i mcp-session-id h.txt | awk '{print $2}' | tr -d '\r')
curl -s $H -H "mcp-session-id: $SID" http://localhost:8080/mcp -d '{"jsonrpc":"2.0","method":"notifications/initialized"}'
curl -s $H -H "mcp-session-id: $SID" http://localhost:8080/mcp \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"read_console","arguments":{"action":"get"}}}'
```
As respostas vêm como SSE (`data: {...}`).

## Limitações

- O MCP não simula entrada de teclado/mouse: gameplay interativo precisa de teste manual ou Play Mode tests.
- Editar `.unity`/`.prefab` como texto é frágil; prefira ferramentas do MCP ou scripts de Editor.
