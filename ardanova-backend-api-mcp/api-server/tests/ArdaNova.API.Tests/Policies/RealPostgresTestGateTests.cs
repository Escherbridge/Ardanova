namespace ArdaNova.API.Tests.Policies;

using ArdaNova.API.Tests.Flows;
using ArdaNova.Application.Tests.Support;
using FluentAssertions;

/// <summary>
/// The twin of <c>ArdaNova.Application.Tests.Policies.RealPostgresTestGateTests</c> for this
/// assembly: a real-Postgres test here either runs or reports SKIPPED, never an early-returned
/// green PASSED. A policy test can only reflect over the assembly it is compiled into, so every
/// test assembly carrying <c>[Trait("Category", "Database")]</c> classes needs its own copy of
/// this check.
/// </summary>
public class RealPostgresTestGateTests
{
    [Fact]
    public void EveryDatabaseCategoryTest_IsGatedByTheSkippingAttribute()
    {
        var databaseClasses = RealPostgresTestGate.DatabaseCategoryClasses(typeof(RealPostgresTestGateTests).Assembly);
        databaseClasses.Should().Contain(typeof(SponsorshipVoidFlowTests),
            "the policy is vacuous if the real-Postgres class stops carrying the Database trait");

        RealPostgresTestGate.UngatedMethods(databaseClasses).Should().BeEmpty(
            "a plain [Fact]/[Theory] here would have to early-return without a server and report PASSED");
    }
}
