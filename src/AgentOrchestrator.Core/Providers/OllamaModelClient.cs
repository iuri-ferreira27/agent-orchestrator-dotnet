using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentOrchestrator.Core.Costs;
using AgentOrchestrator.Core.Models;

namespace AgentOrchestrator.Core.Providers;

/// <summary>
/// Cliente do Ollama (modelos locais). Útil para testar o orquestrador de graça.
/// Ollama não cobra nem tem prompt caching, por isso CacheSystemPrompt é ignorado.
/// </summary>
public sealed class OllamaModelClient : IModelClient
{
    private const string Provider = "Ollama";

    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaModelClient(HttpClient http, OllamaOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);
        _http = http;
        _options = options;
    }

    public async Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mensagens = new List<OllamaMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            mensagens.Add(new OllamaMessage { Role = "system", Content = request.SystemPrompt });
        }
        mensagens.Add(new OllamaMessage { Role = "user", Content = request.Prompt });

        var body = new OllamaBody
        {
            Model = request.Model,
            Messages = mensagens,
            Stream = false, // uma resposta única, mais simples que tratar streaming
            Options = new OllamaGenOptions { NumPredict = request.MaxOutputTokens },
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/chat";

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(url, body, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ModelClientException(Provider, ex.StatusCode, $"Falha de conexão com o provedor {Provider}: {ex.Message}", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync(cancellationToken);
                throw ModelClientException.FromResponse(Provider, response.StatusCode, erro);
            }

            OllamaResponse? dto;
            try
            {
                dto = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new ModelClientException(Provider, response.StatusCode, $"Resposta inválida do provedor {Provider}.", ex);
            }

            if (dto is null)
            {
                throw new ModelClientException(Provider, response.StatusCode, $"O provedor {Provider} devolveu uma resposta vazia.");
            }

            var usage = new TokenUsage(dto.PromptEvalCount ?? 0, dto.EvalCount ?? 0);
            return new ModelResponse(dto.Model ?? request.Model, dto.Message?.Content ?? "", usage);
        }
    }

    // DTOs internos do formato do Ollama.

    private sealed class OllamaBody
    {
        [JsonPropertyName("model")] public string Model { get; set; } = "";
        [JsonPropertyName("messages")] public List<OllamaMessage> Messages { get; set; } = [];
        [JsonPropertyName("stream")] public bool Stream { get; set; }
        [JsonPropertyName("options")] public OllamaGenOptions Options { get; set; } = new();
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    private sealed class OllamaGenOptions
    {
        [JsonPropertyName("num_predict")] public int NumPredict { get; set; }
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
        [JsonPropertyName("prompt_eval_count")] public long? PromptEvalCount { get; set; }
        [JsonPropertyName("eval_count")] public long? EvalCount { get; set; }
    }
}
