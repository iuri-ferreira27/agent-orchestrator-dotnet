using System.Text.Json;
using AgentOrchestrator.Core.Models;
using AgentOrchestrator.Core.Orchestration;

namespace AgentOrchestrator.Core.Simulation;

/// <summary>
/// Gera respostas falsas, porém coerentes, para o modo simulado: o "planejador" devolve
/// um JSON válido e os "executores" devolvem um texto determinístico.
/// </summary>
public static class SimulatedResponder
{
    private const int MaxParts = 8;
    private const int MinPartLength = 8;

    // Palavras que indicam trabalho difícil (com e sem acento, pois o usuário pode digitar de qualquer forma).
    private static readonly string[] HeavyKeywords =
    [
        "arquitetura", "segurança", "seguranca", "vulnerab", "concorr", "refator",
        "otimiz", "desempenho", "performance", "depur", "bug"
    ];

    public static string Respond(ModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var system = request.SystemPrompt;
        if (system is not null && system.StartsWith(PlannerPrompts.Marker, StringComparison.Ordinal))
            return RespondAsPlanner(request.Prompt);

        if (system is not null && system.StartsWith(ExecutorPrompts.Marker, StringComparison.Ordinal))
            return RespondAsExecutor(request);

        return $"[simulado:{request.Model}] Resposta genérica para o pedido recebido.";
    }

    private static string RespondAsPlanner(string task)
    {
        var parts = task
            .Split(['\n', '\r', '.', ';', '?'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length >= MinPartLength)
            .Take(MaxParts)
            .ToList();

        if (parts.Count == 0)
            parts.Add(task.Trim());

        var subtasks = parts.Select(p => new
        {
            description = p,
            difficulty = Classify(p)
        });

        return JsonSerializer.Serialize(new { subtasks });
    }

    private static string Classify(string text)
    {
        if (HeavyKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return "heavy";

        return text.Length > 90 ? "standard" : "light";
    }

    // Texto igual para todos os modelos: assim o custo muda só pelo preço, e a comparação é justa.
    private static string RespondAsExecutor(ModelRequest request)
    {
        var subtask = request.Prompt;
        var marker = subtask.IndexOf("Subtarefa ", StringComparison.Ordinal);
        if (marker >= 0) subtask = subtask[marker..];

        return "[simulado] Análise concluída para a subtarefa recebida. " +
               $"O trabalho foi feito pelo modelo {request.Model}. " +
               $"Foco: {subtask.Replace('\n', ' ').Trim()}. " +
               "Resultado entregue de forma objetiva.";
    }
}
