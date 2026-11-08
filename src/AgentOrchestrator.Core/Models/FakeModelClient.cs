using System.Collections.Concurrent;
using AgentOrchestrator.Core.Costs;

namespace AgentOrchestrator.Core.Models;

/// <summary>
/// Cliente simulado: não chama nenhuma API e não gasta dinheiro.
/// Devolve uma resposta previsível e uma contagem de tokens estimada,
/// para desenvolver e testar o orquestrador inteiro sem chave de API.
/// </summary>
public sealed class FakeModelClient : IModelClient
{
    private readonly TimeSpan _latency;
    private readonly ConcurrentQueue<ModelRequest> _calls = new();

    /// <param name="latency">Atraso simulado por chamada (padrão: nenhum). Útil para ver a execução em paralelo.</param>
    public FakeModelClient(TimeSpan? latency = null)
    {
        _latency = latency ?? TimeSpan.Zero;
    }

    /// <summary>Todos os pedidos recebidos, na ordem. Usado nos testes.</summary>
    public IReadOnlyCollection<ModelRequest> Calls => _calls;

    public async Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (_latency > TimeSpan.Zero)
            await Task.Delay(_latency, cancellationToken);

        _calls.Enqueue(request);

        var inputTokens = TokenEstimator.Estimate(request.SystemPrompt) + TokenEstimator.Estimate(request.Prompt);

        // Resposta simulada: metade do tamanho da entrada + 20, respeitando o limite de saída.
        var outputTokens = Math.Min(request.MaxOutputTokens, inputTokens / 2 + 20);

        var preview = request.Prompt.Length <= 60 ? request.Prompt : request.Prompt[..60] + "...";
        var text = $"[simulado:{request.Model}] Resposta para: {preview}";

        return new ModelResponse(request.Model, text, new TokenUsage(inputTokens, outputTokens));
    }
}
