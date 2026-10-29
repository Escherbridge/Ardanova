namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.Feed;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Feed;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// The projector's timer runs only where an operator turned it on: a Development API can point at a shared
/// database (the dev compose's .env does), and its first cycle sweeps. A typo in its settings never stops the API.
/// </summary>
public class FeedProjectorRegistrationTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    [InlineData(" True ", true)]
    public void TheTimer_RunsOnlyWhenTheOperatorTurnsItOn(string? enabled, bool expectRunning)
    {
        var services = new ServiceCollection();
        services.AddFeedProjector(Configuration(("Feed:Projector:Enabled", enabled)));

        services.Any(descriptor => descriptor.ImplementationType == typeof(FeedProjectorHostedService))
            .Should().Be(expectRunning);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(FeedProjectorCycle),
            "the operator backfill needs the cycle even where the timer is off");
    }

    [Fact]
    public void AddInfrastructure_InDevelopment_WithoutTheSwitch_RunsNoTimer()
    {
        using var provider = Infrastructure("Development", enabled: null);

        provider.GetServices<IHostedService>().OfType<FeedProjectorHostedService>().Should().BeEmpty(
            "Development is what the dev compose runs, against whatever DATABASE_URL its .env names");
    }

    [Fact]
    public void AddInfrastructure_InDevelopment_WithTheSwitch_WiresAResolvableCycleAndItsTimer()
    {
        using var provider = Infrastructure("Development", enabled: "true");

        provider.GetServices<IHostedService>().OfType<FeedProjectorHostedService>().Should().ContainSingle();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<FeedProjectorCycle>().Should().NotBeNull();
    }

    public static TheoryData<string, string, string> MalformedSettings() => new()
    {
        { "Feed:Projector:Enabled", "yes", "Feed:Projector:Enabled" },
        { "Feed:Projector:IntervalSeconds", "five", "Feed:Projector:IntervalSeconds" },
        { "Feed:Projector:SweepBatchSize", "0", "Feed:Projector:SweepBatchSize" },
        { "Feed:Projector:LookBackMinutes", "1", "Feed:Projector:LookBackMinutes" },
    };

    [Theory]
    [MemberData(nameof(MalformedSettings))]
    public async Task AMalformedSetting_LeavesTheTimerOff_AndSaysSoAtCritical_WithoutStoppingTheApi(
        string key,
        string value,
        string namedInTheAlarm)
    {
        var logs = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));

        // The switch is ON: the malformed value still wins, because a half-read configuration is not one to run.
        // LookBackMinutes=1 with IntervalSeconds=60 is the "must cover two cycles" rule.
        var register = () => services.AddFeedProjector(Configuration(
            ("Feed:Projector:Enabled", key == "Feed:Projector:Enabled" ? value : "true"),
            ("Feed:Projector:IntervalSeconds", key == "Feed:Projector:LookBackMinutes" ? "60" : null),
            (key, value)));

        register.Should().NotThrow("payments and sign-in start in the same process");
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var hosted = provider.GetServices<IHostedService>().ToList();
        hosted.OfType<FeedProjectorHostedService>().Should().BeEmpty();
        foreach (var service in hosted)
            await service.StartAsync(CancellationToken.None);

        logs.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Critical)
            .Which.Message.Should().Contain(namedInTheAlarm).And.Contain("OFF");
        provider.GetRequiredService<FeedProjectorOptions>().Enabled.Should().BeFalse();
    }

    private static ServiceProvider Infrastructure(string environment, string? enabled)
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Database=ardanova_di_test;Username=postgres;Password=not-used",
            ["Feed:Projector:Enabled"] = enabled,
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration, environment);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(settings
                .GroupBy(setting => setting.Key)
                .ToDictionary(group => group.Key, group => group.Last().Value))
            .Build();

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(List<(LogLevel Level, string Message)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (entries)
                    entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
