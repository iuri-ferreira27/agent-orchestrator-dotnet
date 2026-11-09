using System.Net;

namespace AgentOrchestrator.Core.Providers;

/// <summary>
/// Erro ao falar com um provedor de IA (HTTP com falha ou resposta inválida).
/// Existe para o restante do sistema tratar um único tipo de exceção, sem conhecer detalhes de HTTP.
/// </summary>
public sealed class ModelClientException : Exception
{
    private const int MaxBodyChars = 300;

    public ModelClientException(string provider, HttpStatusCode? statusCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        Provider = provider;
        StatusCode = statusCode;
    }

    /// <summary>Nome do provedor que gerou o erro (ex.: "Anthropic", "Ollama").</summary>
    public string Provider { get; }

    /// <summary>Código HTTP devolvido, quando houve resposta.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Monta a exceção a partir de uma resposta de erro. O corpo é cortado em 300 caracteres
    /// para não poluir logs. A chave de API nunca entra aqui: ela só trafega em header.
    /// </summary>
    internal static ModelClientException FromResponse(string provider, HttpStatusCode statusCode, string? body)
    {
        var trecho = string.IsNullOrWhiteSpace(body)
            ? "(sem corpo)"
            : body.Length <= MaxBodyChars ? body : body[..MaxBodyChars] + "...";

        return new ModelClientException(
            provider,
            statusCode,
            $"O provedor {provider} respondeu com erro HTTP {(int)statusCode} ({statusCode}): {trecho}");
    }
}
