namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.Feed;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Feed;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>The projector's timer runs by default in Development only; any other environment must say so.</summary>
public class FeedProjectorRegistrationTests
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Development", "", true)]
    [InlineData("Development", "false", false)]
    [InlineData("Production", null, false)]
    [InlineData("Staging", "", false)]
    [InlineData("Production", "true", true)]
    [InlineData(null, null, false)]
    public void TheTimer_RunsOnlyWhereTheEnvironmentOrTheOperatorTurnsItOn(
        string? environment,
        string? enabled,
        bool expectRunning)
    {
        var services = new ServiceCollection();
        services.AddFeedProjector(Configuration(enabled), environment);

        services.Any(descriptor => descriptor.ImplementationType == typeof(FeedProjectorHostedService))
            .Should().Be(expectRunning);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(FeedProjectorCycle),
            "the operator backfill needs the cycle even where the timer is off");
    }

    [Fact]
    public void AMisspelledSwitch_IsAStartupFailure_NotASilentOff()
    {
        var register = () => new ServiceCollection().AddFeedProjector(Configuration("yes"), "Production");

        register.Should().Throw<InvalidOperationException>().WithMessage("*Feed:Projector:Enabled*");
    }

    [Fact]
    public void AddInfrastructure_InDevelopment_WiresAResolvableCycleAndItsTimer()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=ardanova_di_test;Username=postgres;Password=not-used",
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration, "Development");

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        provider.GetServices<IHostedService>().OfType<FeedProjectorHostedService>().Should().ContainSingle();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<FeedProjectorCycle>().Should().NotBeNull();
    }

    private static IConfiguration Configuration(string? enabled)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feed:Projector:Enabled"] = enabled })
            .Build();
}
