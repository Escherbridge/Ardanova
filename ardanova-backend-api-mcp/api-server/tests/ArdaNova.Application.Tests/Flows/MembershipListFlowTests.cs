namespace ArdaNova.Application.Tests.Flows;

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
using Npgsql;
using Npgsql.NameTranslation;
using UnitOfWorkOverContext = ArdaNova.Infrastructure.UnitOfWork.UnitOfWork;

/// <summary>
/// SR-03 Phase 3 final join: the composer's attach picker lists every project and guild the writer belongs to, not
/// only the ones they created (GET /api/projects/mine, GET /api/guilds/mine). Real Postgres, because the membership
/// test is a navigation `Any` that has to translate to SQL. Without <c>ARDANOVA_TEST_POSTGRES</c> every test SKIPS.
/// </summary>
[Trait("Category", "Database")]
public sealed class MembershipListFlowTests : IAsyncLifetime
{
    private const string Ada = "user-ada";
    private const string Cy = "user-cy";
    private const string Dee = "user-dee";

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
            Database = "ardanova_membership_" + Guid.NewGuid().ToString("N")[..12],
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

    /// <summary>Ada created the orchard and joined Cy's barn and Cy's draft shed; Cy's kitchen is not hers.</summary>
    [PostgresFact]
    public async Task AMembersProjects_AreTheOnesSheCreatedOrJoined_DraftsOfHerOwnTeamIncluded_ByTitle()
    {
        var mine = await Projects().GetMembershipsAsync(new ProjectViewer(Ada, IsAdmin: false));

        mine.IsSuccess.Should().BeTrue(mine.Error);
        mine.Value!.Select(project => project.Title).Should().Equal("Barn raising", "Orchard", "Shed prototype");
    }

    /// <summary>The route has no userId: each signed actor reads only their own team, and the anonymous public none.</summary>
    [PostgresFact]
    public async Task EachViewerReadsOnlyTheirOwnTeam_AndAnonymousReadsNothing()
    {
        var cys = await Projects().GetMembershipsAsync(new ProjectViewer(Cy, IsAdmin: false));
        var dees = await Projects().GetMembershipsAsync(new ProjectViewer(Dee, IsAdmin: false));
        var anonymous = await Projects().GetMembershipsAsync(ProjectViewer.Anonymous);

        cys.Value!.Select(project => project.Title).Should().Equal("Barn raising", "Shed prototype", "Village kitchen");
        dees.Value!.Should().BeEmpty("Dee is on no project team");
        anonymous.Value!.Should().BeEmpty();
    }

    /// <summary>An admin's read is still their own team: the admin bypass widens what is visible, not whose list it is.</summary>
    [PostgresFact]
    public async Task AnAdmin_StillReadsOnlyTheirOwnTeam()
    {
        var admin = await Projects().GetMembershipsAsync(new ProjectViewer(Dee, IsAdmin: true));

        admin.Value!.Should().BeEmpty();
    }

    [PostgresFact]
    public async Task AMembersGuilds_AreTheOnesSheOwnsOrJoined_ByName()
    {
        var mine = await Guilds().GetMembershipsAsync(Ada);

        mine.IsSuccess.Should().BeTrue(mine.Error);
        mine.Value!.Select(guild => guild.Name).Should().Equal("Orchardists", "Timber framers");
    }

    /// <summary>The picker's read is bounded: the most recently active <see cref="MembershipLists.Cap"/> projects, still by title.</summary>
    [PostgresFact]
    public async Task AProlificMembersProjectList_IsCappedAtTheMostRecentlyActive_AndStillByTitle()
    {
        const string prolific = "user-prolific";
        var start = DateTime.UtcNow.AddDays(-10);
        await using (var context = NewContext())
        {
            context.Users.Add(new User { id = prolific, email = $"{prolific}@example.test", name = prolific, createdAt = start, updatedAt = start });
            // Project 000 is the least recently touched; the five oldest fall past the cap.
            for (var index = 0; index < MembershipLists.Cap + 5; index++)
            {
                var project = NewProject($"project-p{index:000}", $"Plot {index:000}", prolific, ProjectStatus.PUBLISHED, start);
                project.updatedAt = start.AddMinutes(index);
                context.Projects.Add(project);
            }
            await context.SaveChangesAsync();
        }

        var mine = await Projects().GetMembershipsAsync(new ProjectViewer(prolific, IsAdmin: false));

        var titles = mine.Value!.Select(project => project.Title).ToList();
        titles.Should().HaveCount(MembershipLists.Cap);
        titles.Should().NotContain(new[] { "Plot 000", "Plot 001", "Plot 002", "Plot 003", "Plot 004" });
        titles.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        mine.Value![0].Should().BeEquivalentTo(new
        {
            Id = "project-p005",
            Title = "Plot 005",
            Slug = "project-p005",
            Status = ProjectStatus.PUBLISHED,
            Categories = new[] { "CONSTRUCTION" },
        });
    }

    [PostgresFact]
    public async Task AMembersGuildList_IsCappedAtTheMostRecentlyActive()
    {
        var start = DateTime.UtcNow.AddDays(-10);
        await using (var context = NewContext())
        {
            for (var index = 0; index < MembershipLists.Cap + 5; index++)
            {
                var owner = $"user-owner-{index:000}";
                context.Users.Add(new User { id = owner, email = $"{owner}@example.test", name = owner, createdAt = start, updatedAt = start });
                var guild = NewGuild($"guild-g{index:000}", $"Guild {index:000}", owner, start);
                guild.updatedAt = start.AddMinutes(index);
                context.Guilds.Add(guild);
                context.GuildMembers.Add(new GuildMember
                {
                    id = $"guild-member-g{index:000}-dee",
                    guildId = guild.id,
                    userId = Dee,
                    role = GuildMemberRole.MEMBER,
                    joinedAt = start,
                });
            }
            await context.SaveChangesAsync();
        }

        var dees = await Guilds().GetMembershipsAsync(Dee);

        var names = dees.Value!.Select(guild => guild.Name).ToList();
        // 206 guilds: her own (last touched a month ago) and the five oldest joined ones fall past the cap.
        names.Should().HaveCount(MembershipLists.Cap);
        names.Should().NotContain(new[] { "Village cooks", "Guild 000", "Guild 001", "Guild 002", "Guild 003", "Guild 004" });
        names.Should().Contain("Guild 005");
        names.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    private ProjectService Projects()
    {
        var context = NewContext();
        return new ProjectService(
            new ProjectRepository(context),
            new Repository<User>(context),
            Moq.Mock.Of<Common.Interfaces.IProjectDeletionBlockers>(),
            new UnitOfWorkOverContext(context),
            Mapper);
    }

    private GuildService Guilds()
    {
        var context = NewContext();
        return new GuildService(
            new Repository<Guild>(context),
            new Repository<GuildMember>(context),
            new Repository<ProjectTask>(context),
            new UnitOfWorkOverContext(context),
            Mapper,
            new Repository<Project>(context));
    }

    private static async Task SeedAsync(ArdaNovaDbContext context)
    {
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        foreach (var id in new[] { Ada, Cy, Dee })
            context.Users.Add(new User { id = id, email = $"{id}@example.test", name = id, createdAt = monthAgo, updatedAt = monthAgo });

        context.Projects.Add(NewProject("project-orchard", "Orchard", Ada, ProjectStatus.PUBLISHED, monthAgo));
        context.Projects.Add(NewProject("project-barn", "Barn raising", Cy, ProjectStatus.PUBLISHED, monthAgo));
        context.Projects.Add(NewProject("project-shed", "Shed prototype", Cy, ProjectStatus.DRAFT, monthAgo));
        context.Projects.Add(NewProject("project-kitchen", "Village kitchen", Cy, ProjectStatus.PUBLISHED, monthAgo));
        context.ProjectMembers.Add(ProjectMember("project-barn", Ada, ProjectRole.CONTRIBUTOR, monthAgo));
        context.ProjectMembers.Add(ProjectMember("project-shed", Ada, ProjectRole.LEADER, monthAgo));

        // One guild per owner (unique Guild.ownerId).
        context.Guilds.Add(NewGuild("guild-orchardists", "Orchardists", Ada, monthAgo));
        context.Guilds.Add(NewGuild("guild-framers", "Timber framers", Cy, monthAgo));
        context.Guilds.Add(NewGuild("guild-cooks", "Village cooks", Dee, monthAgo));
        context.GuildMembers.Add(new GuildMember
        {
            id = "guild-member-framers-ada",
            guildId = "guild-framers",
            userId = Ada,
            role = GuildMemberRole.MEMBER,
            joinedAt = monthAgo,
        });
        await context.SaveChangesAsync();
    }

    private static Project NewProject(string id, string title, string creatorId, ProjectStatus status, DateTime at) => new()
    {
        id = id,
        title = title,
        slug = id,
        description = $"{title}, a community project.",
        problemStatement = "Something is missing.",
        solution = "Build it together.",
        categories = "CONSTRUCTION",
        status = status,
        createdById = creatorId,
        createdAt = at,
        updatedAt = at,
        publishedAt = status == ProjectStatus.DRAFT ? null : at,
    };

    private static ProjectMember ProjectMember(string projectId, string userId, ProjectRole role, DateTime at) => new()
    {
        id = $"member-{projectId}-{userId}",
        projectId = projectId,
        userId = userId,
        role = role,
        joinedAt = at,
    };

    private static Guild NewGuild(string id, string name, string ownerId, DateTime at) => new()
    {
        id = id,
        name = name,
        slug = id,
        description = $"{name}, a guild.",
        email = $"{id}@example.test",
        ownerId = ownerId,
        createdAt = at,
        updatedAt = at,
    };

    private ArdaNovaDbContext NewContext() => new(_options!);
}
