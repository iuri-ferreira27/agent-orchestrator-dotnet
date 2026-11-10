namespace AgentOrchestrator.Core.Orchestration;

/// <summary>Instruções fixas do planejador. Ficam num texto único para poderem ser cacheadas.</summary>
public static class PlannerPrompts
{
    /// <summary>Marcador que o simulador usa para reconhecer que o pedido é de planejamento.</summary>
    public const string Marker = "### PLANEJADOR ###";

    public static readonly string System = Marker + "\n" + """
        Você é um planejador de tarefas. Divida a tarefa do usuário em subtarefas independentes
        que possam ser executadas em paralelo.

        Responda SOMENTE com JSON, sem explicações, exatamente neste formato:
        {"subtasks":[{"description":"...","difficulty":"light|standard|heavy"}]}

        Regras:
        - Entre 1 e 8 subtarefas.
        - difficulty "light": trabalho mecânico ou simples (listar, renomear, formatar, resumir).
        - difficulty "standard": exige raciocínio moderado.
        - difficulty "heavy": arquitetura, segurança, depuração difícil ou decisões de alto risco.
        """;
}
