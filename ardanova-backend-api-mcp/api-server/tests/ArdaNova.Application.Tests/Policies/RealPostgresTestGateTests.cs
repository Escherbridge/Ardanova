namespace ArdaNova.Application.Tests.Policies;

using System.Reflection;
using ArdaNova.Application.Tests.Flows;
using ArdaNova.Application.Tests.Support;
using FluentAssertions;

/// <summary>A real-Postgres test either runs or reports SKIPPED; it never early-returns into a green PASSED.</summary>
public class RealPostgresTestGateTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("Host=localhost;Port=5432;Username=postgres;Password=postgres", false)]
    public void TheGate_SkipsOnlyWhenNoServerIsConfigured(string? connection, bool skipped)
        => (PostgresTestServer.SkipReasonFor(connection) is not null).Should().Be(skipped);

    [Fact]
    public void EveryDatabaseCategoryTest_IsGatedByTheSkippingAttribute()
    {
        var databaseClasses = typeof(RealPostgresTestGateTests).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributesData().Any(attribute =>
                attribute.AttributeType == typeof(TraitAttribute)
                && attribute.ConstructorArguments is [{ Value: "Category" }, { Value: "Database" }]))
            .ToList();
        databaseClasses.Should().Contain(new[] { typeof(FeedProjectorFlowTests), typeof(SettlementNoEffectReopenFlowTests) },
            "the policy is vacuous if the real-Postgres classes stop carrying the Database trait");

        var ungated = databaseClasses
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is { } fact
                && fact is not PostgresFactAttribute and not PostgresTheoryAttribute)
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        ungated.Should().BeEmpty(
            "a plain [Fact]/[Theory] here would have to early-return without a server and report PASSED");
    }
}
