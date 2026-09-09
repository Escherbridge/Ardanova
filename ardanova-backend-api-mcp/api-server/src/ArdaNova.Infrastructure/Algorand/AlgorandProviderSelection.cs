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
    /// <summary>What an unset <c>Algorand:Provider</c> resolves to. Rejected in Production by
    /// <c>DependencyInjection.ValidateAlgorandProvider</c>, because it fabricates proofs.</summary>
    public const string UnsetProviderDefault = "Simulated";

    /// <summary>Parses the allow-listed <c>Algorand:Provider</c> value; an unknown value throws.</summary>
    public static AlgorandProvider Parse(string? configuredValue)
    {
        var value = string.IsNullOrWhiteSpace(configuredValue)
            ? UnsetProviderDefault
            : configuredValue.Trim();
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
