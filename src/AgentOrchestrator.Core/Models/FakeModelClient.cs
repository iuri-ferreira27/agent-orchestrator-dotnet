using System.Collections.Concurrent;
using AgentOrchestrator.Core.Costs;

namespace AgentOrchestrator.Core.Models;

/// <summary>
/// Cliente simulado: não chama nenhuma API e não gasta dinheiro.
/// Devolve uma resposta previsível e uma contagem de tokens estimada,
/// para desenvolver e testar o orquestrador inteiro sem chave de API.
/// Também simula o prompt caching: a primeira vez que um system prompt aparece ele é
/// "gravado" no cache; nas vezes seguintes é "lido" (mais barato).
/// </summary>
public sealed class FakeModelClient : IModelClient
{
    private readonly TimeSpan _latency;
    private readonly Func<ModelRequest, string>? _responder;
    private readonly ConcurrentQueue<ModelRequest> _calls = new();

    // Guarda as combinações (modelo, system prompt) já vistas. ConcurrentDictionary porque
    // o orquestrador chama este cliente de várias tarefas em paralelo.
    private readonly ConcurrentDictionary<(string Model, string System), byte> _cachedPrompts = new();

    /// <param name="latency">Atraso simulado por chamada (padrão: nenhum). Útil para ver a execução em paralelo.</param>
    /// <param name="responder">Gera o texto da resposta a partir do pedido. Sem ele, usa um texto genérico.</param>
    public FakeModelClient(TimeSpan? latency = null, Func<ModelRequest, string>? responder = null)
    {
        _latency = latency ?? TimeSpan.Zero;
        _responder = responder;
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

        var systemTokens = TokenEstimator.Estimate(request.SystemPrompt);
        var promptTokens = TokenEstimator.Estimate(request.Prompt);
        var inputTokens = systemTokens + promptTokens;

        string text;
        if (_responder is not null)
        {
            text = _responder(request);
        }
        else
        {
            var preview = request.Prompt.Length <= 60 ? request.Prompt : request.Prompt[..60] + "...";
            text = $"[simulado:{request.Model}] Resposta para: {preview}";
        }

        // Saída: o maior entre o tamanho real do texto e uma estimativa baseada na entrada,
        // sempre respeitando o limite pedido.
        var outputTokens = Math.Min(request.MaxOutputTokens,
            Math.Max(TokenEstimator.Estimate(text), inputTokens / 2 + 20));

        TokenUsage usage;
        if (request.CacheSystemPrompt && !string.IsNullOrEmpty(request.SystemPrompt))
        {
            // TryAdd é atômico: só uma chamada concorrente "ganha" a gravação do cache.
            var firstTime = _cachedPrompts.TryAdd((request.Model, request.SystemPrompt), 0);
            usage = firstTime
                ? new TokenUsage(promptTokens, outputTokens, CacheWriteTokens: systemTokens)
                : new TokenUsage(promptTokens, outputTokens, CacheReadTokens: systemTokens);
        }
        else
        {
            usage = new TokenUsage(inputTokens, outputTokens);
        }

        return new ModelResponse(request.Model, text, usage);
    }
}
