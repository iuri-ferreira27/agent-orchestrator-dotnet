using System.Diagnostics;
using AgentOrchestrator.Core.Orchestration;
using static AgentOrchestrator.Tests.Orchestration.OrchestrationTestSupport;

namespace AgentOrchestrator.Tests.Orchestration;

public class OrchestratorTests
{
    [Fact]
    public async Task RunAsync_ProduzPlanoEResultadosOrdenadosPorId()
    {
        var (orchestrator, _) = Create();

        var result = await orchestrator.RunAsync(FourPartTask, RoutingMode.Routed);

        Assert.Equal(4, result.Plan.SubTasks.Count);
        Assert.Equal([1, 2, 3, 4], result.Execution.Results.Select(r => r.SubTask.Id));
        // 1 chamada do planejador + 4 executores.
        Assert.Equal(5, result.Execution.Calls.Count);
        Assert.Equal("Planejador", result.Execution.Calls[0].Role);
        Assert.Equal(result.Execution.Calls.Sum(c => c.Cost), result.Execution.TotalCost);
    }

    [Fact]
    public async Task RunAsync_RespostaFinalTemCabecalhosEmOrdem()
    {
        var (orchestrator, _) = Create();

        var result = await orchestrator.RunAsync(FourPartTask, RoutingMode.Routed);

        var posicoes = Enumerable.Range(1, 4)
            .Select(id => result.FinalAnswer.IndexOf($"### Subtarefa {id}: ", StringComparison.Ordinal))
            .ToList();
        Assert.All(posicoes, p => Assert.True(p >= 0));
        Assert.Equal(posicoes.Order(), posicoes);
    }

    [Fact]
    public async Task RunAsync_ReportaProgresso()
    {
        var (orchestrator, _) = Create();
        var mensagens = new List<string>();

        await orchestrator.RunAsync(FourPartTask, RoutingMode.Routed, new SyncProgress(mensagens.Add));

        Assert.Contains("Planejando...", mensagens);
        Assert.Contains(mensagens, m => m.StartsWith("Subtarefa 2 concluída"));
    }

    [Fact]
    public async Task RunAsync_TarefaVazia_LancaArgumentException()
    {
        var (orchestrator, _) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.RunAsync(" ", RoutingMode.Routed));
    }

    [Fact]
    public async Task ExecuteAsync_EmParalelo_TerminaMuitoAntesDoTempoSequencial()
    {
        var (orchestrator, _) = Create(TimeSpan.FromMilliseconds(200), maxParallelism: 4);

        var stopwatch = Stopwatch.StartNew();
        await orchestrator.RunAsync(FourPartTask, RoutingMode.Routed);
        stopwatch.Stop();

        // Planejador (200 ms) + 4 subtarefas em paralelo (~200 ms) = ~400 ms.
        // Em série seriam ~1000 ms.
        Assert.True(stopwatch.ElapsedMilliseconds < 600, $"Demorou {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task ExecuteAsync_ParalelismoUm_ExecutaEmSerieEFicaMaisLento()
    {
        var (orchestrator, _) = Create(TimeSpan.FromMilliseconds(200), maxParallelism: 1);

        var stopwatch = Stopwatch.StartNew();
        await orchestrator.RunAsync(FourPartTask, RoutingMode.Routed);
        stopwatch.Stop();

        // Planejador + 4 subtarefas em série = ~1000 ms (margem para timers imprecisos).
        Assert.True(stopwatch.ElapsedMilliseconds >= 800, $"Demorou só {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task CompareAsync_HeavyMaisCaro_EconomiaPositivaEBaselineSoUsaHeavy()
    {
        var (orchestrator, _) = Create();

        var comparison = await orchestrator.CompareAsync(FourPartTask);

        Assert.True(comparison.SavingsPercent > 0);
        Assert.True(comparison.Baseline.TotalCost > comparison.Routed.TotalCost);
        Assert.Equal(RoutingMode.AllHeavy, comparison.Baseline.Mode);
        Assert.Equal(RoutingMode.Routed, comparison.Routed.Mode);
        Assert.All(comparison.Baseline.Results, r => Assert.Equal("heavy", r.Model));
        // O roteado usa pelo menos um modelo mais barato.
        Assert.Contains(comparison.Routed.Results, r => r.Model != "heavy");
    }

    [Fact]
    public async Task CompareAsync_PlanejaUmaVezSo()
    {
        var (orchestrator, client) = Create();

        await orchestrator.CompareAsync(FourPartTask);

        var chamadasDoPlanejador = client.Calls.Count(c => c.SystemPrompt!.StartsWith("### PLANEJADOR ###"));
        Assert.Equal(1, chamadasDoPlanejador);
    }

    [Fact]
    public async Task CompareAsync_TarefaVazia_LancaArgumentException()
    {
        var (orchestrator, _) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.CompareAsync(""));
    }

    // Progress<T> posta no contexto de sincronização (assíncrono); este é síncrono e previsível nos testes.
    // Os testes de progresso rodam sequencialmente com Task.WhenAll; usamos lock na lista.
    private sealed class SyncProgress(Action<string> onReport) : IProgress<string>
    {
        private readonly object _lock = new();

        public void Report(string value)
        {
            lock (_lock) onReport(value);
        }
    }
}
