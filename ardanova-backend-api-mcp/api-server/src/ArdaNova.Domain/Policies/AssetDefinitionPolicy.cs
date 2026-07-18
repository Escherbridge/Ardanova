namespace ArdaNova.Domain.Policies;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.ValueObjects;

/// <summary>Validates the immutable identity facts required for a financial asset definition.</summary>
public static class AssetDefinitionPolicy
{
    /// <summary>Returns whether the asset has a canonical chain identity and supported fixed scale.</summary>
    public static bool IsValid(AssetDefinition? definition)
        => definition is not null
           && !string.IsNullOrWhiteSpace(definition.id)
           && !string.IsNullOrWhiteSpace(definition.chainType)
           && !string.IsNullOrWhiteSpace(definition.chainNetwork)
           && !string.IsNullOrWhiteSpace(definition.canonicalAssetId)
           && !string.IsNullOrWhiteSpace(definition.symbol)
           && !string.IsNullOrWhiteSpace(definition.displayName)
           && FixedScaleAmount.IsSupportedScale(definition.scale)
           && !string.Equals(definition.id, definition.supersedesAssetDefinitionId, StringComparison.Ordinal);
}
