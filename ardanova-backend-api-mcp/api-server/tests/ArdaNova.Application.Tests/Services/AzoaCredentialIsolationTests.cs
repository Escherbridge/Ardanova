namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class AzoaCredentialIsolationTests
{
    [Fact]
    public void Production_DoesNotFallBackToLegacyTenantKey()
    {
        var settings = new AzoaSettings { TenantApiKey = "legacy-development-key" };

        AzoaCredentialSelection.ResolveCustodyKey(settings, isProduction: true)
            .Should().BeEmpty();
        AzoaCredentialSelection.ResolveValueKey(settings, isProduction: true)
            .Should().BeEmpty();
        AzoaCredentialSelection.ResolveQuestKey(settings, isProduction: true)
            .Should().BeEmpty();
    }

    [Fact]
    public void Development_CanUseLegacyKeyDuringCredentialMigration()
    {
        var settings = new AzoaSettings { TenantApiKey = "legacy-development-key" };

        AzoaCredentialSelection.ResolveCustodyKey(settings, isProduction: false)
            .Should().Be("legacy-development-key");
        AzoaCredentialSelection.ResolveValueKey(settings, isProduction: false)
            .Should().Be("legacy-development-key");
        AzoaCredentialSelection.ResolveQuestKey(settings, isProduction: false)
            .Should().Be("legacy-development-key");
    }

    [Fact]
    public void ExplicitCredentials_WinOverDevelopmentLegacyFallback()
    {
        var settings = new AzoaSettings
        {
            TenantApiKey = "legacy-development-key",
            CustodyApiKey = "custody-only-key",
            ValueApiKey = "value-only-key",
            QuestApiKey = "quest-only-key",
        };

        AzoaCredentialSelection.ResolveCustodyKey(settings, isProduction: false)
            .Should().Be("custody-only-key");
        AzoaCredentialSelection.ResolveValueKey(settings, isProduction: false)
            .Should().Be("value-only-key");
        AzoaCredentialSelection.ResolveQuestKey(settings, isProduction: false)
            .Should().Be("quest-only-key");
    }

    [Fact]
    public void Production_RejectsOneCredentialSharedAcrossBoundaries()
    {
        AzoaCredentialSelection.AreCredentialsSeparated(
                "same-scoped-key",
                "same-scoped-key",
                "quest-key",
                isProduction: true)
            .Should().BeFalse();

        AzoaCredentialSelection.AreCredentialsSeparated(
                "same-development-key",
                "same-development-key",
                "same-development-key",
                isProduction: false)
            .Should().BeTrue();

        AzoaCredentialSelection.AreCredentialsSeparated(
                "custody-key",
                "value-key",
                "value-key",
                isProduction: true)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("custody", "tenant-provision-wallet-manage-kyc-only-key")]
    [InlineData("value", "nft-mint-only-key")]
    [InlineData("quest", "dapp-develop-only-key")]
    public void ProductionStartup_RejectsPublishedCredentialExamples(
        string credentialBoundary,
        string publishedExample)
    {
        var values = CreateProductionConfiguration();
        var credentialName = credentialBoundary switch
        {
            "custody" => "CustodyApiKey",
            "value" => "ValueApiKey",
            "quest" => "QuestApiKey",
            _ => throw new ArgumentOutOfRangeException(nameof(credentialBoundary)),
        };
        values[$"Azoa:{credentialName}"] = publishedExample;
        if (credentialBoundary == "custody")
            values["Azoa:EnableCustodialAccounts"] = "true";
        if (credentialBoundary == "value")
            values["Azoa:EnableFundingCheckout"] = "true";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var act = () => new ServiceCollection()
            .AddInfrastructure(configuration, "Production");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProductionStartup_AcceptsDistinctGeneratedCredentials()
    {
        var values = CreateProductionConfiguration();
        values["Azoa:EnableCustodialAccounts"] = "true";
        values["Azoa:EnableFundingCheckout"] = "true";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var act = () => new ServiceCollection()
            .AddInfrastructure(configuration, "Production");

        act.Should().NotThrow();
    }

    private static Dictionary<string, string?> CreateProductionConfiguration()
        => new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=ardanova_test;Username=postgres;Password=test",
            ["Algorand:Provider"] = "Simulated",
            ["Azoa:Mode"] = "Simulated",
            ["Azoa:BaseUrl"] = "https://azoa.example",
            ["Azoa:TenantId"] = "34ce2522-400a-48f0-b4a4-9ff5b0730001",
            ["Azoa:TimeoutSeconds"] = "30",
            ["Azoa:CustodyApiKey"] = "custody-random-0123456789abcdef0123456789",
            ["Azoa:ValueApiKey"] = "value-random-abcdef0123456789abcdef012345",
            ["Azoa:QuestApiKey"] = "quest-random-9876543210fedcba9876543210fed",
        };
}
