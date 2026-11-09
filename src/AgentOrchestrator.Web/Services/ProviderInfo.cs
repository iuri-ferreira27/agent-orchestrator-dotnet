namespace AgentOrchestrator.Web.Services;

/// <summary>Provedor de IA ativo, para a interface mostrar de onde vêm as respostas.</summary>
public sealed record ProviderInfo(string Name, bool IsSimulated);

/// <summary>Aviso sobre os preços configurados (mostrado na interface).</summary>
public sealed record PriceNote(string Text);
