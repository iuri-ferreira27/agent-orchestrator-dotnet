using System.Diagnostics;
using System.Text;
using AgentOrchestrator.Core.Costs;
using AgentOrchestrator.Core.Models;

namespace AgentOrchestrator.Core.Orchestration;

/// <summary>
/// Coordena o fluxo: planeja, escolhe o modelo de cada subtarefa, executa em paralelo
/// e soma custos. Também compara o modo roteado com "tudo no modelo pesado".
/// </summary>
public sealed class Orchestrator
{
    private readonly IModelClient _client;
    private readonly IPlanner _planner;
    private readonly IModelRouter _router;
    private readonly PriceTable _prices;
    private readonly OrchestratorOptions _options;

    public Orchestrator(
        IModelClient client,
        IPlanner planner,
        IModelRouter router,
        PriceTable prices,
        OrchestratorOptions options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _prices = prices ?? throw new ArgumentNullException(nameof(prices));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<OrchestrationResult> RunAsync(
        string task,
        RoutingMode mode,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        ValidateTask(task);

        progress?.Report("Planejando...");
        var (plan, plannerCall) = await _planner.PlanAsync(task, ct);

        var execution = await ExecuteAsync(plan, plannerCall, mode, progress, ct);
        return new OrchestrationResult(task, plan, execution, BuildFinalAnswer(execution));
    }

    public async Task<ExecutionResult> ExecuteAsync(
        ExecutionPlan plan,
        CallRecord plannerCall,
        RoutingMode mode,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(plannerCall);

        var stopwatch = Stopwatch.StartNew();

        // O semáforo limita quantas subtarefas rodam ao mesmo tempo; o WhenAll espera todas.
        using var gate = new SemaphoreSlim(_options.MaxParallelism);
        var tasks = plan.SubTasks.Select(subTask => RunSubTaskAsync(plan, subTask, mode, gate, progress, ct));
        var results = (await Task.WhenAll(tasks)).OrderBy(r => r.SubTask.Id).ToList();

        stopwatch.Stop();

        var calls = new List<CallRecord> { plannerCall };
        calls.AddRange(results.Select(r => r.Call));

        var totalUsage = calls.Aggregate(TokenUsage.Zero, (sum, c) => sum + c.Usage);
        var totalCost = calls.Sum(c => c.Cost);

        return new ExecutionResult(mode, results, calls, totalUsage, totalCost, stopwatch.Elapsed);
    }

    public async Task<ComparisonResult> CompareAsync(
        string task,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        ValidateTask(task);

        // Planeja uma única vez: os dois modos executam exatamente o mesmo plano,
        // então a diferença de custo vem só da escolha de modelos.
        progress?.Report("Planejando...");
        var (plan, plannerCall) = await _planner.PlanAsync(task, ct);

        progress?.Report("Executando linha de base (tudo no modelo pesado)...");
        var baseline = await ExecuteAsync(plan, plannerCall, RoutingMode.AllHeavy, progress, ct);

        progress?.Report("Executando com roteamento por dificuldade...");
        var routed = await ExecuteAsync(plan, plannerCall, RoutingMode.Routed, progress, ct);

        var savings = CostCalculator.SavingsPercent(baseline.TotalCost, routed.TotalCost);
        return new ComparisonResult(task, plan, baseline, routed, savings);
    }

    private async Task<SubTaskResult> RunSubTaskAsync(
        ExecutionPlan plan,
        SubTask subTask,
        RoutingMode mode,
        SemaphoreSlim gate,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var model = _router.ModelFor(subTask.Tier, mode);
            var request = new ModelRequest(
                model,
                ExecutorPrompts.Build(plan.Task, subTask),
                ExecutorPrompts.System,
                _options.ExecutorMaxOutputTokens,
                _options.UsePromptCaching);

            var stopwatch = Stopwatch.StartNew();
            var response = await _client.SendAsync(request, ct);
            stopwatch.Stop();

            var call = new CallRecord(
                "Executor",
                subTask.Id,
                response.Model,
                response.Usage,
                _prices.CostOf(response.Model, response.Usage),
                stopwatch.Elapsed);

            progress?.Report($"Subtarefa {subTask.Id} concluída ({response.Model})");
            return new SubTaskResult(subTask, response.Model, response.Text, call);
        }
        finally
        {
            gate.Release();
        }
    }

    private static string BuildFinalAnswer(ExecutionResult execution)
    {
        var sb = new StringBuilder();
        foreach (var r in execution.Results) // já ordenado por Id
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine($"### Subtarefa {r.SubTask.Id}: {r.SubTask.Description}");
            sb.AppendLine(r.Output);
        }

        return sb.ToString().TrimEnd();
    }

    private static void ValidateTask(string task)
    {
        if (string.IsNullOrWhiteSpace(task))
            throw new ArgumentException("A tarefa não pode ser vazia.", nameof(task));
    }
}
