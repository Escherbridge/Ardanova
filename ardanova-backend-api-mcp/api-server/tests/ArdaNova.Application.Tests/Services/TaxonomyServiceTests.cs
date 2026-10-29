namespace ArdaNova.Application.Tests.Services;

using System;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Xunit;

/// <summary>
/// The vocabulary surface GET /api/taxonomy answers from: a suggestion list, never a stored
/// constraint. See Services/Implementations/AGENTS.md §Taxonomy. The SKILL-to-sector join's SQL
/// translation is pinned separately in TaxonomySqlTranslationTests.cs; these run over
/// InMemoryRepository for the parts that need no database.
/// </summary>
public class TaxonomyServiceTests
{
    private static TaxonomyTerm NewTerm(string id, TaxonomyKind kind, string slug, string label, string? parentId = null, bool isActive = true) => new()
    {
        id = id,
        kind = kind,
        slug = slug,
        label = label,
        parentId = parentId,
        sortOrder = 0,
        isActive = isActive,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task ALegacyProjectCategoryCode_StillResolvesToItsHumanLabel()
    {
        var repository = new InMemoryRepository<TaxonomyTerm>();
        repository.Rows.Add(NewTerm("sector-technology", TaxonomyKind.SECTOR, "TECHNOLOGY", "Technology"));
        var service = new TaxonomyService(repository);

        var result = await service.GetActiveTermsAsync("SECTOR");

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Should().ContainSingle(t => t.Slug == "TECHNOLOGY")
            .Which.Label.Should().Be("Technology");
    }

    [Fact]
    public async Task AnInactiveTerm_IsExcludedFromTheSuggestionList()
    {
        var repository = new InMemoryRepository<TaxonomyTerm>();
        repository.Rows.Add(NewTerm("sector-retired", TaxonomyKind.SECTOR, "RETIRED_SECTOR", "Retired", isActive: false));
        var service = new TaxonomyService(repository);

        var result = await service.GetActiveTermsAsync(null);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Should().NotContain(t => t.Slug == "RETIRED_SECTOR");
    }

    [Fact]
    public async Task AnUnknownKind_IsRefusedWithAValidationError()
    {
        var service = new TaxonomyService(new InMemoryRepository<TaxonomyTerm>());

        var result = await service.GetActiveTermsAsync("BOGUS");

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
    }
}
