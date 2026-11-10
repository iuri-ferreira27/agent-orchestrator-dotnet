namespace AgentOrchestrator.Core.Costs;

/// <summary>
/// Tabela de preços por modelo. Centraliza a busca para o resto do código não
/// precisar saber como os preços foram configurados. Nome do modelo não diferencia maiúsculas.
/// </summary>
public sealed class PriceTable
{
    private readonly Dictionary<string, ModelPrice> _prices;

    public PriceTable(IEnumerable<ModelPrice> prices)
    {
        ArgumentNullException.ThrowIfNull(prices);
        // Se o mesmo modelo aparecer duas vezes, o último vence (útil para sobrescrever configuração).
        _prices = new Dictionary<string, ModelPrice>(StringComparer.OrdinalIgnoreCase);
        foreach (var price in prices)
            _prices[price.Model] = price;
    }

    public IReadOnlyCollection<ModelPrice> All => _prices.Values;

    public ModelPrice Get(string model) =>
        TryGet(model, out var price)
            ? price
            : throw new KeyNotFoundException($"Preço não configurado para o modelo '{model}'");

    public bool TryGet(string model, out ModelPrice price)
    {
        if (model is not null && _prices.TryGetValue(model, out var found))
        {
            price = found;
            return true;
        }

        price = null!;
        return false;
    }

    public decimal CostOf(string model, TokenUsage usage) =>
        CostCalculator.Calculate(usage, Get(model));
}
