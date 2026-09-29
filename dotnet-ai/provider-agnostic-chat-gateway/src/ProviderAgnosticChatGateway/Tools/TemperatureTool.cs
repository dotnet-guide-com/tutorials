namespace ProviderAgnosticChatGateway.Tools;

/// <summary>
/// A harmless deterministic function used to demonstrate tool calling.
/// </summary>
public static class TemperatureTool
{
    public static double ConvertTemperature(
        double value,
        string fromUnit,
        string toUnit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromUnit);
        ArgumentException.ThrowIfNullOrWhiteSpace(toUnit);

        string from = Normalize(fromUnit);
        string to = Normalize(toUnit);

        if (from == to)
        {
            return Math.Round(value, 2);
        }

        double celsius = from switch
        {
            "c" => value,
            "f" => (value - 32d) * 5d / 9d,
            "k" => value - 273.15d,
            _ => throw new ArgumentException(
                $"Unsupported temperature unit '{fromUnit}'.",
                nameof(fromUnit))
        };

        double converted = to switch
        {
            "c" => celsius,
            "f" => (celsius * 9d / 5d) + 32d,
            "k" => celsius + 273.15d,
            _ => throw new ArgumentException(
                $"Unsupported temperature unit '{toUnit}'.",
                nameof(toUnit))
        };

        return Math.Round(converted, 2);
    }

    private static string Normalize(string unit) =>
        unit.Trim().ToLowerInvariant() switch
        {
            "c" or "celsius" => "c",
            "f" or "fahrenheit" => "f",
            "k" or "kelvin" => "k",
            _ => unit.Trim().ToLowerInvariant()
        };
}
