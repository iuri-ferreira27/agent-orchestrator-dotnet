using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentOrchestrator.Core.Costs;
using AgentOrchestrator.Core.Models;

namespace AgentOrchestrator.Core.Providers;

/// <summary>
/// Cliente da API de mensagens da Anthropic. Implementa IModelClient, então o orquestrador
/// não sabe (nem precisa saber) que está falando com um provedor real.
/// </summary>
public sealed class AnthropicModelClient : IModelClient
{
    private const string Provider = "Anthropic";

    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;

    public AnthropicModelClient(HttpClient http, AnthropicOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);

        // Falha cedo e com mensagem clara. Repare que a mensagem nunca ecoa o valor da chave.
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "A chave da API da Anthropic não está configurada. " +
                "Defina-a com user-secrets: dotnet user-secrets set \"AI:Anthropic:ApiKey\" \"<sua-chave>\"");
        }

        _http = http;
        _options = options;
    }

    public async Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1/messages";
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(BuildBody(request)),
        };
        // A chave vai só em header, nunca na URL nem no corpo.
        message.Headers.Add("x-api-key", _options.ApiKey);
        message.Headers.Add("anthropic-version", _options.ApiVersion);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, cancellationToken);
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

            AnthropicResponse? dto;
            try
            {
                dto = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new ModelClientException(Provider, response.StatusCode, $"Resposta inválida do provedor {Provider}.", ex);
            }

            if (dto is null)
            {
                throw new ModelClientException(Provider, response.StatusCode, $"O provedor {Provider} devolveu uma resposta vazia.");
            }

            return ToModelResponse(dto, request);
        }
    }

    private static AnthropicBody BuildBody(ModelRequest request)
    {
        object? system = null; // null => campo omitido do JSON
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            // Com cache, o system vira lista de blocos para poder marcar cache_control.
            // Sem cache, a forma simples (string) basta.
            system = request.CacheSystemPrompt
                ? new[]
                {
                    new SystemBlock { Text = request.SystemPrompt, CacheControl = new CacheControl() },
                }
                : request.SystemPrompt;
        }

        return new AnthropicBody
        {
            Model = request.Model,
            MaxTokens = request.MaxOutputTokens,
            System = system,
            Messages = [new AnthropicMessage { Role = "user", Content = request.Prompt }],
        };
    }

    private static ModelResponse ToModelResponse(AnthropicResponse dto, ModelRequest request)
    {
        // A resposta pode ter vários blocos (texto, uso de ferramenta...). Só juntamos os de texto.
        var texto = new StringBuilder();
        foreach (var bloco in dto.Content ?? [])
        {
            if (bloco.Type == "text" && bloco.Text is not null)
            {
                texto.Append(bloco.Text);
            }
        }

        // Campos de uso podem faltar ou vir null; nesse caso contam como 0.
        var usage = new TokenUsage(
            dto.Usage?.InputTokens ?? 0,
            dto.Usage?.OutputTokens ?? 0,
            dto.Usage?.CacheReadInputTokens ?? 0,
            dto.Usage?.CacheCreationInputTokens ?? 0);

        return new ModelResponse(dto.Model ?? request.Model, texto.ToString(), usage);
    }

    // DTOs internos: refletem o JSON da API e não vazam para fora do cliente.

    private sealed class AnthropicBody
    {
        [JsonPropertyName("model")] public string Model { get; set; } = "";
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }

        // WhenWritingNull omite o campo quando não há system prompt.
        [JsonPropertyName("system")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? System { get; set; }

        [JsonPropertyName("messages")] public AnthropicMessage[] Messages { get; set; } = [];
    }

    private sealed class AnthropicMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    private sealed class SystemBlock
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "text";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
        [JsonPropertyName("cache_control")] public CacheControl? CacheControl { get; set; }
    }

    private sealed class CacheControl
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "ephemeral";
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("content")] public List<ContentBlock>? Content { get; set; }
        [JsonPropertyName("usage")] public AnthropicUsage? Usage { get; set; }
    }

    private sealed class ContentBlock
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    private sealed class AnthropicUsage
    {
        [JsonPropertyName("input_tokens")] public long? InputTokens { get; set; }
        [JsonPropertyName("output_tokens")] public long? OutputTokens { get; set; }
        [JsonPropertyName("cache_read_input_tokens")] public long? CacheReadInputTokens { get; set; }
        [JsonPropertyName("cache_creation_input_tokens")] public long? CacheCreationInputTokens { get; set; }
    }
}
