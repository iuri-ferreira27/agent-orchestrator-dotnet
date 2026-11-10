using AgentOrchestrator.Core.Costs;

namespace AgentOrchestrator.Core.Orchestration;

/// <summary>Uma parte da tarefa original, com a dificuldade estimada pelo planejador.</summary>
public sealed record SubTask(int Id, string Description, ModelTier Tier);

/// <summary>
/// Plano de execução. <paramref name="IsFallback"/> é true quando o planejador não devolveu
/// um JSON válido e a tarefa inteira virou uma única subtarefa Standard.
/// </summary>
public sealed record ExecutionPlan(string Task, IReadOnlyList<SubTask> SubTasks, bool IsFallback);

/// <summary>Registro de uma chamada a modelo. <paramref name="Role"/> é "Planejador" ou "Executor".</summary>
public sealed record CallRecord(
    string Role,
    int? SubTaskId,
    string Model,
    TokenUsage Usage,
    decimal Cost,
    TimeSpan Duration);

public sealed record SubTaskResult(SubTask SubTask, string Model, string Output, CallRecord Call);

/// <summary>Resultado de executar um plano. Inclui a chamada do planejador em Calls e nos totais.</summary>
public sealed record ExecutionResult(
    RoutingMode Mode,
    IReadOnlyList<SubTaskResult> Results,
    IReadOnlyList<CallRecord> Calls,
    TokenUsage TotalUsage,
    decimal TotalCost,
    TimeSpan Duration);

public sealed record OrchestrationResult(
    string Task,
    ExecutionPlan Plan,
    ExecutionResult Execution,
    string FinalAnswer);

/// <summary>Mesmo plano executado nos dois modos, para comparar o custo.</summary>
public sealed record ComparisonResult(
    string Task,
    ExecutionPlan Plan,
    ExecutionResult Baseline,
    ExecutionResult Routed,
    decimal SavingsPercent);
