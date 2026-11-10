namespace AgentOrchestrator.Core.Costs;

/// <summary>
/// Preço de um modelo em dólares por 1 milhão de tokens.
/// Os valores reais ficam na configuração (appsettings.json), nunca fixos no código,
/// porque os provedores mudam preços com o tempo.
/// </summary>
public sealed record ModelPrice(
    string Model,
    decimal InputPerMillion,
    decimal OutputPerMillion,
    decimal CacheReadPerMillion,
    decimal CacheWritePerMillion = 0);
