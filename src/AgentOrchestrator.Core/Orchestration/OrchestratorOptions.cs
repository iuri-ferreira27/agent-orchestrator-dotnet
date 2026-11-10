namespace AgentOrchestrator.Core.Orchestration;

/// <summary>
/// Configuração do orquestrador. Usa propriedades com init e construtor vazio
/// para poder ser preenchida automaticamente a partir do appsettings.json.
/// </summary>
public sealed record OrchestratorOptions
{
    public string PlannerModel { get; init; } = "";
    public string LightModel { get; init; } = "";
    public string StandardModel { get; init; } = "";
    public string HeavyModel { get; init; } = "";

    /// <summary>Quantas subtarefas podem rodar ao mesmo tempo.</summary>
    public int MaxParallelism { get; init; } = 4;

    public int PlannerMaxOutputTokens { get; init; } = 1024;
    public int ExecutorMaxOutputTokens { get; init; } = 1024;

    /// <summary>Liga o prompt caching do system prompt (barateia chamadas repetidas).</summary>
    public bool UsePromptCaching { get; init; } = true;

    /// <summary>Falha cedo, com mensagem clara, se a configuração estiver incompleta.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PlannerModel))
            throw new InvalidOperationException("Configuração inválida: PlannerModel não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(LightModel))
            throw new InvalidOperationException("Configuração inválida: LightModel não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(StandardModel))
            throw new InvalidOperationException("Configuração inválida: StandardModel não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(HeavyModel))
            throw new InvalidOperationException("Configuração inválida: HeavyModel não pode ser vazio.");
        if (MaxParallelism < 1)
            throw new InvalidOperationException("Configuração inválida: MaxParallelism deve ser pelo menos 1.");
    }
}
