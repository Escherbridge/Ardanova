namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application;
using ArdaNova.Application.Mappings;
using AutoMapper;
using AutoMapper.Internal;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

public sealed class MappingSecurityPolicyTests
{
    [Fact]
    public void Apply_BoundsEveryRegisteredTypeMap()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfigurationProvider>();

        var typeMaps = configuration.Internal().GetAllTypeMaps();

        typeMaps.Should().NotBeEmpty();
        typeMaps.Should().OnlyContain(
            typeMap => typeMap.MaxDepth == MappingSecurityPolicy.MaximumGraphDepth);
    }

    [Fact]
    public void Apply_OverridesAnUnboundedMapDeclaredByAnotherProfile()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            cfg.CreateMap<RecursiveRequest, RecursiveResponse>().MaxDepth(int.MaxValue);
            MappingSecurityPolicy.Apply(cfg);
        });

        var requestMap = configuration.Internal()
            .GetAllTypeMaps()
            .Single(typeMap =>
                typeMap.SourceType == typeof(RecursiveRequest) &&
                typeMap.DestinationType == typeof(RecursiveResponse));

        requestMap.MaxDepth.Should().Be(MappingSecurityPolicy.MaximumGraphDepth);
    }

    private sealed class RecursiveRequest
    {
        public RecursiveRequest? Child { get; init; }
    }

    private sealed class RecursiveResponse
    {
        public RecursiveResponse? Child { get; init; }
    }
}
