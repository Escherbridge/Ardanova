namespace ArdaNova.Application.Tests.Flows;

using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using ArdaNova.Application.Common.Interfaces;
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
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;
using UnitOfWorkOverContext = ArdaNova.Infrastructure.UnitOfWork.UnitOfWork;

/// <summary>
/// SR-03 feed projector, end to end: real services write the source rows, the real cycle projects them, and
/// the real PostService reads them back - on a throwaway Postgres database, so the (sourceType, sourceId)
/// unique index, the (postId, userId) indexes and the advisory lock are the database's own. Only the clock
/// is faked. Needs <c>ARDANOVA_TEST_POSTGRES</c> (a server connection string); without it every test reports
/// SKIPPED via <see cref="PostgresFactAttribute"/>, the same gate as <see cref="SettlementNoEffectReopenFlowTests"/>.
/// </summary>
[Trait("Category", "Database")]
public sealed class FeedProjectorFlowTests : IAsyncLifetime
{
    private const string Founder = "user-founder";
    private const string Teammate = "user-teammate";
    private const string Stranger = "user-stranger";
    private const string Backer = "user-backer";
    private const string BackerName = "Bea Backerly";
    private const string PublishedProjectId = "project-published";
    private const string DraftProjectId = "project-draft";

    private static readonly IMapper Mapper = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<MappingProfile>();
        MappingSecurityPolicy.Apply(configuration);
    }).CreateMapper();

    private readonly MutableClock _clock = new(DateTimeOffset.UtcNow);
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    public async Task InitializeAsync()
    {
        // Only reached when PostgresFact did not skip, so the server is configured.
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_feed_" + Guid.NewGuid().ToString("N")[..12],
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
            // Each test builds its own throwaway database and its own NpgsqlDataSource/enum-mapped options on
            // purpose (one per test, run in parallel); EF's leak heuristic for "many service providers" does
            // not apply here, and the assembly's growing number of real-Postgres flow tests would otherwise
            // make this throw once the whole suite's cumulative count crosses twenty.
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
        await ApplyLiveFeedConstraintsAsync(context);
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
    public async Task AnUpdate_AnOpenedOpportunity_AndAReachedMilestone_EachBecomeOneEnvelope_AndARerunChangesNothing()
    {
        var updateId = await PostProjectUpdateAsync("Roof is on");
        string opportunityId;
        await using (var context = NewContext())
        {
            var draft = await Opportunities(context).CreateAsync(new CreateOpportunityDto
            {
                PosterId = Founder,
                Title = "Site carpenter",
                Description = "Frame the south wall.",
                Type = OpportunityType.PROJECT_ROLE,
                ExperienceLevel = ExperienceLevel.MID,
                ProjectId = PublishedProjectId,
                IsOpenForApplications = false,
            });
            opportunityId = Succeeded(draft).Id;
        }

        string milestoneId;
        await using (var context = NewContext())
        {
            milestoneId = Succeeded(await Milestones(context).CreateAsync(new CreateProjectMilestoneDto
            {
                ProjectId = PublishedProjectId,
                Title = "Foundation poured",
            })).Id;
            Succeeded(await Milestones(context).CompleteAsync(milestoneId));
        }

        (await RunCycleAsync()).Projected.Should().Be(2);
        (await EnvelopesAsync()).Should().NotContain(
            envelope => envelope.sourceType == FeedSourceType.OPPORTUNITY,
            "an opportunity is never in the feed while it is a draft");

        await using (var context = NewContext())
            Succeeded(await Opportunities(context).UpdateAsync(opportunityId, new UpdateOpportunityDto { Status = OpportunityStatus.OPEN }));
        (await RunCycleAsync()).Projected.Should().Be(1);

        var envelopes = await EnvelopesAsync();
        envelopes.Should().HaveCount(3);
        var update = envelopes.Should().ContainSingle(envelope => envelope.sourceId == updateId).Subject;
        update.type.Should().Be(PostType.PROJECT_UPDATE);
        update.sourceType.Should().Be(FeedSourceType.PROJECT_UPDATE);
        update.authorId.Should().Be(Founder);
        update.title.Should().Be("Roof is on");
        update.content.Should().BeEmpty("the update's own words are read live from the update, not copied");
        var milestone = envelopes.Should().ContainSingle(envelope => envelope.sourceId == milestoneId).Subject;
        milestone.type.Should().Be(PostType.MILESTONE);
        milestone.projectId.Should().Be(PublishedProjectId);
        var opened = envelopes.Should().ContainSingle(envelope => envelope.sourceId == opportunityId).Subject;
        opened.type.Should().Be(PostType.OPPORTUNITY_OPENED);
        opened.opportunityId.Should().Be(opportunityId);
        opened.projectId.Should().Be(PublishedProjectId, "the opportunity's project gates who may see it");
        envelopes.Should().OnlyContain(envelope => envelope.visibility == PostVisibility.PUBLIC);

        var rerun = await RunCycleAsync();
        rerun.Projected.Should().Be(0);
        rerun.AlreadyProjected.Should().Be(3);
        (await EnvelopesAsync()).Select(envelope => envelope.id).Should().BeEquivalentTo(envelopes.Select(envelope => envelope.id));
    }

    [PostgresFact]
    public async Task DeletingAnUpdate_LeavesItsCardUntilTheSweep_WhichTakesTheCardAndItsLikes_AndNothingAPersonWrote()
    {
        var updateId = await PostProjectUpdateAsync("Kiln delivered");
        var noteId = await WriteFieldNoteAsync("A note the founder wrote herself.");
        await RunCycleAsync();
        var envelope = (await EnvelopesAsync()).Single(row => row.sourceId == updateId);
        await using (var context = NewContext())
            Succeeded(await Posts(context).LikeAsync(envelope.id, Stranger));
        await using (var context = NewContext())
            Succeeded(await Posts(context).LikeAsync(noteId, Stranger));

        await using (var context = NewContext())
            Succeeded(await Updates(context).DeleteAsync(updateId));
        (await RunCycleAsync()).Removed.Should().Be(0, "a deleted row leaves nothing for the look-back window to see");

        (await RunCycleAsync(sweep: true)).Removed.Should().Be(1);

        await using var check = NewContext();
        (await check.Posts.AnyAsync(post => post.id == envelope.id)).Should().BeFalse();
        (await check.PostLikes.AnyAsync(like => like.postId == envelope.id)).Should().BeFalse();
        (await check.Posts.SingleAsync(post => post.id == noteId)).likesCount.Should().Be(1);
        (await check.PostLikes.CountAsync(like => like.postId == noteId)).Should().Be(1);
    }

    [PostgresFact]
    public async Task ACardWhoseTargetWasDeleted_IsSwept_EvenLongAfterTheSweepWindow()
    {
        const string doomedProjectId = "project-doomed";
        await using (var context = NewContext())
        {
            context.Projects.Add(NewProject(doomedProjectId, ProjectStatus.PUBLISHED, DateTime.UtcNow));
            await context.SaveChangesAsync();
        }
        await RunCycleAsync();
        (await EnvelopesAsync()).Should().ContainSingle(row => row.sourceId == doomedProjectId);

        _clock.Advance(TimeSpan.FromHours(72));
        await using (var context = NewContext())
            Succeeded(await Projects(context).DeleteAsync(doomedProjectId));
        (await EnvelopesAsync()).Single(row => row.sourceId == doomedProjectId).projectId
            .Should().BeNull("Post.projectId is ON DELETE SET NULL");

        (await RunCycleAsync(sweep: true)).Removed.Should().Be(1);
        (await EnvelopesAsync()).Should().NotContain(row => row.sourceId == doomedProjectId);
    }

    [PostgresFact]
    public async Task ACommentDeletedDaysAfterItsCardWasProjected_IsRemovedByTheFullSweep_WithItsLikes_AndNothingAPersonWrote()
    {
        var commentId = await CommentAsync(Teammate, CommentTargetType.PROJECT, PublishedProjectId);
        var noteId = await WriteFieldNoteAsync("A note the founder wrote herself, long before the comment went stale.");
        await RunCycleAsync();
        var card = (await EnvelopesAsync()).Single(row => row.sourceId == commentId);
        await using (var context = NewContext())
            Succeeded(await Posts(context).LikeAsync(card.id, Stranger));
        await using (var context = NewContext())
            Succeeded(await Posts(context).LikeAsync(noteId, Stranger));

        // Advance the projector's clock, not the wall clock: the comment's own createdAt (what Post.createdAt
        // mirrors) stays real-time, so it now reads as more than 48 h old to a time-filtered sweep.
        _clock.Advance(TimeSpan.FromDays(3));
        await using (var context = NewContext())
            Succeeded(await Comments(context).DeleteAsync(commentId));

        (await RunCycleAsync()).Removed.Should().Be(
            0, "a deleted source leaves nothing for the look-back window to see");

        (await RunCycleAsync(sweep: true)).Removed.Should().Be(
            1, "the full sweep reconciles every envelope against its source, not only the ones a time filter would admit");

        await using var check = NewContext();
        (await check.Posts.AnyAsync(post => post.id == card.id)).Should().BeFalse();
        (await check.PostLikes.AnyAsync(like => like.postId == card.id)).Should().BeFalse();
        (await check.Posts.SingleAsync(post => post.id == noteId)).likesCount.Should().Be(1);
        (await check.PostLikes.CountAsync(like => like.postId == noteId)).Should().Be(1);
    }

    [PostgresFact]
    public async Task AnOpportunityMovedBackToDraftDaysAfterItsCardWasProjected_IsRemovedByTheFullSweep()
    {
        string opportunityId;
        await using (var context = NewContext())
        {
            var created = await Opportunities(context).CreateAsync(new CreateOpportunityDto
            {
                PosterId = Founder,
                Title = "Site carpenter",
                Description = "Frame the south wall.",
                Type = OpportunityType.PROJECT_ROLE,
                ExperienceLevel = ExperienceLevel.MID,
                ProjectId = PublishedProjectId,
                IsOpenForApplications = false,
            });
            opportunityId = Succeeded(created).Id;
            Succeeded(await Opportunities(context).UpdateAsync(
                opportunityId, new UpdateOpportunityDto { Status = OpportunityStatus.OPEN }));
        }
        await RunCycleAsync();
        (await EnvelopesAsync()).Should().ContainSingle(row => row.sourceId == opportunityId);

        _clock.Advance(TimeSpan.FromDays(3));
        await using (var context = NewContext())
            Succeeded(await Opportunities(context).UpdateAsync(
                opportunityId, new UpdateOpportunityDto { Status = OpportunityStatus.DRAFT }));

        (await RunCycleAsync()).Removed.Should().Be(
            0, "the status flip is outside both the look-back and the old time-filtered sweep");

        (await RunCycleAsync(sweep: true)).Removed.Should().Be(1);
        (await EnvelopesAsync()).Should().NotContain(row => row.sourceId == opportunityId);
    }

    [PostgresFact]
    public async Task AnEventMadeInviteOnlyDaysAfterItsCardWasProjected_IsRemovedByTheFullSweep()
    {
        var eventId = await ScheduleEventAsync("Open day", EventVisibility.PUBLIC);
        await RunCycleAsync();
        (await EnvelopesAsync()).Should().ContainSingle(row => row.sourceId == eventId);

        _clock.Advance(TimeSpan.FromDays(3));
        await using (var context = NewContext())
            Succeeded(await Events(context).UpdateAsync(eventId, new UpdateEventDto { Visibility = EventVisibility.INVITE_ONLY }));

        (await RunCycleAsync()).Removed.Should().Be(
            0, "the visibility flip is outside both the look-back and the old time-filtered sweep");

        (await RunCycleAsync(sweep: true)).Removed.Should().Be(1);
        (await EnvelopesAsync()).Should().NotContain(row => row.sourceId == eventId);
    }

    [PostgresFact]
    public async Task MoreStaleEnvelopesThanOneSweepBatch_AreAllReconciledInOnePass_AndAPersonsPostIsUntouched()
    {
        var options = new FeedProjectorOptions { SweepBatchSize = 2 };
        var commentIds = new List<string>();
        for (var i = 0; i < 5; i++)
            commentIds.Add(await CommentAsync(Teammate, CommentTargetType.PROJECT, PublishedProjectId));
        var noteId = await WriteFieldNoteAsync("Mixed into the same id range; must survive every batch.");

        await RunCycleAsync(options: options);
        (await EnvelopesAsync()).Count(row => commentIds.Contains(row.sourceId)).Should().Be(5);

        _clock.Advance(TimeSpan.FromDays(3));
        await using (var context = NewContext())
        {
            var comments = Comments(context);
            foreach (var commentId in commentIds)
                Succeeded(await comments.DeleteAsync(commentId));
        }

        (await RunCycleAsync(options: options)).Removed.Should().Be(
            0, "the deletions are outside the look-back window");

        var swept = await RunCycleAsync(sweep: true, options: options);
        swept.Removed.Should().Be(5, "every stale card must be reconciled in one sweep, however many batches of 2 it takes");

        (await EnvelopesAsync()).Should().NotContain(row => commentIds.Contains(row.sourceId));
        await using var check = NewContext();
        (await check.Posts.AnyAsync(post => post.id == noteId)).Should().BeTrue(
            "a person's own post must never be touched by the sweep, whichever batch its id falls into");
    }

    [PostgresFact]
    public async Task AStranger_CannotReachADraftProjectsCards_OrBacklogTalk_ButTheTeamCan()
    {
        string draftMilestoneId;
        await using (var context = NewContext())
        {
            draftMilestoneId = Succeeded(await Milestones(context).CreateAsync(new CreateProjectMilestoneDto
            {
                ProjectId = DraftProjectId,
                Title = "Secret prototype",
            })).Id;
            Succeeded(await Milestones(context).CompleteAsync(draftMilestoneId));
        }
        var backlogTalkId = await CommentAsync(Teammate, CommentTargetType.PBI, "pbi-wiring");
        var projectTalkId = await CommentAsync(Teammate, CommentTargetType.PROJECT, PublishedProjectId);
        await RunCycleAsync();

        var envelopes = await EnvelopesAsync();
        var draftCard = envelopes.Single(row => row.sourceId == draftMilestoneId);
        var backlogCard = envelopes.Single(row => row.sourceId == backlogTalkId);
        var projectCard = envelopes.Single(row => row.sourceId == projectTalkId);
        backlogCard.visibility.Should().Be(PostVisibility.PROJECT_MEMBERS);

        (await ReadAsAsync(draftCard.id, Stranger)).Type.Should().Be(ResultType.NotFound);
        (await ReadAsAsync(draftCard.id, null)).Type.Should().Be(ResultType.NotFound);
        (await ReadAsAsync(draftCard.id, Teammate)).IsSuccess.Should().BeTrue();
        (await ReadAsAsync(draftCard.id, Founder)).IsSuccess.Should().BeTrue();
        await using (var context = NewContext())
            (await Posts(context).LikeAsync(draftCard.id, Stranger)).Type.Should().Be(ResultType.NotFound);

        (await ReadAsAsync(backlogCard.id, Stranger)).Type.Should().Be(ResultType.NotFound);
        (await ReadAsAsync(backlogCard.id, Teammate)).IsSuccess.Should().BeTrue();
        (await ReadAsAsync(projectCard.id, Stranger)).IsSuccess.Should().BeTrue();

        await using (var context = NewContext())
        {
            var reply = await Posts(context).AddCommentAsync(projectCard.id, new CreatePostCommentDto
            {
                AuthorId = Stranger,
                Content = "A second thread",
            });
            reply.Type.Should().Be(ResultType.ValidationError, "replies go to the native thread the card links to");
        }
        JsonDocument.Parse(projectCard.metadata!).RootElement.GetProperty("thread").GetProperty("commentId").GetString()
            .Should().Be(projectTalkId);
    }

    [PostgresFact]
    public async Task FieldNotes_Profiles_AndTheLegacyFeed_ShowOnlyWhatPeopleWrote_AndACardCannotBeEditedByItsSubject()
    {
        var noteId = await WriteFieldNoteAsync("Poured the slab today.");
        await PostProjectUpdateAsync("Slab poured");
        await RunCycleAsync();
        var card = (await EnvelopesAsync()).Single(row => row.sourceType == FeedSourceType.PROJECT_UPDATE);
        card.authorId.Should().Be(Founder, "the card is attributed to the founder, so every by-author read would see it");

        await using var context = NewContext();
        var posts = Posts(context);
        Succeeded(await posts.GetProjectFieldNotesAsync(PublishedProjectId, Stranger, 1, 20)).Items
            .Select(post => post.Id).Should().Equal(noteId);
        Succeeded(await posts.GetByAuthorForViewerAsync(Founder, Stranger)).Select(post => post.Id).Should().Equal(noteId);
        Succeeded(await posts.GetByUserIdAsync(Founder)).Select(post => post.Id).Should().Equal(noteId);
        Succeeded(await posts.GetFeedAsync(1, 20)).Items.Select(post => post.Id).Should().Equal(noteId);

        (await posts.UpdateAsync(card.id, Founder, new UpdatePostDto { Visibility = PostVisibility.PRIVATE }))
            .Type.Should().Be(ResultType.Forbidden);
        (await posts.DeleteAsync(card.id, Founder)).Type.Should().Be(ResultType.Forbidden);
    }

    [PostgresFact]
    public async Task FundingCrossingHalf_EmitsExactlyOneEnvelope_WithThePercentAndNoAmountOrBacker()
    {
        const string configId = "config-published";
        await using (var context = NewContext())
        {
            context.ProjectTokenConfigs.Add(new ProjectTokenConfig
            {
                id = configId,
                projectId = PublishedProjectId,
                assetName = "Solar barn units",
                unitName = "BARN",
                totalSupply = 1000,
                fundingGoal = 1000,
                fundingRaised = 0,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }
        (await RunCycleAsync()).Projected.Should().Be(0, "nothing has been raised yet");

        // The settlement path is the node's (external); what the projector reads is the row it leaves.
        await using (var context = NewContext())
        {
            context.ProjectInvestments.Add(new ProjectInvestment
            {
                id = "investment-1",
                projectTokenConfigId = configId,
                userId = Backer,
                usdAmount = 612.5,
                tokenAmount = 61,
                investedAt = DateTime.UtcNow,
                showBackerName = true,
            });
            var config = await context.ProjectTokenConfigs.SingleAsync(row => row.id == configId);
            config.fundingRaised = 612.5;
            config.updatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        (await RunCycleAsync()).Projected.Should().Be(1);
        (await RunCycleAsync()).Projected.Should().Be(0);

        var card = (await EnvelopesAsync()).Should().ContainSingle(row => row.type == PostType.FUNDING_PROGRESS).Subject;
        card.sourceId.Should().Be($"{configId}:50");
        card.visibility.Should().Be(PostVisibility.PUBLIC);
        card.authorId.Should().Be(Founder, "a backer is never the face of a funding card");
        JsonDocument.Parse(card.metadata!).RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal("percent");
        var everythingShown = $"{card.title} {card.content} {card.metadata}";
        everythingShown.Should().NotContain("612").And.NotContain("1000").And.NotContain(BackerName).And.NotContain(Backer);
    }

    [PostgresFact]
    public async Task AnInviteOnlyEvent_IsNeverProjected_AndAnEventMadeInviteOnlyLosesItsCardOnTheNextCycle()
    {
        await ScheduleEventAsync("Founders' dinner", EventVisibility.INVITE_ONLY);
        var openDayId = await ScheduleEventAsync("Open day", EventVisibility.PUBLIC);
        (await RunCycleAsync()).Projected.Should().Be(1);
        var card = (await EnvelopesAsync()).Should().ContainSingle(row => row.sourceType == FeedSourceType.EVENT).Subject;
        card.sourceId.Should().Be(openDayId);
        card.eventId.Should().Be(openDayId);

        await using (var context = NewContext())
            Succeeded(await Events(context).UpdateAsync(openDayId, new UpdateEventDto { Visibility = EventVisibility.INVITE_ONLY }));

        (await RunCycleAsync()).Removed.Should().Be(1, "the change bumped the event inside the window; no sweep needed");
        (await EnvelopesAsync()).Should().NotContain(row => row.sourceType == FeedSourceType.EVENT);
    }

    [PostgresTheory]
    [InlineData("like")]
    [InlineData("bookmark")]
    public async Task ARacingDoubleTap_Succeeds_AndCountsOnce(string engagement)
    {
        var noteId = await WriteFieldNoteAsync("Worth a like.");

        // The second tap's check has already passed when the first tap commits: only the unique index can see it.
        await using var context = NewContext();
        var racing = new InterleavingUnitOfWork(new UnitOfWorkOverContext(context), async () =>
        {
            await using var other = NewContext();
            await EngageAsync(Posts(other), engagement, noteId);
        });
        var secondTap = await EngageAsync(Posts(context, racing), engagement, noteId);

        secondTap.Should().BeTrue();
        racing.Raced.Should().BeTrue("the interleaving must actually have happened for this test to mean anything");
        await using var check = NewContext();
        if (engagement == "like")
        {
            (await check.PostLikes.CountAsync(like => like.postId == noteId && like.userId == Stranger)).Should().Be(1);
            (await check.Posts.SingleAsync(post => post.id == noteId)).likesCount.Should().Be(1);
        }
        else
        {
            (await check.PostBookmarks.CountAsync(row => row.postId == noteId && row.userId == Stranger)).Should().Be(1);
        }
    }

    [PostgresFact]
    public async Task OnlyOneRunnerProjectsAtATime_AndAnInsertThatLosesTheRace_IsAlreadyProjected()
    {
        var updateId = await PostProjectUpdateAsync("Walls up");
        await using (var holder = NewContext())
        {
            await using var heldLock = await new FeedEnvelopeStore(holder).TryAcquireRunnerLockAsync();
            heldLock.Should().NotBeNull();

            (await RunCycleAsync()).Outcome.Should().Be(FeedProjectorOutcome.RunnerBusy);
            (await EnvelopesAsync()).Should().BeEmpty();
        }

        (await RunCycleAsync()).Projected.Should().Be(1, "the lock is released with its holder");

        await using var context = NewContext();
        var source = (await new FeedSourceReader(context).ReadByIdsAsync(FeedSourceType.PROJECT_UPDATE, [updateId])).Single();
        var outcome = await new FeedEnvelopeStore(context).InsertAsync(FeedProjectionRules.Enter(source)!, DateTime.UtcNow);
        outcome.Should().Be(FeedInsertOutcome.AlreadyProjected);
        (await EnvelopesAsync()).Should().ContainSingle();
    }

    [PostgresFact]
    public async Task Backfill_ProjectsWhatTheWindowNeverSaw_AndItsDryRunWritesNothing()
    {
        (await RunCycleAsync()).Projected.Should().Be(0, "the seeded world is a month old, far outside the look-back");

        FeedProjectorRun preview;
        await using (var context = NewContext())
            preview = await Cycle(context).BackfillAsync(dryRun: true);
        var expected = new[]
        {
            new FeedSourceKey(FeedSourceType.PROJECT, PublishedProjectId),
            new FeedSourceKey(FeedSourceType.PROJECT_MEMBER, "member-published"),
            new FeedSourceKey(FeedSourceType.PROJECT_MEMBER, "member-draft"),
        };
        preview.DryRun.Should().BeTrue();
        preview.PendingEnvelopes.Select(envelope => envelope.Key).Should().BeEquivalentTo(expected);
        (await EnvelopesAsync()).Should().BeEmpty();

        FeedProjectorRun applied;
        await using (var context = NewContext())
            applied = await Cycle(context).BackfillAsync(dryRun: false);
        applied.Projected.Should().Be(3);
        (await EnvelopesAsync()).Select(row => new FeedSourceKey(row.sourceType!.Value, row.sourceId!))
            .Should().BeEquivalentTo(expected);
    }

    [PostgresTheory]
    [InlineData("like")]
    [InlineData("bookmark")]
    public async Task ARacingDoubleUntap_Succeeds_AndLeavesNothingBehind(string engagement)
    {
        var noteId = await WriteFieldNoteAsync("Liked, then un-liked twice.");
        await using (var context = NewContext())
            (await EngageAsync(Posts(context), engagement, noteId)).Should().BeTrue();

        // The second untap has already found the row when the first one deletes it: its DELETE hits zero rows.
        await using var tapping = NewContext();
        var racing = new InterleavingUnitOfWork(new UnitOfWorkOverContext(tapping), async () =>
        {
            await using var other = NewContext();
            (await DisengageAsync(Posts(other), engagement, noteId)).Should().BeTrue();
        });
        var secondUntap = await DisengageAsync(Posts(tapping, racing), engagement, noteId);

        secondUntap.Should().BeTrue("the row is already gone, which is the state the second tap asked for");
        racing.Raced.Should().BeTrue("the interleaving must actually have happened for this test to mean anything");
        await using var check = NewContext();
        (await check.PostLikes.CountAsync(like => like.postId == noteId)).Should().Be(0);
        (await check.PostBookmarks.CountAsync(row => row.postId == noteId)).Should().Be(0);
        (await check.Posts.SingleAsync(post => post.id == noteId)).likesCount.Should().Be(0);
    }

    [PostgresFact]
    public async Task AnAuthor_CanStillEditAndDeleteTheirOwnNote_WhileTheCardNamingThemStaysRefused()
    {
        var noteId = await WriteFieldNoteAsync("First draft of the note.");
        await PostProjectUpdateAsync("Beams delivered");
        await RunCycleAsync();
        var card = (await EnvelopesAsync()).Single(row => row.sourceType == FeedSourceType.PROJECT_UPDATE);
        await using (var context = NewContext())
            Succeeded(await Posts(context).LikeAsync(noteId, Stranger));

        await using (var context = NewContext())
        {
            var edited = Succeeded(await Posts(context).UpdateAsync(noteId, Founder, new UpdatePostDto { Content = "Second draft." }));
            edited.Content.Should().Be("Second draft.");
            (await Posts(context).DeleteAsync(card.id, Founder)).Type.Should().Be(ResultType.Forbidden);
        }

        await using (var context = NewContext())
            Succeeded(await Posts(context).DeleteAsync(noteId, Founder)).Should().BeTrue();

        await using var check = NewContext();
        (await check.Posts.AnyAsync(post => post.id == noteId)).Should().BeFalse("the author deleted her own note");
        (await check.PostLikes.AnyAsync(like => like.postId == noteId)).Should().BeFalse();
        (await check.Posts.AnyAsync(post => post.id == card.id)).Should().BeTrue("the card she is named on is not hers to delete");
    }

    [PostgresFact]
    public async Task TheEnvelopeStore_StaysUsable_AfterAnInsertThatCollidesOnTheUniqueIndex()
    {
        var projectedId = await PostProjectUpdateAsync("Already on the feed");
        await RunCycleAsync();
        var freshId = await PostProjectUpdateAsync("Not on the feed yet");

        // One context for both inserts, as one cycle's store has: the collision must not poison the second.
        await using var context = NewContext();
        var reader = new FeedSourceReader(context);
        var store = new FeedEnvelopeStore(context);
        var projected = (await reader.ReadByIdsAsync(FeedSourceType.PROJECT_UPDATE, [projectedId])).Single();
        var fresh = (await reader.ReadByIdsAsync(FeedSourceType.PROJECT_UPDATE, [freshId])).Single();

        (await store.InsertAsync(FeedProjectionRules.Enter(projected)!, DateTime.UtcNow)).Should().Be(FeedInsertOutcome.AlreadyProjected);
        (await store.InsertAsync(FeedProjectionRules.Enter(fresh)!, DateTime.UtcNow)).Should().Be(FeedInsertOutcome.Inserted);

        (await EnvelopesAsync()).Select(row => row.sourceId).Should().BeEquivalentTo([projectedId, freshId]);
    }

    [PostgresFact]
    public async Task AnEventMovedToADraftProject_HasItsCardRecreatedThere_SoAStrangerCanNoLongerReachIt()
    {
        var eventId = await ScheduleEventAsync("Open day", EventVisibility.PUBLIC);
        await RunCycleAsync();
        var before = (await EnvelopesAsync()).Single(row => row.sourceId == eventId);
        before.projectId.Should().Be(PublishedProjectId);
        (await ReadAsAsync(before.id, Stranger)).IsSuccess.Should().BeTrue();

        // No service moves an event today; any writer that does (an operator repair, a later "move" feature)
        // leaves exactly this row behind.
        await using (var context = NewContext())
        {
            await context.Events.Where(row => row.id == eventId).ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.projectId, DraftProjectId)
                .SetProperty(row => row.updatedAt, DateTime.UtcNow));
        }

        var run = await RunCycleAsync();
        run.Removed.Should().Be(1, "the card still pointing at the published project is wrong about who may see it");
        run.Projected.Should().Be(1);

        var after = (await EnvelopesAsync()).Single(row => row.sourceId == eventId);
        after.projectId.Should().Be(DraftProjectId);
        after.id.Should().NotBe(before.id);
        (await ReadAsAsync(after.id, Stranger)).Type.Should().Be(ResultType.NotFound, "a draft project's cards are its team's");
        (await ReadAsAsync(after.id, Teammate)).IsSuccess.Should().BeTrue();
        (await RunCycleAsync()).Projected.Should().Be(0, "once the targets match, the card is left alone");
    }

    [PostgresFact]
    public async Task FinishedWork_IsCreditedToWhoeverDidIt_OrToTheFounderWhenNobodyWasAssigned()
    {
        string assignedTaskId;
        string unassignedTaskId;
        await using (var context = NewContext())
        {
            var tasks = Tasks(context);
            assignedTaskId = Succeeded(await tasks.CreateAsync(new CreateTaskDto
            {
                ProjectId = PublishedProjectId,
                Title = "Hang the barn doors",
                AssignedToId = Teammate,
            })).Id;
            unassignedTaskId = Succeeded(await tasks.CreateAsync(new CreateTaskDto
            {
                ProjectId = PublishedProjectId,
                Title = "Sweep the floor",
            })).Id;
            Succeeded(await tasks.UpdateStatusAsync(assignedTaskId, TaskStatus.COMPLETED));
            Succeeded(await tasks.UpdateStatusAsync(unassignedTaskId, TaskStatus.COMPLETED));
        }

        string assignedMilestoneId;
        string unassignedMilestoneId;
        await using (var context = NewContext())
        {
            var milestones = Milestones(context);
            assignedMilestoneId = Succeeded(await milestones.CreateAsync(new CreateProjectMilestoneDto
            {
                ProjectId = PublishedProjectId,
                Title = "Roof raised",
            })).Id;
            unassignedMilestoneId = Succeeded(await milestones.CreateAsync(new CreateProjectMilestoneDto
            {
                ProjectId = PublishedProjectId,
                Title = "Site cleared",
            })).Id;
        }

        // No milestone endpoint sets an assignee yet; the column is what the reader credits.
        await using (var context = NewContext())
        {
            await context.ProjectMilestones.Where(row => row.id == assignedMilestoneId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.assigneeId, Teammate));
        }

        await using (var context = NewContext())
        {
            Succeeded(await Milestones(context).CompleteAsync(assignedMilestoneId));
            Succeeded(await Milestones(context).CompleteAsync(unassignedMilestoneId));
        }

        (await RunCycleAsync()).Projected.Should().Be(4);
        var authors = (await EnvelopesAsync()).ToDictionary(row => row.sourceId!, row => row.authorId);
        authors[assignedTaskId].Should().Be(Teammate, "the card credits the person who did the work");
        authors[unassignedTaskId].Should().Be(Founder);
        authors[assignedMilestoneId].Should().Be(Teammate);
        authors[unassignedMilestoneId].Should().Be(Founder);
    }

    [PostgresFact]
    public async Task ReleasingTheRunnerLock_OnAConnectionTheServerAlreadyDropped_DoesNotMaskTheCancellation()
    {
        await using var holder = NewContext();
        var shutdown = async () =>
        {
            await using var heldLock = await new FeedEnvelopeStore(holder).TryAcquireRunnerLockAsync();
            heldLock.Should().NotBeNull();
            var backend = await holder.Database.SqlQuery<int>($"SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
            await TerminateBackendAsync(backend);

            // What a host shutdown does mid-cycle: the cancellation is the exception that must surface.
            throw new OperationCanceledException("host is stopping");
        };

        await shutdown.Should().ThrowAsync<OperationCanceledException>();

        await using var next = NewContext();
        await using var reacquired = await new FeedEnvelopeStore(next).TryAcquireRunnerLockAsync();
        reacquired.Should().NotBeNull("the server freed the session lock when it dropped the connection");
    }

    [PostgresFact]
    public async Task ARunnerThatStalled_CatchesUpOnRowsCommittedDuringTheStall_InsteadOfSkippingThem()
    {
        var watermark = new FeedProjectorWatermark();
        (await RunCycleAsync(watermark: watermark)).Outcome.Should().Be(FeedProjectorOutcome.Completed);
        var updateId = await PostProjectUpdateAsync("Committed while the runner was stalled");

        // Thirty minutes with no completed cycle: the database was unreachable, the lock was held, the host paused.
        _clock.Advance(TimeSpan.FromMinutes(30));
        (await RunCycleAsync()).Projected.Should().Be(
            0, "a runner with no memory of its last read only looks back ten minutes - the old fixed window");

        var recovered = await RunCycleAsync(watermark: watermark);

        recovered.Projected.Should().Be(1, "the window reaches back to the last successful read, not ten minutes");
        (await EnvelopesAsync()).Should().ContainSingle(row => row.sourceId == updateId);
    }

    [PostgresFact]
    public async Task ASourceKindWhoseReadFails_DoesNotStopTheOtherKinds_IsNeverSwept_AndIsCaughtUpWhenItReadsAgain()
    {
        var watermark = new FeedProjectorWatermark();
        var projectedCommentId = await CommentAsync(Teammate, CommentTargetType.PROJECT, PublishedProjectId);
        (await RunCycleAsync(watermark: watermark)).Projected.Should().Be(1);
        var waitingCommentId = await CommentAsync(Teammate, CommentTargetType.PROJECT, PublishedProjectId);

        // A real read failure on one source table (schema drift): every query for comments now errors with 42703.
        await RenameColumnAsync("ProjectComment", "targetType", "targetType_unreadable");
        var updateId = await PostProjectUpdateAsync("Projected while comments cannot be read");
        _clock.Advance(TimeSpan.FromMinutes(30));

        var degraded = await RunCycleAsync(sweep: true, watermark: watermark);

        degraded.SourceReadsFailed.Should().Be(2, "the window's read and the sweep's read of comments both fail");
        degraded.Projected.Should().Be(1, "every other kind still projects");
        degraded.Removed.Should().Be(0, "an unreadable source is not a deleted one");
        var during = (await EnvelopesAsync()).Select(row => row.sourceId).ToList();
        during.Should().Contain(new[] { updateId, projectedCommentId }).And.NotContain(waitingCommentId);

        await RenameColumnAsync("ProjectComment", "targetType_unreadable", "targetType");
        _clock.Advance(TimeSpan.FromMinutes(30));

        var recovered = await RunCycleAsync(watermark: watermark);

        recovered.SourceReadsFailed.Should().Be(0);
        recovered.Projected.Should().Be(
            1, "comments are read from their own last good read an hour ago, not from where the other kinds got to");
        (await EnvelopesAsync()).Should().ContainSingle(row => row.sourceId == waitingCommentId);
    }

    [PostgresFact]
    public async Task OneCardTheDatabaseRefuses_IsCountedAndSkipped_WhileTheRestOfThePassIsWritten()
    {
        await using (var context = NewContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Post" ADD CONSTRAINT "Post_test_poison_title" CHECK ("title" IS DISTINCT FROM 'Poisoned update')""");
        }

        var poisonedId = await PostProjectUpdateAsync("Poisoned update");
        var fineId = await PostProjectUpdateAsync("Fine update");

        var run = await RunCycleAsync();

        run.CardsFailed.Should().Be(1, "the CHECK violation (23514) is neither 'already projected' nor 'target gone'");
        run.Projected.Should().Be(1);
        var sourceIds = (await EnvelopesAsync()).Select(row => row.sourceId).ToList();
        sourceIds.Should().Contain(fineId).And.NotContain(poisonedId);
    }

    [PostgresFact]
    public async Task TheCyclesStoredCardLookup_IsAnEqualityOnTheKind_SoPostgresCanSeekTheSourceIndex()
    {
        await PostProjectUpdateAsync("Gives the cycle a key to look up");
        await CommentAsync(Teammate, CommentTargetType.PROJECT, PublishedProjectId);
        var recorder = new SqlRecorder();
        var options = new DbContextOptionsBuilder<ArdaNovaDbContext>(_options!).AddInterceptors(recorder).Options;

        await using (var context = new ArdaNovaDbContext(options))
            (await Cycle(context).RunAsync(sweep: false, new FeedProjectorWatermark())).Projected.Should().Be(2);

        var lookups = recorder.Commands
            .Where(sql => sql.Contains("FROM \"Post\"") && Regex.IsMatch(sql, "\"sourceId\" (= ANY|IN \\()"))
            .ToList();
        lookups.Should().HaveCount(2, "one lookup per source kind in the window (an update and a comment)");
        lookups.Should().OnlyContain(
            sql => sql.Contains("\"sourceType\" = "),
            "'sourceType IS NOT NULL AND sourceId = ANY(...)' cannot seek the (sourceType, sourceId) unique index");
    }

    /// <summary>
    /// One timed cycle. Without a <paramref name="watermark"/> each cycle starts with none, as a just-restarted
    /// process does, so it reads only the look-back; pass one shared watermark to model a long-running runner.
    /// </summary>
    private async Task<FeedProjectorRun> RunCycleAsync(
        bool sweep = false,
        FeedProjectorOptions? options = null,
        FeedProjectorWatermark? watermark = null)
    {
        await using var context = NewContext();
        return await Cycle(context, options).RunAsync(sweep, watermark ?? new FeedProjectorWatermark());
    }

    private FeedProjectorCycle Cycle(ArdaNovaDbContext context, FeedProjectorOptions? options = null) => new(
        new FeedSourceReader(context),
        new FeedEnvelopeStore(context),
        new Repository<Post>(context),
        options ?? new FeedProjectorOptions(),
        _clock);

    private async Task<List<Post>> EnvelopesAsync()
    {
        await using var context = NewContext();
        return await context.Posts.AsNoTracking().Where(post => post.sourceType != null).ToListAsync();
    }

    private async Task<Result<PostDto>> ReadAsAsync(string postId, string? viewerId)
    {
        await using var context = NewContext();
        return await Posts(context).GetForViewerAsync(postId, viewerId);
    }

    private async Task<string> PostProjectUpdateAsync(string title)
    {
        await using var context = NewContext();
        return Succeeded(await Updates(context).CreateAsync(new CreateProjectUpdateDto
        {
            ProjectId = PublishedProjectId,
            UserId = Founder,
            Title = title,
            Content = "In the founder's own words.",
        })).Id;
    }

    private async Task<string> WriteFieldNoteAsync(string content)
    {
        await using var context = NewContext();
        return Succeeded(await Posts(context).CreateAsync(new CreatePostDto
        {
            AuthorId = Founder,
            ProjectId = PublishedProjectId,
            Content = content,
        })).Id;
    }

    private async Task<string> CommentAsync(string authorId, CommentTargetType target, string targetId)
    {
        await using var context = NewContext();
        return Succeeded(await Comments(context).CreateAsync(new CreateProjectCommentDto
        {
            ProjectId = PublishedProjectId,
            UserId = authorId,
            Content = "Should the wiring go in before the walls close?",
            TargetType = target,
            TargetId = targetId,
        })).Id;
    }

    private async Task<string> ScheduleEventAsync(string title, EventVisibility visibility)
    {
        await using var context = NewContext();
        return Succeeded(await Events(context).CreateAsync(new CreateEventDto
        {
            OrganizerId = Founder,
            Title = title,
            Type = EventType.SOCIAL,
            Visibility = visibility,
            Timezone = "UTC",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
            ProjectId = PublishedProjectId,
        })).Id;
    }

    private static async Task<bool> EngageAsync(PostService posts, string engagement, string postId)
        => engagement == "like"
            ? (await posts.LikeAsync(postId, Stranger)).IsSuccess
            : (await posts.BookmarkAsync(postId, Stranger)).IsSuccess;

    private static async Task<bool> DisengageAsync(PostService posts, string engagement, string postId)
        => engagement == "like"
            ? (await posts.UnlikeAsync(postId, Stranger)).IsSuccess
            : (await posts.UnbookmarkAsync(postId, Stranger)).IsSuccess;

    private async Task RenameColumnAsync(string table, string from, string to)
    {
        // Identifiers cannot be parameters; these are test-owned constants, never input.
        var statement = $"""ALTER TABLE "{table}" RENAME COLUMN "{from}" TO "{to}" """;
        await using var context = NewContext();
        await context.Database.ExecuteSqlRawAsync(statement);
    }

    /// <summary>Drops one server session the way a broken network or a cancelled command does, and waits for it.</summary>
    private async Task TerminateBackendAsync(int backendPid)
    {
        await using var connection = await _dataSource!.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT pg_terminate_backend(@pid, 5000)", connection);
        command.Parameters.AddWithValue("pid", backendPid);
        (await command.ExecuteScalarAsync()).Should().Be(true);
    }

    private static T Succeeded<T>(Result<T> result)
    {
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static PostService Posts(ArdaNovaDbContext context, IUnitOfWork? unitOfWork = null) => new(
        new Repository<Post>(context),
        new Repository<PostLike>(context),
        new Repository<PostComment>(context),
        new Repository<PostBookmark>(context),
        new Repository<PostShare>(context),
        unitOfWork ?? new UnitOfWorkOverContext(context),
        Mapper);

    private static ProjectUpdateService Updates(ArdaNovaDbContext context)
        => new(new Repository<ProjectUpdate>(context), new UnitOfWorkOverContext(context), Mapper);

    private static ProjectMilestoneService Milestones(ArdaNovaDbContext context)
        => new(new Repository<ProjectMilestone>(context), new UnitOfWorkOverContext(context), Mapper);

    private static TaskService Tasks(ArdaNovaDbContext context) => new(
        new Repository<ProjectTask>(context),
        new Repository<User>(context),
        new Repository<Project>(context),
        new Repository<TaskCommerceAgreement>(context),
        new UnitOfWorkOverContext(context),
        Mapper);

    private static ProjectCommentService Comments(ArdaNovaDbContext context)
        => new(new Repository<ProjectComment>(context), new Repository<User>(context), new UnitOfWorkOverContext(context), Mapper);

    private static ProjectService Projects(ArdaNovaDbContext context) => new(
        new ProjectRepository(context),
        new Repository<User>(context),
        new ProjectDeletionBlockers(context),
        new UnitOfWorkOverContext(context),
        Mapper);

    private static EventService Events(ArdaNovaDbContext context) => new(
        new Repository<Event>(context),
        new Repository<EventAttendee>(context),
        new Repository<User>(context),
        new UnitOfWorkOverContext(context),
        Mapper);

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

    /// <summary>EnsureCreated builds the EF model, which lacks two live-schema facts the projector relies on.</summary>
    /// <remarks>Mirrors prisma/migrations/20261027001000_feed_envelope_columns: the both-or-neither CHECK, and
    /// Post's target FKs as ON DELETE SET NULL (EF's default for an optional FK is NO ACTION).</remarks>
    private static Task ApplyLiveFeedConstraintsAsync(ArdaNovaDbContext context)
        => context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "Post" ADD CONSTRAINT "Post_sourceType_sourceId_check"
              CHECK (("sourceType" IS NULL) = ("sourceId" IS NULL));
            DO $$
            DECLARE target record;
            BEGIN
              FOR target IN
                SELECT c.conname, a.attname AS column_name, c.confrelid::regclass AS referenced
                FROM pg_constraint c
                JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1]
                WHERE c.conrelid = '"Post"'::regclass AND c.contype = 'f'
                  AND a.attname IN ('projectId', 'guildId', 'opportunityId', 'eventId')
              LOOP
                EXECUTE format('ALTER TABLE "Post" DROP CONSTRAINT %I', target.conname);
                EXECUTE format('ALTER TABLE "Post" ADD CONSTRAINT %I FOREIGN KEY (%I) REFERENCES %s ("id") ON DELETE SET NULL',
                  target.conname, target.column_name, target.referenced);
              END LOOP;
            END $$;
            """);

    /// <summary>A month-old world: a published project, a draft, and a teammate on both.</summary>
    private static async Task SeedAsync(ArdaNovaDbContext context)
    {
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        foreach (var (id, name) in new[]
                 {
                     (Founder, "Fern Founder"), (Teammate, "Tam Teammate"), (Stranger, "Sol Stranger"), (Backer, BackerName),
                 })
        {
            context.Users.Add(new User
            {
                id = id,
                email = $"{id}@example.test",
                name = name,
                createdAt = monthAgo,
                updatedAt = monthAgo,
            });
        }

        context.Projects.Add(NewProject(PublishedProjectId, ProjectStatus.PUBLISHED, monthAgo));
        context.Projects.Add(NewProject(DraftProjectId, ProjectStatus.DRAFT, monthAgo));
        context.ProjectMembers.Add(Member("member-published", PublishedProjectId, monthAgo));
        context.ProjectMembers.Add(Member("member-draft", DraftProjectId, monthAgo));
        await context.SaveChangesAsync();
    }

    private static Project NewProject(string id, ProjectStatus status, DateTime at) => new()
    {
        id = id,
        title = $"Solar barn ({id})",
        slug = id,
        description = "A community solar barn.",
        problemStatement = "The village has no shared workshop.",
        solution = "Build one with a solar roof.",
        categories = "ENVIRONMENT",
        status = status,
        createdById = Founder,
        createdAt = at,
        updatedAt = at,
        publishedAt = status == ProjectStatus.DRAFT ? null : at,
    };

    private static ProjectMember Member(string id, string projectId, DateTime at) => new()
    {
        id = id,
        projectId = projectId,
        userId = Teammate,
        role = ProjectRole.CONTRIBUTOR,
        joinedAt = at,
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

    /// <summary>The faked edge: the projector's clock, which a test may move forward.</summary>
    private sealed class MutableClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public void Advance(TimeSpan by) => _now = _now.Add(by);

        public override DateTimeOffset GetUtcNow() => _now;
    }

    /// <summary>Runs the other tap's whole request just before this one saves, so the pre-check has already passed.</summary>
    private sealed class InterleavingUnitOfWork(IUnitOfWork inner, Func<Task> otherTap) : IUnitOfWork
    {
        private Func<Task>? _pending = otherTap;

        public bool Raced { get; private set; }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _pending, null) is { } race)
            {
                await race();
                Raced = true;
            }

            return await inner.SaveChangesAsync(cancellationToken);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => inner.BeginTransactionAsync(cancellationToken);

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => inner.CommitTransactionAsync(cancellationToken);

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => inner.RollbackTransactionAsync(cancellationToken);

        public void ClearTrackedChanges() => inner.ClearTrackedChanges();

        public void Dispose() => inner.Dispose();
    }
}
