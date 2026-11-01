namespace ArdaNova.Application.Tests.Flows;

using System.Data.Common;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Feed;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
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
/// SR-03 phase 2, the read side end to end: real services write, the real projector projects, the real quality
/// pass scores and the real FeedService reads - on a throwaway Postgres database, so visibility predicates, keyset
/// paging and the hashtag containment run as SQL. Nothing is faked. Gated like
/// <see cref="FeedProjectorFlowTests"/>: without <c>ARDANOVA_TEST_POSTGRES</c> every test reports SKIPPED.
/// </summary>
[Trait("Category", "Database")]
public sealed class FeedReadFlowTests : IAsyncLifetime
{
    private const string Ada = "user-ada";
    private const string Bo = "user-bo";
    private const string Fern = "user-fern";
    private const string Sol = "user-sol";
    private const string Tam = "user-tam";
    private const string BarnId = "project-barn";
    private const string BarnTitle = "Barn raising";
    private const string DraftId = "project-draft";

    private static readonly IMapper Mapper = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<MappingProfile>();
        MappingSecurityPolicy.Apply(configuration);
    }).CreateMapper();

    /// <summary>The real clock: services stamp rows with wall time, and a page's asOf must not predate them.</summary>
    private readonly TimeProvider _clock = TimeProvider.System;
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    public async Task InitializeAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_feedread_" + Guid.NewGuid().ToString("N")[..12],
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

        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "Post" ADD CONSTRAINT "Post_sourceType_sourceId_check"
              CHECK (("sourceType" IS NULL) = ("sourceId" IS NULL));
            CREATE INDEX "Post_hashtags_idx" ON "Post" USING GIN ("hashtags");
            """);
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
    public async Task AFollower_SeesATeammatesUpdate_InTheirOwnWords_AboveNewerStrangersNews_WithABecauseYouFollowReason()
    {
        await FollowProjectAsync(Ada, BarnId);
        const string words = "Framed the south wall today; the inspector comes Friday.";
        await using (var context = NewContext())
        {
            Succeeded(await Updates(context).CreateAsync(new CreateProjectUpdateDto
            {
                ProjectId = BarnId,
                UserId = Bo,
                Title = "South wall framed",
                Content = words,
            }));
        }

        var newerElsewhere = await WriteNoteAsync(Sol, "project-kitchen", "Fresh bread from the new oven.");

        (await RunCycleAsync()).Projected.Should().BeGreaterThan(0);

        var page = await PageAsync(Ada);

        var first = page.Items[0];
        first.PostType.Should().Be(nameof(PostType.PROJECT_UPDATE));
        first.Author.Id.Should().Be(Bo);
        first.Body.Should().Be(words, "an update's card shows the writer's own words, read live from the update");
        first.BodyIsUserWritten.Should().BeTrue();
        first.Reasons.Should().ContainSingle(reason => reason.Kind == nameof(FeedReasonKind.FollowedProject))
            .Which.Text.Should().Be($"Because you follow {BarnTitle}");
        first.IsDiscovery.Should().BeFalse();
        first.Viewer.Should().Be(new FeedViewerRelationDto(IsMember: false, IsFollowing: true, CanClaim: false));
        (await PageAsync(Bo)).Items.Single(card => card.Id == first.Id).Viewer.IsMember
            .Should().BeTrue("Bo is on the barn's team");

        var stranger = page.Items.Should().ContainSingle(card => card.Id == newerElsewhere).Subject;
        stranger.IsDiscovery.Should().BeTrue("Ada follows nothing about the kitchen");
        stranger.Reasons[0].Kind.Should().Be(nameof(FeedReasonKind.Discovery));
    }

    [PostgresFact]
    public async Task APrivateProjectsActivity_NeverReachesANonMember_ByHome_ByScope_OrByAHandCraftedCursor()
    {
        var draftNote = await WriteNoteAsync(Fern, DraftId, "Secret prototype in the shed.");
        string draftMilestoneId;
        await using (var context = NewContext())
        {
            var milestones = new ProjectMilestoneService(new Repository<ProjectMilestone>(context), new UnitOfWorkOverContext(context), Mapper);
            draftMilestoneId = Succeeded(await milestones.CreateAsync(new CreateProjectMilestoneDto { ProjectId = DraftId, Title = "Prototype runs" })).Id;
            Succeeded(await milestones.CompleteAsync(draftMilestoneId));
        }

        string backlogTalkId;
        await using (var context = NewContext())
        {
            backlogTalkId = Succeeded(await Comments(context).CreateAsync(new CreateProjectCommentDto
            {
                ProjectId = BarnId,
                UserId = Bo,
                Content = "Team only: the budget for the PBI is tight.",
                TargetType = CommentTargetType.PBI,
                TargetId = "pbi-roof",
            })).Id;
        }

        var publicNote = await WriteNoteAsync(Fern, BarnId, "Open day on Saturday, everyone welcome.");
        await RunCycleAsync();

        string draftCardId;
        string backlogCardId;
        await using (var context = NewContext())
        {
            draftCardId = await context.Posts.Where(post => post.sourceId == draftMilestoneId).Select(post => post.id).SingleAsync();
            backlogCardId = await context.Posts.Where(post => post.sourceId == backlogTalkId).Select(post => post.id).SingleAsync();
        }

        var privateCards = new[] { draftNote, draftCardId, backlogCardId };

        var solSees = await WalkAsync(Sol);
        solSees.Should().Contain(publicNote, "the walk is not vacuous: Sol does see the published project");
        solSees.Should().NotContain(privateCards);

        (await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{DraftId}"))).Type
            .Should().Be(ResultType.NotFound, "a draft project answers a stranger exactly like a missing one");

        // A cursor Sol made by hand: a window spanning all time, nothing served, a future asOf, "exhausted".
        var forged = new FeedCursor(
            DateTime.UtcNow.AddDays(30), "home", null, new FeedKey(DateTime.UtcNow.AddYears(-50), "0"),
            new HashSet<ulong>(), Exhausted: true).Encode();
        var forgedPage = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Cursor: forged, Limit: 50)));
        forgedPage.Items.Select(card => card.Id).Should().Contain(publicNote).And.NotContain(privateCards);

        (await WalkAsync(Tam)).Should().Contain(new[] { draftNote, draftCardId }, "the draft's own team sees its cards");
        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Tam, $"project:{DraftId}", Limit: 50))).Items
            .Select(card => card.Id).Should().Contain(new[] { draftNote, draftCardId });
        (await WalkAsync(Bo)).Should().Contain(backlogCardId, "backlog talk stays with the project's team");
    }

    [PostgresFact]
    public async Task RetractingAnOpportunity_TakesItsCardOffEveryRead_AndReopening_RestoresTheSameCardWithItsReplies()
    {
        string opportunityId;
        await using (var context = NewContext())
        {
            opportunityId = Succeeded(await Opportunities(context).CreateAsync(new CreateOpportunityDto
            {
                PosterId = Fern,
                Title = "Site carpenter",
                Description = "Frame the north wall.",
                Type = OpportunityType.PROJECT_ROLE,
                ExperienceLevel = ExperienceLevel.MID,
                ProjectId = BarnId,
                IsOpenForApplications = false,
            })).Id;
            Succeeded(await Opportunities(context).UpdateAsync(opportunityId, new UpdateOpportunityDto { Status = OpportunityStatus.OPEN }));
        }

        await RunCycleAsync();
        Post before;
        await using (var context = NewContext())
            before = await context.Posts.AsNoTracking().SingleAsync(post => post.sourceId == opportunityId);
        await using (var context = NewContext())
        {
            Succeeded(await Posts(context).LikeAsync(before.id, Ada));
            Succeeded(await Posts(context).AddCommentAsync(before.id, new CreatePostCommentDto { AuthorId = Ada, Content = "I can start Monday." }));
        }

        (await WalkAsync(Ada)).Should().Contain(before.id);

        await using (var context = NewContext())
            Succeeded(await Opportunities(context).UpdateAsync(opportunityId, new UpdateOpportunityDto { Status = OpportunityStatus.DRAFT }));
        (await RunCycleAsync()).Retracted.Should().Be(1);

        (await WalkAsync(Ada)).Should().NotContain(before.id);
        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Ada, $"project:{BarnId}", Limit: 50))).Items
            .Select(card => card.Id).Should().NotContain(before.id);
        await using (var context = NewContext())
        {
            var posts = Posts(context);
            (await posts.GetForViewerAsync(before.id, Ada)).Type.Should().Be(ResultType.NotFound);
            (await posts.GetCommentsForViewerAsync(before.id, Ada)).Type.Should().Be(ResultType.NotFound);
            (await posts.LikeAsync(before.id, Sol)).Type.Should().Be(ResultType.NotFound);
            (await posts.AddCommentAsync(before.id, new CreatePostCommentDto { AuthorId = Sol, Content = "Still open?" }))
                .Type.Should().Be(ResultType.NotFound);
        }

        await using (var context = NewContext())
            Succeeded(await Opportunities(context).UpdateAsync(opportunityId, new UpdateOpportunityDto { Status = OpportunityStatus.OPEN }));
        (await RunCycleAsync()).Restored.Should().Be(1);

        await using (var check = NewContext())
        {
            var after = await check.Posts.AsNoTracking().SingleAsync(post => post.sourceId == opportunityId);
            after.id.Should().Be(before.id, "re-opening restores the same card, never a fresh one");
            after.createdAt.Should().Be(before.createdAt, "a restored card is not bumped as new");
            after.retractedAt.Should().BeNull();
            after.likesCount.Should().Be(1);
            (await check.PostLikes.CountAsync(like => like.postId == before.id && like.userId == Ada)).Should().Be(1);
        }

        await using (var context = NewContext())
            Succeeded(await Posts(context).GetCommentsForViewerAsync(before.id, Ada)).Should().ContainSingle()
                .Which.Content.Should().Be("I can start Monday.");
        (await WalkAsync(Ada)).Should().Contain(before.id);
    }

    [PostgresFact]
    public async Task WalkingTheHomeFeed_NeverRepeatsACard_KeepsEveryPageDiverse_AndRunsPastTheFollowedPoolIntoDiscovery()
    {
        await FollowProjectAsync(Ada, BarnId);
        var others = Enumerable.Range(1, 12).Select(index => $"project-other-{index}").ToList();
        await using (var context = NewContext())
        {
            foreach (var (projectId, index) in others.Select((id, index) => (id, index)))
            {
                var founder = $"user-founder-{index}";
                context.Users.Add(NewUser(founder));
                context.Projects.Add(NewProject(projectId, $"Workshop {index}", founder, ProjectStatus.PUBLISHED, "MANUFACTURING"));
            }

            await context.SaveChangesAsync();
        }

        // 4 followed + 48 discovery notes: one ranked window, so the order is the global ranking.
        var followed = new List<string>();
        var everyone = new List<string>();
        for (var round = 0; round < 4; round++)
        {
            foreach (var author in round < 2 ? new[] { Fern, Bo } : [])
            {
                var id = await WriteNoteAsync(author, BarnId, $"Barn note {round} by {author}.");
                followed.Add(id);
                everyone.Add(id);
            }

            foreach (var (projectId, index) in others.Select((id, index) => (id, index)))
                everyone.Add(await WriteNoteAsync($"user-founder-{index}", projectId, $"Workshop {index}, note {round}."));
        }

        var pages = new List<FeedPageDto>();
        string? cursor = null;
        do
        {
            var page = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Ada, Cursor: cursor, Limit: 20)));
            pages.Add(page);
            cursor = page.NextCursor;
        }
        while (cursor is not null && pages.Count < 20);

        var served = pages.SelectMany(page => page.Items).Select(card => card.Id).ToList();
        served.Should().OnlyHaveUniqueItems("no card is served twice across the scroll");
        served.Should().BeEquivalentTo(everyone, "the scroll ends only when everything visible has been served");

        foreach (var page in pages)
        {
            page.Items.GroupBy(card => card.Project!.Id).Should().OnlyContain(subject => subject.Count() <= FeedRanking.HomeSubjectCap);
            page.Items.GroupBy(card => card.Author.Id).Should().OnlyContain(author => author.Count() <= FeedRanking.HomeAuthorCap);
            for (var slot = FeedRanking.DiscoveryEvery; slot <= page.Items.Count; slot += FeedRanking.DiscoveryEvery)
                page.Items[slot - 1].IsDiscovery.Should().BeTrue($"slot {slot} is a discovery slot");
        }

        var lastFollowedPage = pages.FindLastIndex(page => page.Items.Any(card => followed.Contains(card.Id)));
        pages[0].Items.Should().HaveCount(20, "a full home page of 20 is assembled under the caps");
        pages[0].Items[0].Id.Should().BeOneOf(followed, "what Ada follows leads her feed");
        pages.Skip(lastFollowedPage + 1).Should().NotBeEmpty("the scroll runs on past the followed pool")
            .And.OnlyContain(page => page.Items.All(card => card.IsDiscovery));
    }

    [PostgresFact]
    public async Task AProjectsOwnFeed_IsNewestFirst_WithoutCapsOrDiscovery_AndItsCursorIsRefusedOnAnotherFeed()
    {
        var notes = new List<string>();
        for (var i = 0; i < 5; i++)
            notes.Add(await WriteNoteAsync(Fern, BarnId, $"Day {i} on the barn."));

        var page = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{BarnId}", Limit: 20)));

        page.Items.Select(card => card.Id).Should().Equal(Enumerable.Reverse(notes),
            "one subject, one author: all five, newest first - the home caps would have shown two");
        page.Items.Should().OnlyContain(card => !card.IsDiscovery);
        page.NextCursor.Should().BeNull();

        var projectCursor = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{BarnId}", Limit: 2))).NextCursor;
        var homeCursor = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Limit: 1))).NextCursor;
        (await Feed().GetPageAsync(new FeedPageRequest(Sol, Cursor: projectCursor))).Type.Should().Be(ResultType.ValidationError);
        (await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{BarnId}", homeCursor))).Type.Should().Be(ResultType.ValidationError);
        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{BarnId}", projectCursor, Limit: 20))).Items
            .Select(card => card.Id).Should().Equal(Enumerable.Reverse(notes).Skip(2), "the project cursor continues its own feed");
    }

    [PostgresFact]
    public async Task ANoteWithMixedCaseDuplicateTags_StoresOneOfEach_AndTheTagScopeServesItOnlyWhereThePostMayBeSeen()
    {
        var publicNote = await WriteNoteAsync(Fern, BarnId,
            "Raised the frame #Construction #construction #Carpentry - photos at example.com/#gallery");
        var draftNote = await WriteNoteAsync(Fern, DraftId, "Prototype joinery #construction");

        await using (var context = NewContext())
        {
            (await context.Posts.AsNoTracking().Where(post => post.id == publicNote).Select(post => post.hashtags).SingleAsync())
                .Should().Equal("construction", "carpentry");
        }

        var recorder = new SqlRecorder();
        var solPage = Succeeded(await Feed(recorder).GetPageAsync(new FeedPageRequest(Sol, "tag:#Construction", Limit: 50)));
        solPage.Items.Select(card => card.Id).Should().Contain(publicNote).And.NotContain(draftNote);
        solPage.Items.Single(card => card.Id == publicNote).Hashtags.Should().Equal("construction", "carpentry");
        // Npgsql emits "@wanted <@ p.hashtags"; Postgres commutes it to "hashtags @> ..." for the GIN index (verified
        // with EXPLAIN). "= ANY(hashtags)" - what a plain Contains would emit - could not use that index at all.
        recorder.Commands.Should().Contain(sql => sql.Contains("<@ p.hashtags"), "the tag scope is the containment the GIN index serves")
            .And.NotContain(sql => sql.Contains("= ANY (p.hashtags)"));

        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Tam, "tag:construction", Limit: 50))).Items
            .Select(card => card.Id).Should().Contain(new[] { publicNote, draftNote }, "the draft's team may see its tagged note");
    }

    [PostgresFact]
    public async Task ATagThatNamesASector_AlsoServesThatSectorsUntaggedCards_ButNeverAPrivateProjects()
    {
        string updateId;
        await using (var context = NewContext())
        {
            updateId = Succeeded(await Updates(context).CreateAsync(new CreateProjectUpdateDto
            {
                ProjectId = BarnId,
                UserId = Bo,
                Title = "Roof trusses delivered",
                Content = "Forty trusses, stacked by the north wall.",
            })).Id;
        }

        var draftUntagged = await WriteNoteAsync(Fern, DraftId, "Untagged prototype notes.");
        var kitchenUntagged = await WriteNoteAsync(Sol, "project-kitchen", "Untagged kitchen news.");
        await RunCycleAsync();
        string updateCardId;
        await using (var context = NewContext())
            updateCardId = await context.Posts.Where(post => post.sourceId == updateId).Select(post => post.id).SingleAsync();

        var solSees = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, "tag:construction", Limit: 50))).Items;

        var card = solSees.Should().ContainSingle(item => item.Id == updateCardId,
            "an untagged update on a CONSTRUCTION project belongs to #construction").Subject;
        card.Hashtags.Should().BeEmpty();
        card.Reasons.Should().Contain(reason => reason.Kind == nameof(FeedReasonKind.InScope) && reason.Text == "In Construction");
        solSees.Select(item => item.Id).Should().NotContain(new[] { draftUntagged, kitchenUntagged },
            "the draft is private to its team, and the kitchen is not construction");
        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Tam, "tag:construction", Limit: 50))).Items
            .Select(item => item.Id).Should().Contain(draftUntagged, "the draft's team sees its sector cards");
    }

    [PostgresFact]
    public async Task AViewerWhoLikedCarpentryPosts_GetsAnotherCarpentryCard_WithTheTagReason_AboveNewerUntaggedNews()
    {
        for (var i = 0; i < 2; i++)
        {
            var liked = await WriteNoteAsync(Fern, BarnId, $"Mortise and tenon, day {i} #carpentry");
            await using var context = NewContext();
            Succeeded(await Posts(context).LikeAsync(liked, Ada));
        }

        var carpentry = await WriteNoteAsync(Sol, "project-kitchen", "Built the bread shelves myself #Carpentry");
        var untagged = await WriteNoteAsync(Tam, "project-kitchen-2", "The new oven arrived.");

        var items = (await PageAsync(Ada, limit: 50)).Items.ToList();

        var card = items.Single(item => item.Id == carpentry);
        card.Reasons.Should().Contain(reason => reason.Kind == nameof(FeedReasonKind.TagInterest)
            && reason.Text == "Because you're into #carpentry");
        card.IsDiscovery.Should().BeFalse("a tag Ada engages with is affinity, not discovery");
        items.FindIndex(item => item.Id == carpentry).Should().BeLessThan(items.FindIndex(item => item.Id == untagged));
        items.Single(item => item.Id == untagged).IsDiscovery.Should().BeTrue();
    }

    [PostgresFact]
    public async Task TheQualityPass_LetsApprovalsVouchForReplies_SoAFlameThreadStaysAtBaseline_AndRanksBelowAnEndorsedNote()
    {
        var endorsed = await WriteNoteAsync(Fern, BarnId, "Shared the barn plans with the village.");
        var flame = await WriteNoteAsync(Sol, "project-kitchen", "Ovens are overrated. Fight me.");

        await using (var context = NewContext())
        {
            var posts = Posts(context);
            Succeeded(await posts.LikeAsync(endorsed, Ada));
            Succeeded(await posts.LikeAsync(endorsed, Tam));
            Succeeded(await posts.BookmarkAsync(endorsed, Ada));
            for (var i = 0; i < 20; i++)
                Succeeded(await posts.AddCommentAsync(flame, new CreatePostCommentDto { AuthorId = i % 2 == 0 ? Bo : Tam, Content = $"No, you are wrong ({i})." }));
        }

        FeedQualityRun run;
        await using (var context = NewContext())
            run = await new FeedQualityPass(new FeedReadStore(context), new FeedEnvelopeStore(context), new FeedProjectorOptions(), _clock).RunAsync();
        run.Outcome.Should().Be(FeedProjectorOutcome.Completed);

        await using (var check = NewContext())
        {
            var scores = await check.Posts.AsNoTracking().Where(post => post.id == endorsed || post.id == flame)
                .ToDictionaryAsync(post => post.id, post => post.trendingScore);
            scores[flame].Should().Be((decimal)FeedQualityScore.Baseline, "twenty replies with no approval vouch for nothing");
            scores[endorsed].Should().BeGreaterThan(scores[flame]);
        }

        var items = (await PageAsync(Ada, limit: 50)).Items.Select(card => card.Id).ToList();
        items.IndexOf(endorsed).Should().BeLessThan(items.IndexOf(flame), "the older endorsed note outranks the newer flame thread");
    }

    [PostgresFact]
    public async Task OneAccountPraisingItsOwnPost_StaysAtBaseline_WhileFiveDistinctLikersLiftAnother()
    {
        const string gus = "user-gus";
        await using (var context = NewContext())
        {
            context.Users.Add(NewUser(gus));
            await context.SaveChangesAsync();
        }

        var endorsed = await WriteNoteAsync(gus, BarnId, "Shared the barn plans with the village.");
        var gamed = await WriteNoteAsync(Sol, "project-kitchen", "Best oven on the platform - share it everywhere!");
        await using (var context = NewContext())
        {
            var posts = Posts(context);
            for (var i = 0; i < 50; i++)
                Succeeded(await posts.ShareAsync(gamed, Sol, null));
            Succeeded(await posts.LikeAsync(gamed, Sol));
            Succeeded(await posts.BookmarkAsync(gamed, Sol));
            for (var i = 0; i < 10; i++)
                Succeeded(await posts.AddCommentAsync(gamed, new CreatePostCommentDto { AuthorId = Sol, Content = $"Bump {i}" }));
            foreach (var person in new[] { Ada, Bo, Fern, Sol, Tam })
                Succeeded(await posts.LikeAsync(endorsed, person));
        }

        await using (var context = NewContext())
            (await new FeedQualityPass(new FeedReadStore(context), new FeedEnvelopeStore(context), new FeedProjectorOptions(), _clock).RunAsync())
                .Outcome.Should().Be(FeedProjectorOutcome.Completed);

        await using (var check = NewContext())
        {
            var rows = await check.Posts.AsNoTracking().Where(post => post.id == endorsed || post.id == gamed)
                .ToDictionaryAsync(post => post.id);
            rows[gamed].sharesCount.Should().Be(50, "the raw counter really was inflated - the score must not read it");
            rows[gamed].trendingScore.Should().Be((decimal)FeedQualityScore.Baseline,
                "fifty self-shares, a self-like, a self-bookmark and ten self-replies are one account praising itself");
            rows[endorsed].trendingScore.Should().BeGreaterThan(rows[gamed].trendingScore);
        }

        var signedOut = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(null, Limit: 50))).Items.Select(card => card.Id).ToList();
        signedOut.IndexOf(endorsed).Should().BeLessThan(signedOut.IndexOf(gamed), "the older post five people liked outranks it");
    }

    [PostgresFact]
    public async Task ABusySubject_NeverBreaksTheCapsBeforeTheFeedIsExhausted_ThePageComesBackShortInstead()
    {
        var now = DateTime.UtcNow.AddMinutes(-1);
        var busy = Enumerable.Range(0, 200).Select(index => ("project-busy", $"user-w{index % 60}", now.AddMinutes(-index)));
        var older = Enumerable.Range(0, 20).Select(index => ($"project-quiet-{index}", $"user-q{index}", now.AddDays(-1).AddMinutes(-index)));
        await SeedNotesAsync(busy.Concat(older).ToList());

        var first = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Limit: 20)));

        first.Items.Should().HaveCount(FeedRanking.HomeSubjectCap,
            "the window is full of one subject and the feed is not exhausted: two cards, not twenty of the same project");
        first.Items.Should().OnlyContain(card => card.Project!.Id == "project-busy");
        first.NextCursor.Should().NotBeNull();
        var second = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Cursor: first.NextCursor, Limit: 20)));
        second.Items.Should().HaveCount(FeedRanking.HomeSubjectCap);
        second.Items.Select(card => card.Id).Should().NotIntersectWith(first.Items.Select(card => card.Id));
    }

    [PostgresFact]
    public async Task AWalkOverMoreCardsThanAWindowHolds_ServesEveryVisibleCardOnce_HoldsTheCaps_AndCrossesWindows()
    {
        var now = DateTime.UtcNow.AddMinutes(-1);
        // 240 cards over 12 projects and 18 authors, plus a burst of 12 from one busy project among the newest, so
        // the subject cap binds on the first pages; 5 notes on the draft project Sol may never see.
        var spread = Enumerable.Range(1, 240).Select(index => ($"project-s{index % 12}", $"user-a{index % 18}", now.AddMinutes(-index)));
        var burst = Enumerable.Range(1, 12).Select(index => ("project-burst", "user-burst", now.AddMinutes(-index - 0.5)));
        var visible = await SeedNotesAsync(spread.Concat(burst).ToList());
        var hidden = await SeedNotesAsync(Enumerable.Range(1, 5).Select(index => (DraftId, Fern, now.AddMinutes(-index * 7))).ToList());
        visible.Should().HaveCountGreaterThan(FeedService.MaxWindow + FeedService.WindowChunk);

        var pages = new List<FeedPageDto>();
        var windowTops = new HashSet<string?>();
        string? cursor = null;
        do
        {
            var page = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Cursor: cursor, Limit: 20)));
            pages.Add(page);
            cursor = page.NextCursor;
            if (cursor is not null && FeedCursor.TryDecode(cursor, out var decoded))
                windowTops.Add(decoded!.Top?.Id);
        }
        while (cursor is not null && pages.Count < 40);

        var served = pages.SelectMany(page => page.Items).Select(card => card.Id).ToList();
        served.Should().OnlyHaveUniqueItems();
        served.Should().BeEquivalentTo(visible, "every visible card is eventually served, and nothing else");
        served.Should().NotIntersectWith(hidden);
        windowTops.Count.Should().BeGreaterThan(2, "the scroll moved through several windows, not one");
        foreach (var page in pages)
        {
            page.Items.GroupBy(card => card.Project!.Id).Should().OnlyContain(subject => subject.Count() <= FeedRanking.HomeSubjectCap);
            page.Items.GroupBy(card => card.Author.Id).Should().OnlyContain(author => author.Count() <= FeedRanking.HomeAuthorCap);
        }

        pages[0].Items.Count(card => card.Project!.Id == "project-burst").Should().Be(
            FeedRanking.HomeSubjectCap, "ten of the twenty newest cards are the burst; the cap defers the rest");

        // A hand-made cursor carrying the most served fingerprints a cursor may hold, plus a whole-feed window.
        var random = new Random(31);
        var stuffed = new HashSet<ulong>(Enumerable.Range(0, FeedCursor.MaxServed).Select(_ => (ulong)random.NextInt64(1L << 48)));
        var forged = new FeedCursor(DateTime.UtcNow, "home", null, new FeedKey(now.AddYears(-1), "0"), stuffed, false).Encode();
        var after = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Cursor: forged, Limit: 20)));
        after.Items.Should().NotBeEmpty();
        FeedCursor.TryDecode(after.NextCursor, out var next).Should().BeTrue("the served set is cut back to the window, so the cursor stays decodable");
        next!.Served.Count.Should().BeLessThanOrEqualTo(FeedService.MaxWindow + FeedService.WindowChunk);
        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Cursor: after.NextCursor, Limit: 20))).Items
            .Select(card => card.Id).Should().NotIntersectWith(after.Items.Select(card => card.Id));
    }

    /// <summary>
    /// Writes person posts straight to Post, creating any missing author or (published) project; hundreds of
    /// PostService round trips would only slow these paging tests, whose subject is the read path.
    /// </summary>
    private async Task<List<string>> SeedNotesAsync(IReadOnlyList<(string ProjectId, string AuthorId, DateTime At)> notes)
    {
        await using var context = NewContext();
        var users = (await context.Users.Select(user => user.id).ToListAsync()).ToHashSet();
        var projects = (await context.Projects.Select(project => project.id).ToListAsync()).ToHashSet();
        var ids = new List<string>(notes.Count);
        foreach (var (projectId, authorId, at) in notes)
        {
            if (users.Add(authorId))
                context.Users.Add(NewUser(authorId));
            if (projects.Add(projectId))
                context.Projects.Add(NewProject(projectId, projectId, authorId, ProjectStatus.PUBLISHED, "MANUFACTURING"));

            var id = Guid.NewGuid().ToString();
            ids.Add(id);
            context.Posts.Add(new Post
            {
                id = id,
                authorId = authorId,
                projectId = projectId,
                type = PostType.POST,
                visibility = PostVisibility.PUBLIC,
                content = $"Note {ids.Count} on {projectId}.",
                createdAt = at,
                updatedAt = at,
            });
        }

        await context.SaveChangesAsync();
        return ids;
    }

    private async Task<FeedPageDto> PageAsync(string viewerId, int limit = 20)
        => Succeeded(await Feed().GetPageAsync(new FeedPageRequest(viewerId, Limit: limit)));

    /// <summary>Every card id the viewer's home scroll serves, page after page, until the cursor runs out.</summary>
    private async Task<List<string>> WalkAsync(string viewerId)
    {
        var ids = new List<string>();
        string? cursor = null;
        for (var pages = 0; pages < 30; pages++)
        {
            var page = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(viewerId, Cursor: cursor, Limit: 10)));
            ids.AddRange(page.Items.Select(card => card.Id));
            cursor = page.NextCursor;
            if (cursor is null)
                break;
        }

        return ids;
    }

    private FeedService Feed(SqlRecorder? recorder = null)
    {
        var options = recorder is null
            ? _options!
            : new DbContextOptionsBuilder<ArdaNovaDbContext>(_options!).AddInterceptors(recorder).Options;
        var context = new ArdaNovaDbContext(options);
        return new FeedService(
            new Repository<Post>(context),
            new Repository<Project>(context),
            new Repository<Guild>(context),
            new FeedSourceReader(context),
            new FeedReadStore(context),
            _clock);
    }

    private async Task<FeedProjectorRun> RunCycleAsync()
    {
        await using var context = NewContext();
        return await new FeedProjectorCycle(
            new FeedSourceReader(context),
            new FeedEnvelopeStore(context),
            new Repository<Post>(context),
            new FeedProjectorOptions(),
            _clock).RunAsync(sweep: false, new FeedProjectorWatermark());
    }

    /// <summary>No endpoint writes a project follow yet (only guild and user follows have services); this is the row one would.</summary>
    private async Task FollowProjectAsync(string userId, string projectId)
    {
        await using var context = NewContext();
        context.ProjectFollows.Add(new ProjectFollow
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            projectId = projectId,
            notifyUpdates = true,
            notifyMilestones = true,
            createdAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();
    }

    private async Task<string> WriteNoteAsync(string authorId, string projectId, string content)
    {
        await using var context = NewContext();
        return Succeeded(await Posts(context).CreateAsync(new CreatePostDto
        {
            AuthorId = authorId,
            ProjectId = projectId,
            Content = content,
        })).Id;
    }

    private static T Succeeded<T>(Result<T> result)
    {
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static PostService Posts(ArdaNovaDbContext context) => new(
        new Repository<Post>(context),
        new Repository<PostLike>(context),
        new Repository<PostComment>(context),
        new Repository<PostBookmark>(context),
        new Repository<PostShare>(context),
        new UnitOfWorkOverContext(context),
        Mapper);

    private static ProjectUpdateService Updates(ArdaNovaDbContext context)
        => new(new Repository<ProjectUpdate>(context), new UnitOfWorkOverContext(context), Mapper);

    private static ProjectCommentService Comments(ArdaNovaDbContext context)
        => new(new Repository<ProjectComment>(context), new Repository<User>(context), new UnitOfWorkOverContext(context), Mapper);

    private static OpportunityService Opportunities(ArdaNovaDbContext context) => new(
        new Repository<Opportunity>(context),
        new Repository<OpportunityApplication>(context),
        new Repository<OpportunityUpdate>(context),
        new Repository<OpportunityComment>(context),
        new Repository<User>(context),
        new Repository<Guild>(context),
        new Repository<Project>(context),
        new Repository<ProjectTask>(context),
        new Repository<ProjectMember>(context),
        new UnitOfWorkOverContext(context),
        Mapper);

    /// <summary>
    /// A month-old world: a published construction project (Bo on its team), a draft (Tam on its team), two
    /// published kitchens, and the slice of the taxonomy the canonical tag layer reads.
    /// </summary>
    private static async Task SeedAsync(ArdaNovaDbContext context)
    {
        foreach (var id in new[] { Ada, Bo, Fern, Sol, Tam })
            context.Users.Add(NewUser(id));

        context.Projects.Add(NewProject(BarnId, BarnTitle, Fern, ProjectStatus.PUBLISHED, "CONSTRUCTION"));
        context.Projects.Add(NewProject(DraftId, "Shed prototype", Fern, ProjectStatus.DRAFT, "CONSTRUCTION"));
        context.Projects.Add(NewProject("project-kitchen", "Village kitchen", Sol, ProjectStatus.PUBLISHED, "FOOD_BEVERAGE"));
        context.Projects.Add(NewProject("project-kitchen-2", "Second kitchen", Tam, ProjectStatus.PUBLISHED, "FOOD_BEVERAGE"));
        context.ProjectMembers.Add(Member("member-barn-bo", BarnId, Bo));
        context.ProjectMembers.Add(Member("member-draft-tam", DraftId, Tam));

        var monthAgo = DateTime.UtcNow.AddDays(-30);
        context.TaxonomyTerms.Add(Term("taxonomy_sector_construction", TaxonomyKind.SECTOR, "CONSTRUCTION", "Construction", null, monthAgo));
        context.TaxonomyTerms.Add(Term("taxonomy_sector_manufacturing", TaxonomyKind.SECTOR, "MANUFACTURING", "Manufacturing", null, monthAgo));
        context.TaxonomyTerms.Add(Term("taxonomy_sector_food_beverage", TaxonomyKind.SECTOR, "FOOD_BEVERAGE", "Food and drink", null, monthAgo));
        context.TaxonomyTerms.Add(Term("taxonomy_skill_carpentry", TaxonomyKind.SKILL, "CARPENTRY", "Carpentry", "taxonomy_sector_construction", monthAgo));
        await context.SaveChangesAsync();
    }

    private static TaxonomyTerm Term(string id, TaxonomyKind kind, string slug, string label, string? parentId, DateTime at) => new()
    {
        id = id,
        kind = kind,
        slug = slug,
        label = label,
        parentId = parentId,
        isActive = true,
        createdAt = at,
        updatedAt = at,
    };

    private static User NewUser(string id)
    {
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        return new User { id = id, email = $"{id}@example.test", name = id[5..], createdAt = monthAgo, updatedAt = monthAgo };
    }

    private static Project NewProject(string id, string title, string founder, ProjectStatus status, string categories)
    {
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        return new Project
        {
            id = id,
            title = title,
            slug = id,
            description = $"{title}, a community project.",
            problemStatement = "Something is missing.",
            solution = "Build it together.",
            categories = categories,
            status = status,
            createdById = founder,
            createdAt = monthAgo,
            updatedAt = monthAgo,
            publishedAt = status == ProjectStatus.DRAFT ? null : monthAgo,
        };
    }

    private static ProjectMember Member(string id, string projectId, string userId) => new()
    {
        id = id,
        projectId = projectId,
        userId = userId,
        role = ProjectRole.CONTRIBUTOR,
        joinedAt = DateTime.UtcNow.AddDays(-30),
    };

    private ArdaNovaDbContext NewContext() => new(_options!);

    /// <summary>Records the SQL EF sends, so a test can assert the shape Postgres plans from.</summary>
    private sealed class SqlRecorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            lock (Commands)
                Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
