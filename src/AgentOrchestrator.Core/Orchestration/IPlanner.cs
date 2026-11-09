namespace AgentOrchestrator.Core.Orchestration;

public interface IPlanner
{
    Task<(ExecutionPlan Plan, CallRecord Call)> PlanAsync(string task, CancellationToken ct = default);
}
