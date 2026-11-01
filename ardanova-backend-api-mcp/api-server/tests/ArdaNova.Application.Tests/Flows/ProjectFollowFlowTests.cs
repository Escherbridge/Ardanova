namespace ArdaNova.Application.Tests.Flows;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Feed;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Feed;
using ArdaNova.Infrastructure.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Npgsql.NameTranslation;
using UnitOfWorkOverContext = ArdaNova.Infrastructure.UnitOfWork.UnitOfWork;

/// <summary>
/// SR-03 Phase 3 §8-6: project follow hardens the three flaws GuildFollowService has. Real
/// Postgres so the (userId, projectId) unique index backs the idempotency claims for real, not
/// just a pre-check in front of an InMemoryRepository that has no constraint to race against.
/// </summary>
/// <remarks>
/// Needs <c>ARDANOVA_TEST_POSTGRES</c> (a connection string without a database name); without it
/// every test reports SKIPPED via <see cref="PostgresFactAttribute"/>.
/// </remarks>
[Trait("Category", "Database")]
public sealed class ProjectFollowFlowTests : IAsyncLifetime
{
    private const string Founder = "user-founder";
    private const string Follower = "user-follower";
    private const string Stranger = "user-stranger";
    private const string PublicProjectId = "project-public";
    private const string DraftProjectId = "project-draft";

    private static readonly IMapper Mapper = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<MappingProfile>();
        MappingSecurityPolicy.Apply(configuration);
    }).CreateMapper();

    private readonly TimeProvider _clock = TimeProvider.System;
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    public async Task InitializeAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_follow_" + Guid.NewGuid().ToString("N")[..12],
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
    public async Task FollowingAHiddenProject_AsANonMember_IsRefusedTheSame404AMissingProjectWould()
    {
        var result = await FollowService().FollowAsync(DraftProjectId, Viewer(Stranger));

        result.Type.Should().Be(ResultType.NotFound, "a draft's existence is private to non-members, so follow refuses it exactly like a missing project");
    }

    [PostgresFact]
    public async Task FollowingTwice_BothSucceed_ButOnlyOneRowIsEverStored()
    {
        var service = FollowService();

        var first = await service.FollowAsync(PublicProjectId, Viewer(Follower));
        var second = await service.FollowAsync(PublicProjectId, Viewer(Follower));

        first.IsSuccess.Should().BeTrue(first.Error);
        second.IsSuccess.Should().BeTrue(second.Error);
        second.Value!.Id.Should().Be(first.Value!.Id, "the double follow answers with the same row, not a new one");

        await using var context = NewContext();
        (await context.ProjectFollows.CountAsync(f => f.projectId == PublicProjectId && f.userId == Follower))
            .Should().Be(1);
    }

    /// <summary>
    /// The actual race GuildFollowService has no catch for (§8-6): two requests land in the gap
    /// between FollowAsync's pre-check and its save, on two separate DbContexts, so Postgres - not
    /// the pre-check - is what resolves the double insert. Both calls still succeed and only one row lands.
    /// </summary>
    [PostgresFact]
    public async Task TwoConcurrentFollowsOfTheSameProject_BothSucceed_AndOnlyOneRowIsStored()
    {
        var first = FollowService().FollowAsync(PublicProjectId, Viewer(Follower));
        var second = FollowService().FollowAsync(PublicProjectId, Viewer(Follower));
        await Task.WhenAll(first, second);

        first.Result.IsSuccess.Should().BeTrue(first.Result.Error);
        second.Result.IsSuccess.Should().BeTrue(second.Result.Error);
        second.Result.Value!.Id.Should().Be(first.Result.Value!.Id, "whichever write lost the race still reads back the winner's row");

        await using var context = NewContext();
        (await context.ProjectFollows.CountAsync(f => f.projectId == PublicProjectId && f.userId == Follower))
            .Should().Be(1);
    }

    [PostgresFact]
    public async Task UnfollowingAProjectTheViewerNeverFollowed_StillSucceeds()
    {
        var result = await FollowService().UnfollowAsync(PublicProjectId, Viewer(Stranger));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value.Should().BeTrue();
    }

    [PostgresFact]
    public async Task FollowingThenUnfollowing_LeavesNoRowBehind()
    {
        var service = FollowService();
        Succeeded(await service.FollowAsync(PublicProjectId, Viewer(Follower)));

        var unfollowed = await service.UnfollowAsync(PublicProjectId, Viewer(Follower));

        unfollowed.IsSuccess.Should().BeTrue(unfollowed.Error);
        await using var context = NewContext();
        (await context.ProjectFollows.AnyAsync(f => f.projectId == PublicProjectId && f.userId == Follower))
            .Should().BeFalse();
    }

    /// <summary>
    /// A double-tap unfollow: both requests have read the project and reached their delete before either commits (a
    /// held row lock lines them up), so the second delete finds nothing. That is still "not following", never a 500.
    /// </summary>
    [PostgresFact]
    public async Task TwoConcurrentUnfollows_BothSucceed_AndLeaveNoRow()
    {
        Succeeded(await FollowService().FollowAsync(PublicProjectId, Viewer(Follower)));

        await using var holder = await _dataSource!.OpenConnectionAsync();
        await using var holding = await holder.BeginTransactionAsync();
        await using (var hold = new NpgsqlCommand(
            """SELECT 1 FROM "ProjectFollow" WHERE "projectId" = @project AND "userId" = @user FOR UPDATE""", holder, holding))
        {
            hold.Parameters.AddWithValue("project", PublicProjectId);
            hold.Parameters.AddWithValue("user", Follower);
            (await hold.ExecuteScalarAsync()).Should().Be(1);
        }
        var first = FollowService().UnfollowAsync(PublicProjectId, Viewer(Follower));
        var second = FollowService().UnfollowAsync(PublicProjectId, Viewer(Follower));
        await WaitForLockWaitersAsync(2);
        await holding.CommitAsync();

        foreach (var unfollowed in await Task.WhenAll(first, second))
            unfollowed.IsSuccess.Should().BeTrue(unfollowed.Error);
        await using var context = NewContext();
        (await context.ProjectFollows.AnyAsync(f => f.projectId == PublicProjectId && f.userId == Follower))
            .Should().BeFalse();
    }

    /// <summary>Cross-lane flow (join step 3, Phase 2's read side): a follower who is not on the team still gets the card, with FollowedProject as its reason.</summary>
    [PostgresFact]
    public async Task AfterFollowing_TheHomeFeedGivesTheProjectsUpdateTheFollowedProjectReason()
    {
        Succeeded(await FollowService().FollowAsync(PublicProjectId, Viewer(Follower)));

        const string words = "Trusses go up this weekend.";
        await using (var context = NewContext())
        {
            var updates = new ProjectUpdateService(new Repository<ProjectUpdate>(context), new UnitOfWorkOverContext(context), Mapper);
            Succeeded(await updates.CreateAsync(new CreateProjectUpdateDto
            {
                ProjectId = PublicProjectId,
                UserId = Founder,
                Title = "Trusses",
                Content = words,
            }));
        }

        (await RunFeedCycleAsync()).Projected.Should().BeGreaterThan(0);

        var page = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Follower)));

        var card = page.Items.Should().ContainSingle(item => item.Body == words).Subject;
        card.Reasons.Should().ContainSingle(reason => reason.Kind == nameof(FeedReasonKind.FollowedProject));
    }

    private ProjectFollowService FollowService()
    {
        var context = NewContext();
        var projectService = new ProjectService(
            new ProjectRepository(context),
            new Repository<User>(context),
            Moq.Mock.Of<Common.Interfaces.IProjectDeletionBlockers>(),
            new UnitOfWorkOverContext(context),
            Mapper);
        return new ProjectFollowService(new Repository<ProjectFollow>(context), projectService, new UnitOfWorkOverContext(context));
    }

    private FeedService Feed()
    {
        var context = NewContext();
        return new FeedService(
            new Repository<Post>(context),
            new Repository<Project>(context),
            new Repository<Guild>(context),
            new FeedSourceReader(context),
            new FeedReadStore(context),
            _clock);
    }

    private async Task<FeedProjectorRun> RunFeedCycleAsync()
    {
        await using var context = NewContext();
        return await new FeedProjectorCycle(
            new FeedSourceReader(context),
            new FeedEnvelopeStore(context),
            new Repository<Post>(context),
            new FeedProjectorOptions(),
            _clock).RunAsync(sweep: false, new FeedProjectorWatermark());
    }

    private static ProjectViewer Viewer(string userId) => new(userId, IsAdmin: false);

    /// <summary>Returns once <paramref name="count"/> requests on this database are queued behind a row lock.</summary>
    private async Task WaitForLockWaitersAsync(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            await using var command = _dataSource!.CreateCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'");
            if ((long)(await command.ExecuteScalarAsync())! >= count)
                return;
            await Task.Delay(25);
        }

        throw new TimeoutException($"Fewer than {count} unfollows ever queued behind the held row.");
    }

    private static T Succeeded<T>(Result<T> result)
    {
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static async Task SeedAsync(ArdaNovaDbContext context)
    {
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        foreach (var id in new[] { Founder, Follower, Stranger })
            context.Users.Add(new User { id = id, email = $"{id}@example.test", name = id, createdAt = monthAgo, updatedAt = monthAgo });

        context.Projects.Add(NewProject(PublicProjectId, "Barn raising", ProjectStatus.PUBLISHED, monthAgo));
        context.Projects.Add(NewProject(DraftProjectId, "Shed prototype", ProjectStatus.DRAFT, monthAgo));
        await context.SaveChangesAsync();
    }

    private static Project NewProject(string id, string title, ProjectStatus status, DateTime at) => new()
    {
        id = id,
        title = title,
        slug = id,
        description = $"{title}, a community project.",
        problemStatement = "Something is missing.",
        solution = "Build it together.",
        categories = "CONSTRUCTION",
        status = status,
        createdById = Founder,
        createdAt = at,
        updatedAt = at,
        publishedAt = status == ProjectStatus.DRAFT ? null : at,
    };

    private ArdaNovaDbContext NewContext() => new(_options!);
}
