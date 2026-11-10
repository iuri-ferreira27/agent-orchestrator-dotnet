using System.Globalization;

namespace AgentOrchestrator.Web.Services;

/// <summary>Formatação padronizada na interface (cultura invariante, para o ponto decimal ser estável).</summary>
public static class Format
{
    public static string Money(decimal value, int decimals = 6) =>
        "US$ " + value.ToString("F" + decimals, CultureInfo.InvariantCulture);

    public static string Percent(decimal value) =>
        value.ToString("F1", CultureInfo.InvariantCulture) + "%";

    public static string Number(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    public static string Millis(TimeSpan value) =>
        ((long)value.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
}
