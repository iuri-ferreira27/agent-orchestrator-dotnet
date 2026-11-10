namespace AgentOrchestrator.Core.Orchestration;

/// <summary>Instruções e montagem do prompt dos executores.</summary>
public static class ExecutorPrompts
{
    /// <summary>Marcador que o simulador usa para reconhecer que o pedido é de execução.</summary>
    public const string Marker = "### EXECUTOR ###";

    public static readonly string System = Marker + "\n" + """
        Você executa uma subtarefa específica de uma tarefa maior.
        Faça somente a subtarefa pedida, de forma objetiva e completa.
        """;

    /// <summary>Monta o prompt variável (a parte que muda a cada chamada, fora do cache).</summary>
    public static string Build(string task, SubTask subTask)
    {
        ArgumentNullException.ThrowIfNull(subTask);
        return $"Tarefa geral: {task}\nSubtarefa {subTask.Id}: {subTask.Description}";
    }
}
