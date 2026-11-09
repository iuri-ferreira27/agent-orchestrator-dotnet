using AgentOrchestrator.Core.Orchestration;

namespace AgentOrchestrator.Tests.Orchestration;

public class PlanParserTests
{
    [Fact]
    public void Parse_JsonPuro_CriaSubtarefasComIdsSequenciais()
    {
        const string json = """{"subtasks":[{"description":"A","difficulty":"light"},{"description":"B","difficulty":"heavy"}]}""";

        var plan = PlanParser.Parse(json, "tarefa");

        Assert.False(plan.IsFallback);
        Assert.Equal([1, 2], plan.SubTasks.Select(s => s.Id));
        Assert.Equal([ModelTier.Light, ModelTier.Heavy], plan.SubTasks.Select(s => s.Tier));
        Assert.Equal("tarefa", plan.Task);
    }

    [Fact]
    public void Parse_JsonCercadoPorTextoECodeFence_Extrai()
    {
        const string texto = """
            Claro! Aqui está o plano:
            ```json
            {"subtasks":[{"description":"Fazer X","difficulty":"standard"}]}
            ```
            Espero que ajude.
            """;

        var plan = PlanParser.Parse(texto, "t");

        Assert.False(plan.IsFallback);
        Assert.Equal("Fazer X", Assert.Single(plan.SubTasks).Description);
    }

    [Fact]
    public void Parse_DificuldadeDesconhecida_ViraStandard()
    {
        const string json = """{"subtasks":[{"description":"A","difficulty":"impossivel"}]}""";

        var plan = PlanParser.Parse(json, "t");

        Assert.Equal(ModelTier.Standard, plan.SubTasks[0].Tier);
    }

    [Fact]
    public void Parse_DescricaoVazia_EhIgnorada()
    {
        const string json = """{"subtasks":[{"description":"  ","difficulty":"light"},{"description":"Ok","difficulty":"light"}]}""";

        var plan = PlanParser.Parse(json, "t");

        var unica = Assert.Single(plan.SubTasks);
        Assert.Equal(1, unica.Id);
        Assert.Equal("Ok", unica.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("isto não é json")]
    [InlineData("{ quebrado ")]
    [InlineData("""{"subtasks":[]}""")]
    [InlineData("""{"outra":1}""")]
    public void Parse_SemPlanoValido_UsaFallback(string texto)
    {
        var plan = PlanParser.Parse(texto, "minha tarefa");

        Assert.True(plan.IsFallback);
        var unica = Assert.Single(plan.SubTasks);
        Assert.Equal("minha tarefa", unica.Description);
        Assert.Equal(ModelTier.Standard, unica.Tier);
        Assert.Equal(1, unica.Id);
    }

    [Fact]
    public void Parse_MaisDeOitoSubtarefas_LimitaEmOito()
    {
        var itens = Enumerable.Range(1, 12).Select(i => $$"""{"description":"Parte {{i}}","difficulty":"light"}""");
        var json = "{\"subtasks\":[" + string.Join(",", itens) + "]}";

        var plan = PlanParser.Parse(json, "t");

        Assert.Equal(8, plan.SubTasks.Count);
        Assert.Equal(8, plan.SubTasks[^1].Id);
    }
}
