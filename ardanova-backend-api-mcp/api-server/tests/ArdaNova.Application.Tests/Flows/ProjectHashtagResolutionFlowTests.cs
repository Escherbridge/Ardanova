namespace ArdaNova.Application.Tests.Flows;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Npgsql;
using Npgsql.NameTranslation;
using UnitOfWorkOverContext = ArdaNova.Infrastructure.UnitOfWork.UnitOfWork;

/// <summary>
/// SR-03 Phase 3 §3 (light metadata): a project's Hashtags field is a FULL REPLACEMENT of
/// Categories/ProjectType/Tags, resolved against the real TaxonomyTerm table - never a merge, and
/// never the InMemoryRepository ProjectCategoryFilterFlowTests runs over, because the SECTOR/
/// PROJECT_TYPE classification runs a real query through <see cref="TaxonomyService"/>.
/// </summary>
/// <remarks>
/// Needs <c>ARDANOVA_TEST_POSTGRES</c> (a connection string without a database name); without it
/// every test reports SKIPPED via <see cref="PostgresFactAttribute"/>.
/// </remarks>
[Trait("Category", "Database")]
public sealed class ProjectHashtagResolutionFlowTests : IAsyncLifetime
{
    private const string Founder = "user-founder";

    private static readonly IMapper Mapper = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<MappingProfile>();
        MappingSecurityPolicy.Apply(configuration);
    }).CreateMapper();

    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    public async Task InitializeAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_hashtags_" + Guid.NewGuid().ToString("N")[..12],
        };
        var translator = new NpgsqlNullNameTranslator();
        var enumTypes = typeof(ProjectType).Assembly.GetTypes()
            .Where(type => type.IsEnum && type.Namespace == "ArdaNova.Domain.Models.Enums")
            .ToList();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connection.ConnectionString);
        foreach (var enumType in enumTypes)
            dataSourceBuilder.MapEnum(enumType, enumType.Name, translator);
        _dataSource = dataSourceBuilder.Build();
        _options = new DbContextOptionsBuilder<ArdaNovaDbContext>()
            .UseNpgsql(_dataSource, npgsql =>
            {
                foreach (var enumType in enumTypes)
                    npgsql.MapEnum(enumType, enumType.Name, "public", translator);
            })
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
        await SeedAsync(context);
    }

    public async Task DisposeAsync()
    {
        if (_options is null)
            return;

        await using (var context = NewContext())
            await context.Database.EnsureDeletedAsync();
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
    }

    [PostgresFact]
    public async Task CreatingWithSectorAndFreeHashtags_ResolvesTheSectorToCategories_AndKeepsTheRestAsFreeTags()
    {
        var created = Succeeded(await Service().CreateAsync(NewProjectDto(["#construction", "#barn-raising"])));

        created.Categories.Should().Equal("CONSTRUCTION");
        created.Tags.Should().Be("barn-raising");
        created.ProjectType.Should().Be(ProjectType.TEMPORARY, "no PROJECT_TYPE tag was submitted, so it resolves to the enum default");
        created.Hashtags.Should().Equal(
            new[] { "#construction", "#temporary" },
            "the edit form round-trips the derived SECTOR/PROJECT_TYPE fields, not the free tag already carried in Tags");
    }

    [PostgresFact]
    public async Task EditingToADifferentSectorHashtag_ReplacesTheOldCategory_InsteadOfAddingToIt()
    {
        var created = Succeeded(await Service().CreateAsync(NewProjectDto(["#construction"])));
        created.Categories.Should().Equal("CONSTRUCTION");

        var edited = Succeeded(await Service().UpdateAsync(created.Id, new UpdateProjectDto { Hashtags = ["#hospitality"] }));

        edited.Categories.Should().Equal("HOSPITALITY");
        edited.Categories.Should().NotContain("CONSTRUCTION", "a hashtag full replacement clears a category whose tag is gone, never a merge");
    }

    [PostgresFact]
    public async Task TwoProjectTypeHashtags_OnlyTheFirstSubmittedWins()
    {
        var created = Succeeded(await Service().CreateAsync(NewProjectDto(["#physical-build", "#venue"])));

        created.ProjectType.Should().Be(ProjectType.PHYSICAL_BUILD, "the first PROJECT_TYPE tag in submission order wins");
    }

    [PostgresFact]
    public async Task ClearingEveryHashtagOnEdit_ResetsCategoriesAndProjectTypeToTheirDefaults()
    {
        var created = Succeeded(await Service().CreateAsync(NewProjectDto(["#construction", "#physical-build"])));
        created.Categories.Should().Equal("CONSTRUCTION");
        created.ProjectType.Should().Be(ProjectType.PHYSICAL_BUILD);

        var cleared = Succeeded(await Service().UpdateAsync(created.Id, new UpdateProjectDto { Hashtags = [] }));

        cleared.Categories.Should().BeEmpty("removing a tag clears what it derived - a full replacement, not a merge");
        cleared.ProjectType.Should().Be(ProjectType.TEMPORARY, "an absent PROJECT_TYPE tag resets to the enum default");
    }

    [PostgresFact]
    public async Task ASectorResolvedFromAHashtag_IsStillFoundByTheWholeValueCategoryFilter()
    {
        var created = Succeeded(await Service().CreateAsync(NewProjectDto(["#construction"])));
        Succeeded(await Service().PublishAsync(created.Id));

        var matches = Succeeded(await Service().GetByCategory("CONSTRUCTION"));

        matches.Select(project => project.Id).Should().Contain(created.Id,
            "CreateAsync's category comes from NormalizeCategory/HasCategory's own canonical code, whichever path wrote it");
    }

    private static CreateProjectDto NewProjectDto(List<string> hashtags) => new()
    {
        CreatedById = Founder,
        Title = "Barn raising weekend",
        Description = "A description long enough for the create flow.",
        ProblemStatement = "The barn roof leaks.",
        Solution = "Reframe and re-roof it together.",
        Hashtags = hashtags,
    };

    private static T Succeeded<T>(Result<T> result)
    {
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private ProjectService Service()
    {
        var context = NewContext();
        return new ProjectService(
            new ProjectRepository(context),
            new Repository<User>(context),
            Mock.Of<Common.Interfaces.IProjectDeletionBlockers>(),
            new UnitOfWorkOverContext(context),
            Mapper,
            new TaxonomyService(new Repository<TaxonomyTerm>(context)));
    }

    private ArdaNovaDbContext NewContext() => new(_options!);

    /// <summary>Only the terms these tests classify against; production seeds the full vocabulary (see the 20261027003000_taxonomy_term migration).</summary>
    private static async Task SeedAsync(ArdaNovaDbContext context)
    {
        context.Users.Add(new User { id = Founder, email = $"{Founder}@example.test", name = "Founder", createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow });
        context.TaxonomyTerms.Add(Term("taxonomy_sector_construction", TaxonomyKind.SECTOR, "CONSTRUCTION", "Construction"));
        context.TaxonomyTerms.Add(Term("taxonomy_sector_hospitality", TaxonomyKind.SECTOR, "HOSPITALITY", "Hospitality"));
        context.TaxonomyTerms.Add(Term("taxonomy_type_physical_build", TaxonomyKind.PROJECT_TYPE, "PHYSICAL_BUILD", "Physical build"));
        context.TaxonomyTerms.Add(Term("taxonomy_type_venue", TaxonomyKind.PROJECT_TYPE, "VENUE", "Venue"));
        await context.SaveChangesAsync();
    }

    private static TaxonomyTerm Term(string id, TaxonomyKind kind, string slug, string label) => new()
    {
        id = id,
        kind = kind,
        slug = slug,
        label = label,
        isActive = true,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow,
    };
}
