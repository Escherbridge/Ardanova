namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tasks;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The real composition root resolves the task and post writes, so a missing store or a DI cycle fails here, not on the first request.</summary>
public class TaskWritesRegistrationTests
{
    [Fact]
    public void TheAppComposition_ResolvesTheClaimStoreAndEveryServiceThatNowDependsOnIt()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=ardanova_di_test;Username=postgres;Password=not-used"
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        // Many composition tests build this same DI graph in one test run; see CompositionRootWarnings.
        services.SuppressManyServiceProvidersCreatedWarning();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITaskClaimStore>().Should().BeOfType<TaskClaimStore>();
        scope.ServiceProvider.GetRequiredService<ITaskService>().Should().BeOfType<TaskService>();
        scope.ServiceProvider.GetRequiredService<IPostService>().Should().BeOfType<PostService>();
    }
}
