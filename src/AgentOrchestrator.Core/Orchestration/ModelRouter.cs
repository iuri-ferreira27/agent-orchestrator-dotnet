namespace AgentOrchestrator.Core.Orchestration;

/// <summary>Escolhe o modelo de uma subtarefa conforme a dificuldade e o modo.</summary>
public sealed class ModelRouter : IModelRouter
{
    private readonly OrchestratorOptions _options;

    public ModelRouter(OrchestratorOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string ModelFor(ModelTier tier, RoutingMode mode)
    {
        if (mode == RoutingMode.AllHeavy) return _options.HeavyModel;

        return tier switch
        {
            ModelTier.Light => _options.LightModel,
            ModelTier.Standard => _options.StandardModel,
            ModelTier.Heavy => _options.HeavyModel,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Dificuldade desconhecida.")
        };
    }
}
