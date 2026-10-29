namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Domain.Models.Enums;
using FluentAssertions;

/// <summary>
/// The feed envelope and project-kind vocabulary (SR-03 phase 0) the API can write exists in the
/// database the migration set builds. A C# enum value with no ADD VALUE migration compiles and
/// then raises 22P02 on write; a mapped column with no migration raises 42703. Reads the same
/// on-disk artefacts as <see cref="PrismaSources"/>.
/// </summary>
public class FeedEnvelopePersistenceContractTests
{
    [Fact]
    public void PostAndProjectTypes_AreValuesOfThePostgresEnumsTheMigrationSetBuilds()
    {
        var sources = PrismaSources.Current;

        Enum.GetNames<PostType>().Should().BeSubsetOf(sources.MigratedEnumValuesOf("PostType"));
        Enum.GetNames<ProjectType>().Should().BeSubsetOf(sources.MigratedEnumValuesOf("ProjectType"));
    }

    [Theory]
    [InlineData("Post")]
    [InlineData("TaxonomyTerm")]
    public void FeedTables_MatchThePrismaSchemaInBothDirections(string table)
    {
        var sources = PrismaSources.Current;

        sources.PrismaFieldsOf(table).Should().BeEquivalentTo(sources.MigratedColumnsOf(table));
    }
}
