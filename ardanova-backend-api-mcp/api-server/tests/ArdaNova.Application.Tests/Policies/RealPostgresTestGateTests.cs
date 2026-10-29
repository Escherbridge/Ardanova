namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.Tests.Flows;
using ArdaNova.Application.Tests.Support;
using FluentAssertions;

/// <summary>
/// A real-Postgres test either runs or reports SKIPPED; it never early-returns into a green PASSED.
/// Scans only this assembly - <c>ArdaNova.API.Tests</c> carries its own twin of this class for its
/// own real-Postgres flow tests (e.g. <c>SponsorshipVoidFlowTests</c>), since a policy test can only
/// reflect over the assembly it is compiled into.
/// </summary>
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
        var databaseClasses = RealPostgresTestGate.DatabaseCategoryClasses(typeof(RealPostgresTestGateTests).Assembly);
        databaseClasses.Should().Contain(new[] { typeof(FeedProjectorFlowTests), typeof(SettlementNoEffectReopenFlowTests) },
            "the policy is vacuous if the real-Postgres classes stop carrying the Database trait");

        RealPostgresTestGate.UngatedMethods(databaseClasses).Should().BeEmpty(
            "a plain [Fact]/[Theory] here would have to early-return without a server and report PASSED");
    }
}
