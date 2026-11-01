namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ActorAssertionReplayCleanupRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersScopeSafeReplayCleanupDependencies()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=ardanova_di_test;Username=postgres;Password=not-used"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        // Many composition tests build this same DI graph in one test run; see CompositionRootWarnings.
        services.SuppressManyServiceProvidersCreatedWarning();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        provider.GetServices<IHostedService>()
            .OfType<ActorAssertionReplayCleanupService>()
            .Should().ContainSingle();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ActorAssertionReplayCleanupCycle>()
            .Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IActorAssertionReplayCleanupStore>()
            .Should().BeOfType<ActorAssertionReplayCleanupStore>();
    }
}
