namespace AgentOrchestrator.Core.Costs;

/// <summary>
/// Converte consumo de tokens em custo (US$).
/// É a base para provar a economia: mesmo trabalho, modelos diferentes, custos comparados.
/// </summary>
public static class CostCalculator
{
    private const decimal OneMillion = 1_000_000m;

    public static decimal Calculate(TokenUsage usage, ModelPrice price)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(price);

        return usage.InputTokens * price.InputPerMillion / OneMillion
             + usage.OutputTokens * price.OutputPerMillion / OneMillion
             + usage.CacheReadTokens * price.CacheReadPerMillion / OneMillion
             + usage.CacheWriteTokens * price.CacheWritePerMillion / OneMillion;
    }

    /// <summary>
    /// Percentual economizado de <paramref name="optimized"/> em relação a <paramref name="baseline"/>.
    /// Ex.: baseline 10, optimized 4 → 60.
    /// </summary>
    public static decimal SavingsPercent(decimal baseline, decimal optimized)
    {
        if (baseline <= 0) return 0;
        return Math.Round((baseline - optimized) / baseline * 100m, 1);
    }
}
