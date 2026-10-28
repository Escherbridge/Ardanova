namespace ArdaNova.Application.Tests.Flows;

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// P8-15: filtering by category is a whole-token match, not a substring one, so a search for
/// BUSINESS never surfaces a project whose only category is SMALL_BUSINESS. The real
/// <see cref="ProjectService"/> runs over <see cref="InMemoryRepository{T}"/>; the SQL-translation
/// half of this fix is pinned separately in Services/TaxonomySqlTranslationTests.cs.
/// </summary>
public class ProjectCategoryFilterFlowTests
{
    private readonly InMemoryRepository<Project> _projects = new();
    private readonly InMemoryRepository<User> _users = new();
    private readonly ProjectService _service;

    public ProjectCategoryFilterFlowTests()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            MappingSecurityPolicy.Apply(cfg);
        }).CreateMapper();

        _service = new ProjectService(
            ProjectTable(_projects),
            _users,
            new Mock<IProjectDeletionBlockers>().Object,
            new InMemoryUnitOfWork(),
            mapper);
    }

    [Fact]
    public async Task FilteringByCategory_MatchesWholeTokensOnly_NeverASubstring()
    {
        var multiSector = await _service.CreateAsync(NewProjectDto(
            "Community kitchen rebuild", new List<string> { "CONSTRUCTION", "HOSPITALITY" }));
        var smallBusiness = await _service.CreateAsync(NewProjectDto(
            "Corner bakery launch", new List<string> { "SMALL_BUSINESS" }));
        multiSector.IsSuccess.Should().BeTrue(multiSector.Error);
        smallBusiness.IsSuccess.Should().BeTrue(smallBusiness.Error);

        await _service.PublishAsync(multiSector.Value!.Id);
        await _service.PublishAsync(smallBusiness.Value!.Id);

        var businessMatches = await _service.GetByCategory("BUSINESS");
        businessMatches.IsSuccess.Should().BeTrue(businessMatches.Error);
        businessMatches.Value!.Should().BeEmpty(
            "BUSINESS is a substring of SMALL_BUSINESS but must never match it as a whole category token");

        var hospitalityMatches = await _service.GetByCategory("HOSPITALITY");
        hospitalityMatches.IsSuccess.Should().BeTrue(hospitalityMatches.Error);
        hospitalityMatches.Value!.Select(p => p.Id).Should().Equal(multiSector.Value!.Id);
    }

    [Fact]
    public async Task ALegacyCategory_StillFiltersExactly_AfterTheSubstringFixLandedAlongsideTaxonomy()
    {
        var techProject = await _service.CreateAsync(NewProjectDto(
            "Open source ledger tool", new List<string> { "TECHNOLOGY" }));
        techProject.IsSuccess.Should().BeTrue(techProject.Error);
        await _service.PublishAsync(techProject.Value!.Id);

        var matches = await _service.GetByCategory("TECHNOLOGY");

        matches.IsSuccess.Should().BeTrue(matches.Error);
        matches.Value!.Select(p => p.Id).Should().Equal(techProject.Value!.Id);
    }

    private static CreateProjectDto NewProjectDto(string title, List<string> categories) => new()
    {
        CreatedById = "founder-1",
        Title = title,
        Description = "A description long enough for the create flow.",
        ProblemStatement = "A problem worth solving.",
        Solution = "A workable plan.",
        Categories = categories
    };

    /// <summary>The project table as the real repository type the service depends on.</summary>
    private static IProjectRepository ProjectTable(InMemoryRepository<Project> rows)
    {
        var repository = new Mock<IProjectRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string id, CancellationToken ct) => rows.GetByIdAsync(id, ct));
        repository.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns((Project project, CancellationToken ct) => rows.AddAsync(project, ct));
        repository.Setup(r => r.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns((Project project, CancellationToken ct) => rows.UpdateAsync(project, ct));
        repository.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<Project, bool>> predicate, CancellationToken ct) => rows.FindOneAsync(predicate, ct));
        repository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<Project, bool>> predicate, CancellationToken ct) => rows.FindAsync(predicate, ct));
        return repository.Object;
    }
}
