namespace ArdaNova.Application.Tests.Flows;

using System.Text.RegularExpressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

/// <summary>
/// Deleting a project: every table whose foreign key would make the database refuse is checked first,
/// so the owner gets a 409 naming what is left instead of an opaque foreign-key 400.
/// </summary>
public class ProjectDeletionFlowTests
{
    private const string ProjectId = "project-1";

    private static readonly IReadOnlyList<ProjectDependent> Dependents = ProjectDeletionBlockers.RestrictingDependents(BuildModel());

    public static TheoryData<string> DependentKinds()
    {
        var data = new TheoryData<string>();
        foreach (var dependent in Dependents)
            data.Add(dependent.Kind);
        return data;
    }

    [Fact]
    public void TheCheckedTables_AreExactlyTheRestrictForeignKeysToProjectInTheMigrations()
    {
        var fromModel = Dependents.Select(d => $"{d.Table}.{d.Column}");

        fromModel.Should().BeEquivalentTo(RestrictForeignKeysToProjectInMigrations(),
            "the guard must cover every FK the database enforces, and nothing the database would set to NULL");
    }

    [Fact]
    public void EveryBlockingKind_HasAPlainLanguageLabel()
        => Dependents.Select(d => d.Kind).Should().OnlyContain(kind => ProjectDeletionBlockerLabels.Labels.ContainsKey(kind));

    [Theory]
    [MemberData(nameof(DependentKinds))]
    public async Task AProjectWithAnyDependentRow_IsRefusedWith409NamingIt_AndDeletesOnceItIsGone(string kind)
    {
        var projects = new InMemoryRepository<Project>();
        projects.Rows.Add(new Project { id = ProjectId, title = "p", slug = "p", createdById = "founder", status = ProjectStatus.DRAFT });
        var dependents = new DependentRows();
        dependents.Add(kind, ProjectId);
        var service = new ProjectService(
            ProjectTable(projects), new InMemoryRepository<User>(), dependents, new InMemoryUnitOfWork(), new Mock<IMapper>().Object);

        var refused = await service.DeleteAsync(ProjectId);

        refused.Type.Should().Be(ResultType.Conflict);
        refused.Error.Should().Contain(ProjectDeletionBlockerLabels.Describe(kind));
        (await service.GetDeletionBlockersAsync(ProjectId)).Should().Equal(kind);
        projects.Rows.Should().ContainSingle("a refused delete leaves the project in place");

        dependents.Clear();
        var deleted = await service.DeleteAsync(ProjectId);

        deleted.IsSuccess.Should().BeTrue(deleted.Error);
        projects.Rows.Should().BeEmpty();
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IModel BuildModel()
    {
        // Model building needs the provider, not a connection.
        var options = new DbContextOptionsBuilder<ArdaNovaDbContext>().UseNpgsql("Host=model-only").Options;
        using var context = new ArdaNovaDbContext(options);
        return context.Model;
    }

    private static IEnumerable<string> RestrictForeignKeysToProjectInMigrations()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "ardanova-client", "prisma", "migrations")))
            directory = directory.Parent;
        directory.Should().NotBeNull("the test reads the Prisma migrations as the database's source of truth");

        var constraint = new Regex(
            "ALTER TABLE \"(?<table>\\w+)\" ADD CONSTRAINT \"(?<name>\\w+)\" FOREIGN KEY \\(\"(?<column>\\w+)\"\\) REFERENCES \"Project\"\\(\"id\"\\) ON DELETE (?<action>[A-Z ]+?) ON UPDATE");
        var latest = new Dictionary<string, (string Column, string Action)>();
        foreach (var file in Directory.GetFiles(Path.Combine(directory!.FullName, "ardanova-client", "prisma", "migrations"), "migration.sql", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            foreach (System.Text.RegularExpressions.Match match in constraint.Matches(File.ReadAllText(file)))
                latest[match.Groups["name"].Value] = ($"{match.Groups["table"].Value}.{match.Groups["column"].Value}", match.Groups["action"].Value);

        return latest.Values.Where(fk => fk.Action == "RESTRICT").Select(fk => fk.Column).ToList();
    }

    /// <summary>Rows that reference a project, by entity kind: the in-memory counterpart of the EXISTS probe.</summary>
    private sealed class DependentRows : IProjectDeletionBlockers
    {
        private readonly List<(string Kind, string ProjectId)> _rows = new();

        public void Add(string kind, string projectId) => _rows.Add((kind, projectId));

        public void Clear() => _rows.Clear();

        public Task<IReadOnlyList<string>> FindAsync(string projectId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(_rows.Where(r => r.ProjectId == projectId).Select(r => r.Kind).Distinct().Order(StringComparer.Ordinal).ToList());
    }

    /// <summary>The project table as a list: reads and deletes go to <paramref name="rows"/>.</summary>
    private static IProjectRepository ProjectTable(InMemoryRepository<Project> rows)
    {
        var repository = new Mock<IProjectRepository>(MockBehavior.Strict);
        repository.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string id, CancellationToken ct) => rows.GetByIdAsync(id, ct));
        repository.Setup(r => r.DeleteAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns((Project project, CancellationToken ct) => rows.DeleteAsync(project, ct));
        return repository.Object;
    }
}
