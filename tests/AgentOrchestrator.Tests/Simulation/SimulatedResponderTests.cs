using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Orchestration;
using AgentOrchestrator.Core.Simulation;

namespace AgentOrchestrator.Tests.Simulation;

public class SimulatedResponderTests
{
    private static ModelRequest PedidoDePlanejador(string tarefa) =>
        new("m", tarefa, PlannerPrompts.System);

    [Fact]
    public void Respond_Planejador_DevolveJsonQueOParserEntende()
    {
        var json = SimulatedResponder.Respond(PedidoDePlanejador("Liste os arquivos. Revise o codigo do login."));

        var plan = PlanParser.Parse(json, "t");

        Assert.False(plan.IsFallback);
        Assert.Equal(2, plan.SubTasks.Count);
    }

    [Theory]
    [InlineData("Revise a arquitetura do sistema", ModelTier.Heavy)]
    [InlineData("Corrija a vulnerabilidade de login", ModelTier.Heavy)]
    [InlineData("Depurar o erro de memoria", ModelTier.Heavy)]
    [InlineData("Listar os arquivos do projeto", ModelTier.Light)]
    public void Respond_Planejador_ClassificaDificuldadePorPalavraChave(string tarefa, ModelTier esperado)
    {
        var plan = PlanParser.Parse(SimulatedResponder.Respond(PedidoDePlanejador(tarefa)), tarefa);

        Assert.Equal(esperado, Assert.Single(plan.SubTasks).Tier);
    }

    [Fact]
    public void Respond_Planejador_FraseLongaSemPalavraChaveVira_Standard()
    {
        var tarefa = "Escreva um texto explicativo sobre como organizar as pastas de um projeto pequeno para iniciantes";
        Assert.True(tarefa.Length > 90);

        var plan = PlanParser.Parse(SimulatedResponder.Respond(PedidoDePlanejador(tarefa)), tarefa);

        Assert.Equal(ModelTier.Standard, Assert.Single(plan.SubTasks).Tier);
    }

    [Fact]
    public void Respond_Planejador_TarefaCurta_GeraUmaParteComATarefaToda()
    {
        var plan = PlanParser.Parse(SimulatedResponder.Respond(PedidoDePlanejador("Oi")), "Oi");

        Assert.Equal("Oi", Assert.Single(plan.SubTasks).Description);
    }

    [Fact]
    public void Respond_Planejador_LimitaEmOitoPartes()
    {
        var tarefa = string.Join(". ", Enumerable.Range(1, 12).Select(i => $"Fazer a parte numero {i}"));

        var plan = PlanParser.Parse(SimulatedResponder.Respond(PedidoDePlanejador(tarefa)), tarefa);

        Assert.Equal(8, plan.SubTasks.Count);
    }

    [Fact]
    public void Respond_Executor_TextoIgualParaQualquerModeloExcetoNome()
    {
        var sub = new SubTask(1, "Listar arquivos", ModelTier.Light);
        var prompt = ExecutorPrompts.Build("tarefa", sub);

        var a = SimulatedResponder.Respond(new ModelRequest("modelo-a", prompt, ExecutorPrompts.System));
        var b = SimulatedResponder.Respond(new ModelRequest("modelo-b", prompt, ExecutorPrompts.System));

        Assert.StartsWith("[simulado]", a);
        Assert.Contains("Listar arquivos", a);
        Assert.Equal(a.Length - "modelo-a".Length, b.Length - "modelo-b".Length);
    }

    [Fact]
    public void Respond_SemMarcador_DevolveTextoGenerico()
    {
        var texto = SimulatedResponder.Respond(new ModelRequest("m", "qualquer"));

        Assert.StartsWith("[simulado:m]", texto);
    }
}
