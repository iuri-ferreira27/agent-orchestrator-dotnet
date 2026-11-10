namespace AgentOrchestrator.Core.Providers;

/// <summary>
/// Configuração do Ollama, que roda modelos localmente (sem chave de API e sem custo por token).
/// </summary>
public sealed record OllamaOptions
{
    public string BaseUrl { get; init; } = "http://localhost:11434";
}
