@AGENTS.md

## Específico do Claude Code

- O servidor MCP do Unity está registrado como `unityMCP` (HTTP, `http://localhost:8080/mcp`) em escopo **local** da
  pasta `/home/victor/workspace/workspace/Huntz` (WSL). Se abrir o Claude Code a partir desta pasta do projeto, registre
  de novo ou use `--scope user` — ver [docs/UNITY_MCP.md](docs/UNITY_MCP.md).
- Se o `unityMCP` aparecer como falho (ECONNREFUSED), o servidor no Unity provavelmente não estava rodando quando a
  sessão começou: inicie-o no Unity e use `/mcp` para reconectar.
- Ao terminar uma sessão de trabalho, atualize [docs/HANDOFF.md](docs/HANDOFF.md) para o próximo agente.
