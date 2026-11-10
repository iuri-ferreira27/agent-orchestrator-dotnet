using System.Diagnostics;
using AgentOrchestrator.Core.Costs;
using AgentOrchestrator.Core.Models;

namespace AgentOrchestrator.Core.Orchestration;

/// <summary>Usa um modelo para quebrar a tarefa em subtarefas com dificuldade.</summary>
public sealed class Planner : IPlanner
{
    private readonly IModelClient _client;
    private readonly PriceTable _prices;
    private readonly OrchestratorOptions _options;

    public Planner(IModelClient client, PriceTable prices, OrchestratorOptions options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _prices = prices ?? throw new ArgumentNullException(nameof(prices));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<(ExecutionPlan Plan, CallRecord Call)> PlanAsync(string task, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(task))
            throw new ArgumentException("A tarefa não pode ser vazia.", nameof(task));

        var request = new ModelRequest(
            _options.PlannerModel,
            task,
            PlannerPrompts.System,
            _options.PlannerMaxOutputTokens,
            _options.UsePromptCaching);

        var stopwatch = Stopwatch.StartNew();
        var response = await _client.SendAsync(request, ct);
        stopwatch.Stop();

        var call = new CallRecord(
            "Planejador",
            null,
            response.Model,
            response.Usage,
            _prices.CostOf(response.Model, response.Usage),
            stopwatch.Elapsed);

        return (PlanParser.Parse(response.Text, task), call);
    }
}
