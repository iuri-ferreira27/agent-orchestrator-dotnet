using AgentOrchestrator.Core.Orchestration;

namespace AgentOrchestrator.Web.Services;

/// <summary>Uma entrada do histórico: uma execução ou uma comparação.</summary>
public sealed record HistoryEntry(
    DateTime Timestamp,
    string Mode,
    string Task,
    int SubTaskCount,
    decimal TotalCost,
    decimal? SavingsPercent,
    bool IsSimulated);

/// <summary>
/// Histórico em memória dos últimos resultados. Singleton compartilhado entre usuários,
/// por isso o acesso é protegido por lock. Some ao reiniciar o app (de propósito: é só uma demonstração).
/// </summary>
public sealed class RunHistory
{
    public const int MaxEntries = 20;

    private readonly object _lock = new();
    private readonly List<HistoryEntry> _entries = new();

    public void Add(OrchestrationResult result, RoutingMode mode, bool isSimulated)
    {
        var label = mode == RoutingMode.Routed ? "Somente roteado" : "Somente tudo no forte";
        Add(new HistoryEntry(DateTime.Now, label, result.Task, result.Plan.SubTasks.Count,
            result.Execution.TotalCost, null, isSimulated));
    }

    public void Add(ComparisonResult result, bool isSimulated) =>
        Add(new HistoryEntry(DateTime.Now, "Comparação", result.Task, result.Plan.SubTasks.Count,
            result.Routed.TotalCost, result.SavingsPercent, isSimulated));

    private void Add(HistoryEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }
    }

    /// <summary>Cópia da lista, da mais recente para a mais antiga.</summary>
    public IReadOnlyList<HistoryEntry> GetAll()
    {
        lock (_lock)
        {
            var copy = new List<HistoryEntry>(_entries);
            copy.Reverse();
            return copy;
        }
    }
}
