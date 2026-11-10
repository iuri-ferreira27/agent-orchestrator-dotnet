using AgentOrchestrator.Core.Costs;
using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Orchestration;
using AgentOrchestrator.Core.Simulation;

namespace AgentOrchestrator.Tests.Orchestration;

/// <summary>Montagem comum dos testes: opções, preços e orquestrador com cliente simulado.</summary>
internal static class OrchestrationTestSupport
{
    public static OrchestratorOptions Options(int maxParallelism = 4) => new()
    {
        PlannerModel = "heavy",
        LightModel = "light",
        StandardModel = "standard",
        HeavyModel = "heavy",
        MaxParallelism = maxParallelism
    };

    // Heavy é bem mais caro que light: assim o roteamento economiza.
    public static PriceTable Prices() => new(
    [
        new ModelPrice("light", 1m, 5m, 0.1m, 1.25m),
        new ModelPrice("standard", 3m, 15m, 0.3m, 3.75m),
        new ModelPrice("heavy", 15m, 75m, 1.5m, 18.75m)
    ]);

    public static (Orchestrator Orchestrator, FakeModelClient Client) Create(
        TimeSpan? latency = null, int maxParallelism = 4)
    {
        var options = Options(maxParallelism);
        var prices = Prices();
        var client = new FakeModelClient(latency, SimulatedResponder.Respond);
        var orchestrator = new Orchestrator(
            client, new Planner(client, prices, options), new ModelRouter(options), prices, options);
        return (orchestrator, client);
    }

    // Quatro frases independentes, uma delas pesada (contém "seguranca").
    public const string FourPartTask =
        "Liste os arquivos do projeto. Revise a seguranca do login. Formate o codigo fonte. Resuma o relatorio final.";
}
