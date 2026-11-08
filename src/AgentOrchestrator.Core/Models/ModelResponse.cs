using AgentOrchestrator.Core.Costs;

namespace AgentOrchestrator.Core.Models;

/// <summary>
/// Resposta de um modelo de IA, com o consumo de tokens usado para calcular o custo.
/// </summary>
public sealed record ModelResponse(string Model, string Text, TokenUsage Usage);
