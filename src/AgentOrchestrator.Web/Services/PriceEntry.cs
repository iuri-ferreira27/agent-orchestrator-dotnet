namespace AgentOrchestrator.Web.Services;

/// <summary>
/// Forma simples de um preço no appsettings.json (a configuração precisa de um tipo com
/// propriedades mutáveis para fazer o bind). Depois é convertido para ModelPrice do Core.
/// </summary>
public sealed class PriceEntry
{
    public string Model { get; set; } = "";
    public decimal InputPerMillion { get; set; }
    public decimal OutputPerMillion { get; set; }
    public decimal CacheReadPerMillion { get; set; }
    public decimal CacheWritePerMillion { get; set; }
}
