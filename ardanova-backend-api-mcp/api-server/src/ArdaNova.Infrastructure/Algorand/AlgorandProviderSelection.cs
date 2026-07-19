namespace ArdaNova.Infrastructure.Algorand;

/// <summary>Allow-listed implementations of the Algorand application port.</summary>
public enum AlgorandProvider
{
    Legacy,
    Azoa,
    Simulated,
}

public static class AlgorandProviderSelection
{
    public static AlgorandProvider Parse(string? configuredValue)
    {
        var value = string.IsNullOrWhiteSpace(configuredValue) ? "Simulated" : configuredValue.Trim();
        return value.ToUpperInvariant() switch
        {
            "LEGACY" => AlgorandProvider.Legacy,
            "AZOA" => AlgorandProvider.Azoa,
            "SIMULATED" => AlgorandProvider.Simulated,
            _ => throw new InvalidOperationException(
                $"Unknown Algorand:Provider '{value}'. Allowed values are Legacy, Azoa, and Simulated."),
        };
    }
}
