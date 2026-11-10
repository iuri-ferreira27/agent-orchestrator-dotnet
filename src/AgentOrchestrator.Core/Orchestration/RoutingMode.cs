namespace AgentOrchestrator.Core.Orchestration;

/// <summary>Como escolher o modelo de cada subtarefa.</summary>
public enum RoutingMode
{
    /// <summary>Escolhe o modelo pela dificuldade (economiza).</summary>
    Routed,

    /// <summary>Usa o modelo pesado em tudo. É a linha de base da comparação.</summary>
    AllHeavy
}
