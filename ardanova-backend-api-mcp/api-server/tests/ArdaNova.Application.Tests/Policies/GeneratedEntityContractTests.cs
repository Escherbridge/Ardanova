namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Domain.Models.Entities;
using FluentAssertions;

/// <summary>Hand-written entity behaviour that neither the compiler nor the CI regeneration gate sees; see Domain/Models/AGENTS.md "Generated contract".</summary>
public class GeneratedEntityContractTests
{
    [Fact]
    public void ProjectWrittenWithoutUnitOrMedia_IsInTheLedgerUnitWithAnEmptyMediaArray()
    {
        var project = new Project();

        project.fundingGoalUnit.Should().Be("USD");
        project.mediaPaths.Should().Be("[]");
    }

    [Fact]
    public void RetiredTreasuryLedger_HasNoPublicConstructor()
    {
        typeof(TreasuryTransaction).GetConstructors().Should().BeEmpty();
    }
}
