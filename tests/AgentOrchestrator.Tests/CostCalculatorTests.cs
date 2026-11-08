using AgentOrchestrator.Core.Costs;

namespace AgentOrchestrator.Tests;

public class CostCalculatorTests
{
    // Preço fictício só para teste: facilita conferir as contas de cabeça.
    private static readonly ModelPrice TestPrice = new("modelo-teste", 10m, 20m, 1m);

    [Fact]
    public void Calculate_SomaEntradaSaidaECache()
    {
        var usage = new TokenUsage(InputTokens: 1_000_000, OutputTokens: 500_000, CacheReadTokens: 2_000_000);

        var cost = CostCalculator.Calculate(usage, TestPrice);

        // 1M * 10 + 0,5M * 20 + 2M * 1 = 10 + 10 + 2
        Assert.Equal(22m, cost);
    }

    [Fact]
    public void Calculate_SemTokens_CustoZero()
    {
        Assert.Equal(0m, CostCalculator.Calculate(TokenUsage.Zero, TestPrice));
    }

    [Fact]
    public void TokenUsage_Soma()
    {
        var total = new TokenUsage(10, 20, 30) + new TokenUsage(1, 2, 3);

        Assert.Equal(new TokenUsage(11, 22, 33), total);
    }

    [Theory]
    [InlineData(10, 4, 60)]
    [InlineData(10, 10, 0)]
    [InlineData(0, 5, 0)]
    public void SavingsPercent_CalculaEconomia(decimal baseline, decimal optimized, decimal expected)
    {
        Assert.Equal(expected, CostCalculator.SavingsPercent(baseline, optimized));
    }
}
