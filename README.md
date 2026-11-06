# Agent Orchestrator .NET

Orquestrador de múltiplos agentes de IA em .NET: um agente **planejador** divide a tarefa, agentes **executores** mais baratos resolvem cada parte, e um painel em **Blazor** mostra tokens e custo de cada execução.

**Objetivo:** reduzir o custo de IA sem perder qualidade, com números medidos.

## Estrutura

| Projeto | Função |
|---|---|
| `src/AgentOrchestrator.Core` | Regras do orquestrador: planejamento, roteamento de modelos e cálculo de custo |
| `src/AgentOrchestrator.Web` | Interface Blazor (Server) |
| `tests/AgentOrchestrator.Tests` | Testes automatizados (xUnit) |

## Como rodar

Requer o [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet run --project src/AgentOrchestrator.Web
```

## Fluxo de branches

- `main`: produção. Só recebe código via pull request.
- `dev`: integração. As features entram aqui primeiro.
- `feature/*`: uma branch por funcionalidade, com PR para `dev`.
- `hotfix/*`: correção urgente a partir de `main`, com PR para `main`.
