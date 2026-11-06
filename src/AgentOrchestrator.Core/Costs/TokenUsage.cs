namespace AgentOrchestrator.Core.Costs;

/// <summary>
/// Quantidade de tokens consumidos em uma chamada a um modelo de IA.
/// </summary>
/// <param name="InputTokens">Tokens enviados ao modelo (prompt), sem cache.</param>
/// <param name="OutputTokens">Tokens gerados pelo modelo (resposta).</param>
/// <param name="CacheReadTokens">Tokens de entrada lidos do cache de prompt (mais baratos).</param>
public sealed record TokenUsage(long InputTokens, long OutputTokens, long CacheReadTokens = 0)
{
    public static TokenUsage Zero { get; } = new(0, 0, 0);

    /// <summary>Soma o consumo de duas chamadas.</summary>
    public static TokenUsage operator +(TokenUsage a, TokenUsage b) =>
        new(a.InputTokens + b.InputTokens,
            a.OutputTokens + b.OutputTokens,
            a.CacheReadTokens + b.CacheReadTokens);
}
