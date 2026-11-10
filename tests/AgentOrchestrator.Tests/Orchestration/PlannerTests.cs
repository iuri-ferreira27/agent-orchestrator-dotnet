using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Orchestration;
using AgentOrchestrator.Core.Simulation;

namespace AgentOrchestrator.Tests.Orchestration;

public class PlannerTests
{
    [Fact]
    public async Task PlanAsync_ComSimulador_DevolvePlanoValidoECustoDoPlanejador()
    {
        var options = OrchestrationTestSupport.Options();
        var client = new FakeModelClient(responder: SimulatedResponder.Respond);
        var planner = new Planner(client, OrchestrationTestSupport.Prices(), options);

        var (plan, call) = await planner.PlanAsync(OrchestrationTestSupport.FourPartTask);

        Assert.False(plan.IsFallback);
        Assert.Equal(4, plan.SubTasks.Count);
        Assert.Equal("Planejador", call.Role);
        Assert.Null(call.SubTaskId);
        Assert.Equal("heavy", call.Model);
        Assert.True(call.Cost > 0);
    }

    [Fact]
    public async Task PlanAsync_EnviaSystemPromptDoPlanejadorComCache()
    {
        var client = new FakeModelClient(responder: SimulatedResponder.Respond);
        var planner = new Planner(client, OrchestrationTestSupport.Prices(), OrchestrationTestSupport.Options());

        await planner.PlanAsync("Fazer algo bem completo");

        var request = Assert.Single(client.Calls);
        Assert.StartsWith("### PLANEJADOR ###", request.SystemPrompt);
        Assert.True(request.CacheSystemPrompt);
    }

    [Fact]
    public async Task PlanAsync_RespostaInvalida_UsaFallback()
    {
        var client = new FakeModelClient(responder: _ => "não sei planejar");
        var planner = new Planner(client, OrchestrationTestSupport.Prices(), OrchestrationTestSupport.Options());

        var (plan, _) = await planner.PlanAsync("Tarefa qualquer");

        Assert.True(plan.IsFallback);
    }

    [Fact]
    public async Task PlanAsync_TarefaVazia_LancaArgumentException()
    {
        var client = new FakeModelClient();
        var planner = new Planner(client, OrchestrationTestSupport.Prices(), OrchestrationTestSupport.Options());

        await Assert.ThrowsAsync<ArgumentException>(() => planner.PlanAsync("  "));
    }
}
