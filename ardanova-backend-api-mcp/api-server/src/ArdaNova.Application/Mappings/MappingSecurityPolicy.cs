namespace ArdaNova.Application.Mappings;

using Mapster;

/// <summary>
/// Bounds every map so a cyclic entity graph cannot recurse without limit.
/// Mapster does not apply a depth limit unless one is set, and an explicit
/// per-map depth would otherwise override the default, so this runs last.
/// </summary>
public static class MappingSecurityPolicy
{
    public const int MaximumGraphDepth = 64;

    public static void Apply(TypeAdapterConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Default.MaxDepth(MaximumGraphDepth);
        foreach (var rule in configuration.RuleMap.Values)
            rule.Settings.MaxDepth = MaximumGraphDepth;
    }
}
