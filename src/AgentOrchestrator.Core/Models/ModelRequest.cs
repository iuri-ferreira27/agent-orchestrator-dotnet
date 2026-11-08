namespace AgentOrchestrator.Core.Models;

/// <summary>
/// Pedido enviado a um modelo de IA.
/// </summary>
/// <param name="Model">Nome do modelo (ex.: o modelo barato dos executores ou o forte do planejador).</param>
/// <param name="Prompt">Texto da tarefa.</param>
/// <param name="SystemPrompt">Instruções fixas de comportamento (opcional). É a parte que mais se beneficia de cache.</param>
/// <param name="MaxOutputTokens">Limite de tokens na resposta. Limitar a saída também controla o custo.</param>
public sealed record ModelRequest(
    string Model,
    string Prompt,
    string? SystemPrompt = null,
    int MaxOutputTokens = 1024);
