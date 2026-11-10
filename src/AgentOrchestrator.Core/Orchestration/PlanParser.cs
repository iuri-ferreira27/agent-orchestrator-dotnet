using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentOrchestrator.Core.Orchestration;

/// <summary>
/// Converte a resposta de texto do planejador em um <see cref="ExecutionPlan"/>.
/// É tolerante porque modelos às vezes cercam o JSON com texto ou com ```json.
/// </summary>
public static class PlanParser
{
    private const int MaxSubTasks = 8;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static ExecutionPlan Parse(string text, string task)
    {
        var subTasks = TryExtract(text);
        return subTasks is { Count: > 0 }
            ? new ExecutionPlan(task, subTasks, IsFallback: false)
            : Fallback(task);
    }

    // Se o planejador falhar, ainda executamos a tarefa inteira como uma subtarefa.
    private static ExecutionPlan Fallback(string task) =>
        new(task, [new SubTask(1, task, ModelTier.Standard)], IsFallback: true);

    private static List<SubTask>? TryExtract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        PlanDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PlanDto>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto?.Subtasks is null) return null;

        var result = new List<SubTask>();
        foreach (var item in dto.Subtasks)
        {
            var description = item?.Description?.Trim();
            if (string.IsNullOrEmpty(description)) continue;
            if (result.Count == MaxSubTasks) break;

            result.Add(new SubTask(result.Count + 1, description, ParseTier(item!.Difficulty)));
        }

        return result;
    }

    private static ModelTier ParseTier(string? difficulty) =>
        difficulty?.Trim().ToLowerInvariant() switch
        {
            "light" => ModelTier.Light,
            "heavy" => ModelTier.Heavy,
            _ => ModelTier.Standard // desconhecida ou ausente
        };

    private sealed class PlanDto
    {
        [JsonPropertyName("subtasks")]
        public List<SubTaskDto?>? Subtasks { get; set; }
    }

    private sealed class SubTaskDto
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }
    }
}
