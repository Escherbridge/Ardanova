namespace ArdaNova.API.Tests.Flows;

using System.Security.Claims;
using System.Text.Json;
using ArdaNova.API.Controllers;
using ArdaNova.API.Filters;
using ArdaNova.API.Middleware;
using ArdaNova.Application;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tasks;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using ArdaNova.Infrastructure.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Npgsql.NameTranslation;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;
using UnitOfWorkOverContext = ArdaNova.Infrastructure.UnitOfWork.UnitOfWork;

/// <summary>
/// SR-03 phase 3 Lane K1: asking for help, picking up an ask, and the two come-back notifications, driven through
/// <see cref="PostsController"/> and <see cref="TasksController"/> over the app's own service graph on a throwaway
/// Postgres database. Only the AZOA account-readiness read (the process edge) is faked. Without
/// <c>ARDANOVA_TEST_POSTGRES</c> every test reports SKIPPED.
/// </summary>
[Trait("Category", "Database")]
public sealed class HelpWantedFlowTests : IAsyncLifetime
{
    private const string Fern = "user-fern";
    private const string Lee = "user-lee";
    private const string Bea = "user-bea";
    private const string Ari = "user-ari";
    private const string Ash = "user-ash";
    private const string Cy = "user-cy";

    private const string BarnId = "project-barn";
    private const string KitchenId = "project-kitchen";
    private const string DraftId = "project-draft";

    private const string FrameTask = "task-frame";
    private const string PaidTask = "task-paid";
    private const string DoneTask = "task-done";
    private const string EscrowTask = "task-escrow";
    private const string TakenTask = "task-taken";
    private const string DraftTask = "task-draft";

    private readonly Readiness _readiness = new();
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;
    private ServiceProvider? _services;

    public async Task InitializeAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_helpwanted_" + Guid.NewGuid().ToString("N")[..12],
        };
        var translator = new NpgsqlNullNameTranslator();
        var enumTypes = typeof(PostType).Assembly.GetTypes()
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

        await using (var context = NewContext())
        {
            await context.Database.EnsureCreatedAsync();
            // The CHECK migration 20261101000000_feed_help_wanted adds beside the unique index EF already builds.
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "Post" ADD CONSTRAINT "Post_taskId_help_wanted_only"
                  CHECK ("taskId" IS NULL OR "type"::text = 'HELP_WANTED');
                """);
            await SeedAsync(context);
        }

        // The app's own registrations (AddApplication) over this database; the AZOA readiness read is the only fake.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddScoped(_ => NewContext());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWorkOverContext>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectDeletionBlockers, ProjectDeletionBlockers>();
        services.AddScoped<ITaskClaimStore, TaskClaimStore>();
        services.AddSingleton<IAzoaValueEligibilityService>(_readiness);
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
            await _services.DisposeAsync();
        if (_options is null)
            return;

        await using (var context = NewContext())
            await context.Database.EnsureDeletedAsync();
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
    }

    // -- Asking ------------------------------------------------------------------------------------

    [PostgresFact]
    public async Task AMemberAsksForHelp_TheTaskNamesTheAsker_AndPickingUpYourOwnAskNotifiesNoOne()
    {
        var created = await AskAsync(Bea, FrameTask, "Framing the east wall Saturday, need two hands #carpentry");

        StatusOf(created).Should().Be(201);
        var ask = ((ObjectResult)created).Value.Should().BeOfType<PostDto>().Subject;
        ask.Type.Should().Be(nameof(PostType.HELP_WANTED));
        ask.TaskId.Should().Be(FrameTask);
        ask.ProjectId.Should().Be(BarnId, "an ask lives on its task's project even when the composer sent none");

        // Anyone who can see the task sees who asked, so "Message" needs no extra route.
        var task = await ReadTaskAsync(Cy, FrameTask);
        task.HelpWanted.Should().Be(new TaskHelpWantedDto { PostId = ask.Id, AskerId = Bea, AskerName = "Bea" });

        StatusOf(await ClaimAsync(Bea, FrameTask)).Should().Be(200);
        (await AssigneeAsync(FrameTask)).Should().Be(Bea);
        (await NotificationsAsync(Bea)).Should().BeEmpty("picking up your own ask is not news");
    }

    [PostgresFact]
    public async Task AnAsk_IsRefused_UnlessItNamesAnOpenTaskOnAProjectTheAuthorBelongsTo()
    {
        var table = new (string Actor, PostType Type, string? TaskId, string? ProjectId, int Status, string Why)[]
        {
            (Cy, PostType.HELP_WANTED, FrameTask, null, 403, "a non-member cannot ask on a team's task"),
            (Cy, PostType.HELP_WANTED, DraftTask, null, 404, "a draft's task reads as missing to an outsider"),
            (Bea, PostType.HELP_WANTED, "task-missing", null, 404, "no such task"),
            (Bea, PostType.HELP_WANTED, null, null, 400, "an ask names its task"),
            (Bea, PostType.POST, FrameTask, BarnId, 400, "only an ask names a task"),
            (Bea, PostType.HELP_WANTED, DoneTask, null, 400, "a done task needs no help"),
            (Bea, PostType.HELP_WANTED, FrameTask, KitchenId, 400, "the post's project must be the task's"),
        };

        foreach (var row in table)
        {
            var result = await AskAsync(row.Actor, row.TaskId, "Need a hand", row.Type, row.ProjectId);
            StatusOf(result).Should().Be(row.Status, row.Why);
        }

        await using var context = NewContext();
        (await context.Posts.CountAsync()).Should().Be(0, "a refused ask writes nothing");
    }

    [PostgresFact]
    public async Task ASecondAskOnATask_Is409NamingTheFirst_OnTheWire()
    {
        var firstId = await AskIdAsync(Bea, FrameTask);

        var second = await AskAsync(Ari, FrameTask, "Could also use someone on the trusses");

        StatusOf(second).Should().Be(409);
        var body = WireBody(second);
        body.GetProperty("code").GetString().Should().Be(PostService.HelpWantedExistsCode);
        body.GetProperty("conflictId").GetString().Should().Be(firstId, "the composer offers \"Edit your ask\" on this id");
        body.GetProperty("error").GetString().Should().Be(PostService.AskAlreadyOpen);
        body.GetProperty("statusCode").GetInt32().Should().Be(409);
        await using var context = NewContext();
        (await context.Posts.CountAsync(post => post.taskId == FrameTask)).Should().Be(1);
    }

    [PostgresFact]
    public async Task AManagerRemovesTheAskOfAnAuthorWhoLeft_AndTheTaskCanBeAskedAboutAgain()
    {
        var askId = await AskIdAsync(Bea, FrameTask);
        StatusOf(await ReplyAsync(Cy, askId, "I can help Saturday")).Should().Be(201);
        await using (var context = NewContext())
        {
            context.ProjectMembers.Remove(await context.ProjectMembers.SingleAsync(member => member.userId == Bea && member.projectId == BarnId));
            await context.SaveChangesAsync();
        }

        StatusOf(await DeletePostAsync(Ari, askId)).Should().Be(403, "a fellow member cannot remove someone's ask");
        StatusOf(await DeletePostAsync(Lee, askId)).Should().Be(204, "a manager can, so a departed author never blocks the task");

        await using (var context = NewContext())
        {
            (await context.Posts.AnyAsync(post => post.id == askId)).Should().BeFalse();
            (await context.Notifications.AnyAsync(note => note.postId == askId)).Should().BeFalse("the ask's notifications go with it");
        }
        StatusOf(await AskAsync(Ari, FrameTask, "Still need two hands for the east wall")).Should().Be(201);
    }

    // -- Picking up --------------------------------------------------------------------------------

    [PostgresFact]
    public async Task TwoMembersPickUpTheSameAskAtOnce_ExactlyOneGetsIt_AndTheAskerHearsOnce()
    {
        var askId = await AskIdAsync(Bea, PaidTask);
        // Both requests read the task as open and pass their checks before either writes: the paid task's readiness
        // read is the seam a concurrent request really crosses between its read and its write.
        var arrived = 0;
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _readiness.During = async () =>
        {
            if (Interlocked.Increment(ref arrived) == 2)
                bothRead.SetResult();
            await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(20));
        };

        var results = await Task.WhenAll(ClaimAsync(Ari, PaidTask), ClaimAsync(Ash, PaidTask));

        arrived.Should().Be(2, "both claims read the task as open before either wrote");
        results.Select(StatusOf).Should().BeEquivalentTo(new[] { 200, 409 });
        var winner = StatusOf(results[0]) == 200 ? Ari : Ash;
        WireBody(results.Single(result => StatusOf(result) == 409))
            .GetProperty("code").GetString().Should().Be(TaskClaimCodes.Taken);
        (await AssigneeAsync(PaidTask)).Should().Be(winner);

        var note = (await NotificationsAsync(Bea)).Should().ContainSingle("only the claim that landed tells the asker").Subject;
        note.type.Should().Be(NotificationType.TASK_ASSIGNED);
        note.postId.Should().Be(askId);
        note.title.Should().Be("Picked up");
        note.message.Should().Be($"{(winner == Ari ? "Ari" : "Ash")} took Wire the shed");
        note.actionUrl.Should().Be($"/tasks/{PaidTask}");
    }

    [PostgresFact]
    public async Task AClaimThatCannotLand_Is409WithItsReason_AndChangesNothing()
    {
        await AskIdAsync(Bea, EscrowTask);
        await AskIdAsync(Bea, TakenTask);
        var table = new (string TaskId, string Code, string? Assignee)[]
        {
            (DoneTask, TaskClaimCodes.Done, null),
            (EscrowTask, TaskClaimCodes.UnderAgreement, null),
            (TakenTask, TaskClaimCodes.Taken, Ash),
        };

        foreach (var row in table)
        {
            var refused = await ClaimAsync(Ari, row.TaskId);
            StatusOf(refused).Should().Be(409, row.TaskId);
            WireBody(refused).GetProperty("code").GetString().Should().Be(row.Code, row.TaskId);
            (await AssigneeAsync(row.TaskId)).Should().Be(row.Assignee, row.TaskId);
        }

        (await NotificationsAsync(Bea)).Should().BeEmpty("no claim landed");
    }

    [PostgresFact]
    public async Task APaidTask_StillChecksTheClaimersAccountSetupBeforeTheWrite()
    {
        _readiness.NotReady.Add(Ari);

        StatusOf(await ClaimAsync(Ari, PaidTask)).Should().Be(403);
        (await AssigneeAsync(PaidTask)).Should().BeNull("an unfinished setup stops the claim before it writes");

        _readiness.NotReady.Remove(Ari);
        StatusOf(await ClaimAsync(Ari, PaidTask)).Should().Be(200);
        (await AssigneeAsync(PaidTask)).Should().Be(Ari);
    }

    // -- Replies -----------------------------------------------------------------------------------

    [PostgresFact]
    public async Task RepliesFoldIntoOneUnreadNotificationPerPost_AndYourOwnReplyIsSilent()
    {
        var askId = await AskIdAsync(Bea, FrameTask);

        await ReplyAsync(Bea, askId, "Bringing the nail gun");
        (await NotificationsAsync(Bea)).Should().BeEmpty("replying on your own post is not news");

        var longReply = string.Concat(Enumerable.Repeat("0123456789", 9));
        StatusOf(await ReplyAsync(Cy, askId, longReply)).Should().Be(201);
        var first = (await NotificationsAsync(Bea)).Should().ContainSingle().Subject;
        first.type.Should().Be(NotificationType.COMMENT_REPLY);
        first.postId.Should().Be(askId);
        first.title.Should().Be("New reply");
        first.message.Should().Be($"Cy: “{string.Concat(Enumerable.Repeat("0123456789", 8))}…”");
        first.actionUrl.Should().Be($"/projects/{BarnId}");

        await ReplyAsync(Ari, askId, "Count me\n   in");
        await ReplyAsync(Bea, askId, "Thanks both");
        var folded = (await NotificationsAsync(Bea)).Should().ContainSingle("replies fold into the one unread row").Subject;
        folded.id.Should().Be(first.id);
        folded.title.Should().Be("2 new replies");
        folded.message.Should().Be("Ari: “Count me in”");

        // Reading it ends the fold: the next reply opens a fresh notification.
        await using (var scope = _services!.CreateAsyncScope())
            (await scope.ServiceProvider.GetRequiredService<INotificationService>().MarkAsReadAsync(folded.id)).IsSuccess.Should().BeTrue();
        await ReplyAsync(Cy, askId, "See you at nine");
        var notes = await NotificationsAsync(Bea);
        notes.Should().HaveCount(2);
        notes.Single(note => !note.isRead).title.Should().Be("New reply");
    }

    // -- Seams -------------------------------------------------------------------------------------

    private Task<IActionResult> AskAsync(
        string actor,
        string? taskId,
        string words,
        PostType type = PostType.HELP_WANTED,
        string? projectId = null)
        => AsAsync<PostsController>(actor, posts => posts.Create(
            new PostsController.CreatePostRequest(words, projectId, null, type, null, null, null, taskId),
            CancellationToken.None));

    private async Task<string> AskIdAsync(string actor, string taskId)
    {
        var created = await AskAsync(actor, taskId, "Need a hand here");
        StatusOf(created).Should().Be(201);
        return ((PostDto)((ObjectResult)created).Value!).Id;
    }

    private Task<IActionResult> ReplyAsync(string actor, string postId, string words)
        => AsAsync<PostsController>(actor, posts => posts.AddComment(
            postId, new PostsController.AddPostCommentRequest(words, null), CancellationToken.None));

    private Task<IActionResult> DeletePostAsync(string actor, string postId)
        => AsAsync<PostsController>(actor, posts => posts.Delete(postId, CancellationToken.None));

    private Task<IActionResult> ClaimAsync(string actor, string taskId)
        => AsAsync<TasksController>(actor, tasks => tasks.Assign(
            taskId, new AssignTaskDto { UserId = actor }, CancellationToken.None));

    private async Task<TaskDto> ReadTaskAsync(string actor, string taskId)
    {
        var read = await AsAsync<TasksController>(actor, tasks => tasks.GetById(taskId, CancellationToken.None));
        return read.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<TaskDto>().Subject;
    }

    /// <summary>One request: a fresh DI scope (its own DbContext and connection) and a signed actor, as the API pipeline gives each call.</summary>
    private async Task<IActionResult> AsAsync<TController>(string actor, Func<TController, Task<IActionResult>> request)
        where TController : ControllerBase
    {
        await using var scope = _services!.CreateAsyncScope();
        var controller = ActivatorUtilities.CreateInstance<TController>(scope.ServiceProvider);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, actor),
                        new Claim("sub", actor),
                        new Claim(ActorAssertionMiddleware.ClaimType, "v2"),
                    ],
                    ActorAssertionMiddleware.AuthenticationType)),
            },
        };
        return await request(controller);
    }

    private static int StatusOf(IActionResult result) => result switch
    {
        ForbidResult => 403,
        ObjectResult objectResult => objectResult.StatusCode ?? 200,
        StatusCodeResult statusResult => statusResult.StatusCode,
        _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}"),
    };

    /// <summary>The body exactly as the pipeline writes it: the error envelope filter, then the API's JSON options.</summary>
    private static JsonElement WireBody(IActionResult result)
    {
        var normalized = (ObjectResult)ErrorEnvelopeResultFilter.Normalize(new DefaultHttpContext(), result);
        return JsonSerializer.SerializeToElement(normalized.Value, ArdaNovaJson.Configure(new JsonSerializerOptions()));
    }

    private async Task<string?> AssigneeAsync(string taskId)
    {
        await using var context = NewContext();
        return await context.ProjectTasks.Where(task => task.id == taskId).Select(task => task.assignedToId).SingleAsync();
    }

    private async Task<List<Notification>> NotificationsAsync(string userId)
    {
        await using var context = NewContext();
        return await context.Notifications.AsNoTracking()
            .Where(note => note.userId == userId)
            .OrderBy(note => note.createdAt)
            .ToListAsync();
    }

    /// <summary>
    /// The barn (Fern founded it; Lee leads; Bea, Ari and Ash contribute; Cy is outside), Bea's own kitchen, and
    /// Fern's draft. Tasks: open, paid, done, escrow-funded, already Ash's, and one on the draft.
    /// </summary>
    private static async Task SeedAsync(ArdaNovaDbContext context)
    {
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        foreach (var id in new[] { Fern, Lee, Bea, Ari, Ash, Cy })
        {
            var name = char.ToUpperInvariant(id[5]) + id[6..];
            context.Users.Add(new User { id = id, email = $"{id}@example.test", name = name, createdAt = monthAgo, updatedAt = monthAgo });
        }

        context.Projects.Add(NewProject(BarnId, "Barn raising", Fern, ProjectStatus.PUBLISHED, monthAgo));
        context.Projects.Add(NewProject(KitchenId, "Village kitchen", Bea, ProjectStatus.PUBLISHED, monthAgo));
        context.Projects.Add(NewProject(DraftId, "Shed prototype", Fern, ProjectStatus.DRAFT, monthAgo));
        context.ProjectMembers.Add(Member(Lee, ProjectRole.LEADER, monthAgo));
        foreach (var contributor in new[] { Bea, Ari, Ash })
            context.ProjectMembers.Add(Member(contributor, ProjectRole.CONTRIBUTOR, monthAgo));

        context.ProjectTasks.Add(NewTask(FrameTask, BarnId, "Frame the east wall", monthAgo));
        context.ProjectTasks.Add(NewTask(PaidTask, BarnId, "Wire the shed", monthAgo, reward: 25m));
        context.ProjectTasks.Add(NewTask(DoneTask, BarnId, "Pour the footings", monthAgo, status: TaskStatus.COMPLETED));
        context.ProjectTasks.Add(NewTask(EscrowTask, BarnId, "Raise the roof", monthAgo, escrow: EscrowStatus.FUNDED));
        context.ProjectTasks.Add(NewTask(TakenTask, BarnId, "Hang the doors", monthAgo, assignee: Ash));
        context.ProjectTasks.Add(NewTask(DraftTask, DraftId, "Sketch the shed", monthAgo));
        await context.SaveChangesAsync();
    }

    private static Project NewProject(string id, string title, string founder, ProjectStatus status, DateTime at) => new()
    {
        id = id,
        title = title,
        slug = id,
        description = $"{title}, a community project.",
        problemStatement = "Something is missing.",
        solution = "Build it together.",
        categories = "CONSTRUCTION",
        status = status,
        createdById = founder,
        createdAt = at,
        updatedAt = at,
        publishedAt = status == ProjectStatus.DRAFT ? null : at,
    };

    private static ProjectMember Member(string userId, ProjectRole role, DateTime at) => new()
    {
        id = $"member-{userId}",
        projectId = BarnId,
        userId = userId,
        role = role,
        joinedAt = at,
    };

    private static ProjectTask NewTask(
        string id,
        string projectId,
        string title,
        DateTime at,
        TaskStatus status = TaskStatus.TODO,
        EscrowStatus escrow = EscrowStatus.NONE,
        decimal? reward = null,
        string? assignee = null) => new()
    {
        id = id,
        projectId = projectId,
        title = title,
        status = status,
        priority = TaskPriority.MEDIUM,
        taskType = TaskType.FEATURE,
        escrowStatus = escrow,
        equityReward = reward,
        assignedToId = assignee,
        completedAt = status == TaskStatus.COMPLETED ? at : null,
        createdAt = at,
        updatedAt = at,
    };

    private ArdaNovaDbContext NewContext() => new(_options!);

    /// <summary>The AZOA account read: ready unless listed, with an optional pause to line up concurrent requests.</summary>
    private sealed class Readiness : IAzoaValueEligibilityService
    {
        public HashSet<string> NotReady { get; } = new(StringComparer.Ordinal);

        public Func<Task>? During { get; set; }

        public async Task<Result<AzoaCustodialAccountStatusDto>> RequireReadyAsync(
            string userId,
            string action,
            CancellationToken ct = default)
        {
            if (During is { } during)
                await during();
            return NotReady.Contains(userId)
                ? Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete(action))
                : Result<AzoaCustodialAccountStatusDto>.Success(new AzoaCustodialAccountStatusDto());
        }
    }
}
