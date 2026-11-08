namespace AgentOrchestrator.Core.Models;

/// <summary>
/// Contrato para conversar com qualquer provedor de IA.
/// O orquestrador só conhece esta interface, nunca o provedor concreto.
/// Assim dá para trocar o cliente simulado por um real mudando apenas o registro de dependências.
/// </summary>
public interface IModelClient
{
    Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken = default);
}
