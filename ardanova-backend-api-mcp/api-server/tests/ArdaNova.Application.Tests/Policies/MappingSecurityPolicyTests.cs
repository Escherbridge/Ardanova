namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application;
using ArdaNova.Application.Mappings;
using FluentAssertions;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

public sealed class MappingSecurityPolicyTests
{
    [Fact]
    public void Apply_BoundsEveryRegisteredTypeMap()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var configuration = provider.GetRequiredService<TypeAdapterConfig>();

        configuration.RuleMap.Should().NotBeEmpty();
        configuration.RuleMap.Values.Should().OnlyContain(
            rule => rule.Settings.MaxDepth == MappingSecurityPolicy.MaximumGraphDepth);
        configuration.Default.Settings.MaxDepth.Should().Be(MappingSecurityPolicy.MaximumGraphDepth);
    }

    [Fact]
    public void Apply_OverridesAnUnboundedMapDeclaredBesideTheProfile()
    {
        var configuration = MappingProfile.CreateConfig(cfg =>
            cfg.NewConfig<RecursiveRequest, RecursiveResponse>().MaxDepth(int.MaxValue));

        var requestMap = configuration.RuleMap.Values.Single(rule =>
            rule.Settings.SourceType == typeof(RecursiveRequest) &&
            rule.Settings.DestinationType == typeof(RecursiveResponse));

        requestMap.Settings.MaxDepth.Should().Be(MappingSecurityPolicy.MaximumGraphDepth);
    }

    [Fact]
    public void ACyclicGraph_StopsWithinTheDepthLimit()
    {
        var mapper = MappingProfile.CreateMapper(cfg =>
            cfg.NewConfig<RecursiveRequest, RecursiveResponse>());
        var cycle = new RecursiveRequest();
        cycle.Child = cycle;

        var mapped = mapper.Map<RecursiveResponse>(cycle);

        var depth = 0;
        var seen = new HashSet<RecursiveResponse>();
        for (var cursor = mapped; cursor is not null && seen.Add(cursor); cursor = cursor.Child)
            depth++;

        depth.Should().BeInRange(1, MappingSecurityPolicy.MaximumGraphDepth);
    }

    private sealed class RecursiveRequest
    {
        public RecursiveRequest? Child { get; set; }
    }

    private sealed class RecursiveResponse
    {
        public RecursiveResponse? Child { get; init; }
    }
}
