using AgentOrchestrator.Core.Costs;

namespace AgentOrchestrator.Tests.Orchestration;

public class PriceTableTests
{
    private static readonly ModelPrice Barato = new("Barato", 1m, 5m, 0.1m);

    [Fact]
    public void Get_ModeloExistente_DevolveOPreco()
    {
        var table = new PriceTable([Barato]);

        Assert.Same(Barato, table.Get("Barato"));
    }

    [Fact]
    public void Get_NomeComOutraCaixa_Encontra()
    {
        var table = new PriceTable([Barato]);

        Assert.Same(Barato, table.Get("bArAtO"));
        Assert.True(table.TryGet("BARATO", out _));
    }

    [Fact]
    public void Get_ModeloInexistente_LancaErroClaro()
    {
        var table = new PriceTable([Barato]);

        var ex = Assert.Throws<KeyNotFoundException>(() => table.Get("x"));

        Assert.Equal("Preço não configurado para o modelo 'x'", ex.Message);
    }

    [Fact]
    public void TryGet_ModeloInexistente_DevolveFalso()
    {
        var table = new PriceTable([Barato]);

        Assert.False(table.TryGet("x", out _));
    }

    [Fact]
    public void CostOf_UsaOCalculadorDeCusto()
    {
        var table = new PriceTable([Barato]);

        // 1 milhão de entrada a US$ 1 + 1 milhão de saída a US$ 5.
        var cost = table.CostOf("barato", new TokenUsage(1_000_000, 1_000_000));

        Assert.Equal(6m, cost);
    }

    [Fact]
    public void All_ListaTodosOsPrecos()
    {
        var table = new PriceTable([Barato, new ModelPrice("Caro", 10m, 50m, 1m)]);

        Assert.Equal(2, table.All.Count);
    }
}
