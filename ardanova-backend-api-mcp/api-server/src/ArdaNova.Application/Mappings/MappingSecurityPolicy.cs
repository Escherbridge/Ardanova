namespace ArdaNova.Application.Mappings;

using AutoMapper;
using AutoMapper.Internal;

public static class MappingSecurityPolicy
{
    public const int MaximumGraphDepth = 64;

    public static void Apply(IMapperConfigurationExpression configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Internal().ForAllMaps((_, mapping) => mapping.MaxDepth(MaximumGraphDepth));
    }
}
