namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// P8-15 and the taxonomy parentSlug projection both depend on EF Core actually turning the C#
/// predicate into SQL, not quietly falling back to client evaluation - something
/// <see cref="ArdaNova.Application.Tests.Support.InMemoryRepository{T}"/>'s LINQ-to-Objects
/// <c>Query()</c> can never catch, because it has no SQL to generate in the first place.
/// "Host=model-only" builds the real Npgsql provider's model without opening a connection (the
/// same technique <c>ProjectDeletionFlowTests.BuildModel</c> and
/// <c>TokenBalanceServiceTrackingTests</c> already use for a real, inspectable model), so
/// <c>ToQueryString()</c> renders genuine SQL text for inspection.
/// </summary>
public class TaxonomySqlTranslationTests
{
    private static ArdaNovaDbContext NewModelOnlyContext() => new(
        new DbContextOptionsBuilder<ArdaNovaDbContext>().UseNpgsql("Host=model-only").Options);

    /// <summary>The comma-padded needle, not a bare `%BUSINESS%` search, is what keeps BUSINESS from matching SMALL_BUSINESS.</summary>
    [Fact]
    public void HasCategory_TranslatesToACommaPaddedComparison_NotARawSubstringSearch()
    {
        using var context = NewModelOnlyContext();

        var sql = context.Projects
            .Where(ProjectQueryFilters.HasCategory("BUSINESS"))
            .ToQueryString();

        sql.Should().Contain(
            "','",
            "the whole-token match concatenates commas onto both the column and the needle before comparing them");
    }

    /// <summary>
    /// EF Core escapes `%` and `_` inside the <c>Contains</c> argument before building the LIKE
    /// pattern, so a category value that happens to contain a LIKE wildcard character is matched
    /// literally instead of acting as a wildcard. Confirmed live: `A%B_C` renders as the escaped
    /// parameter value `'%,A\%B\_C,%'` in the generated SQL.
    /// </summary>
    [Fact]
    public void HasCategory_EscapesLikeWildcardsInTheCategoryValue_SoTheyMatchLiterallyNotAsWildcards()
    {
        using var context = NewModelOnlyContext();

        var sql = context.Projects
            .Where(ProjectQueryFilters.HasCategory("A%B_C"))
            .ToQueryString();

        sql.Should().Contain(
            "%,A\\%B\\_C,%",
            "the % and _ a caller's category value happens to contain must be escaped before EF builds the LIKE pattern, or they would act as wildcards instead of literal characters");
    }

    /// <summary>A SKILL's parentSlug must come from a SQL join on the Parent navigation, never a separate per-row lookup.</summary>
    [Fact]
    public void TaxonomyParentSlugProjection_TranslatesToASqlJoin()
    {
        using var context = NewModelOnlyContext();

        var sql = context.TaxonomyTerms
            .Where(term => term.isActive)
            .Select(term => new { term.slug, ParentSlug = term.Parent != null ? term.Parent.slug : (string?)null })
            .ToQueryString();

        sql.ToUpperInvariant().Should().Contain(
            "JOIN",
            "parentSlug is read off the Parent navigation inside the projection, which EF Core can only answer with a join");
    }
}
