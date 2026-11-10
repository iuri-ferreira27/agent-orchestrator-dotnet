using AgentOrchestrator.Core.Orchestration;

namespace AgentOrchestrator.Tests.Orchestration;

public class ModelRouterTests
{
    private readonly ModelRouter _router = new(OrchestrationTestSupport.Options());

    [Theory]
    [InlineData(ModelTier.Light, "light")]
    [InlineData(ModelTier.Standard, "standard")]
    [InlineData(ModelTier.Heavy, "heavy")]
    public void ModelFor_Routed_EscolhePelaDificuldade(ModelTier tier, string esperado)
    {
        Assert.Equal(esperado, _router.ModelFor(tier, RoutingMode.Routed));
    }

    [Theory]
    [InlineData(ModelTier.Light)]
    [InlineData(ModelTier.Standard)]
    [InlineData(ModelTier.Heavy)]
    public void ModelFor_AllHeavy_SempreUsaOPesado(ModelTier tier)
    {
        Assert.Equal("heavy", _router.ModelFor(tier, RoutingMode.AllHeavy));
    }

    [Fact]
    public void Validate_ModeloVazio_LancaErroClaro()
    {
        var invalida = OrchestrationTestSupport.Options() with { LightModel = "" };

        var ex = Assert.Throws<InvalidOperationException>(invalida.Validate);

        Assert.Contains("LightModel", ex.Message);
    }

    [Fact]
    public void Validate_ParalelismoMenorQueUm_Lanca()
    {
        var invalida = OrchestrationTestSupport.Options(maxParallelism: 0);

        Assert.Throws<InvalidOperationException>(invalida.Validate);
    }
}
