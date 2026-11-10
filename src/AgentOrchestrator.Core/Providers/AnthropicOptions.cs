namespace AgentOrchestrator.Core.Providers;

/// <summary>
/// Configuração do provedor Anthropic. Usa propriedades com init e construtor vazio
/// para poder ser preenchida automaticamente pela configuração do ASP.NET.
/// </summary>
public sealed record AnthropicOptions
{
    /// <summary>Chave da API. Nunca versionar: guarde em user-secrets.</summary>
    public string ApiKey { get; init; } = "";

    public string BaseUrl { get; init; } = "https://api.anthropic.com";

    /// <summary>Versão do protocolo exigida no header anthropic-version.</summary>
    public string ApiVersion { get; init; } = "2023-06-01";
}
