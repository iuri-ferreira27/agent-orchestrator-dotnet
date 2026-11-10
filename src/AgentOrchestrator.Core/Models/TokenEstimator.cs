namespace AgentOrchestrator.Core.Models;

/// <summary>
/// Estimativa simples de tokens a partir do texto.
/// Regra prática: cerca de 4 caracteres por token. Não é exato; serve para simulação.
/// Provedores reais devolvem a contagem exata na resposta.
/// </summary>
public static class TokenEstimator
{
    private const int CharsPerToken = 4;

    public static long Estimate(string? text) =>
        string.IsNullOrEmpty(text) ? 0 : (text.Length + CharsPerToken - 1) / CharsPerToken;
}
