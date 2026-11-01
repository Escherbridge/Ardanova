namespace ArdaNova.Application.Tests.Flows;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Feed;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

/// <summary>
/// SR-03 phase 3 lane K2 on the same throwaway database and seed as the rest of <see cref="FeedReadFlowTests"/>:
/// HELP_WANTED asks on every read path, the per-scope page rules (Q1) and the opportunity slug. Asks are written
/// straight to Post because their write path (PostService, lane K1) arrives at the join; the read side is under test.
/// </summary>
public sealed partial class FeedReadFlowTests
{
    [PostgresFact]
    public async Task AProjectsOwnFeed_ServesAllSixOfOneWritersPosts_NewestFirst_WhileHomeStillCapsThatProject()
    {
        var now = DateTime.UtcNow.AddMinutes(-1);
        var barnNotes = await SeedNotesAsync(Enumerable.Range(0, 6).Select(index => (BarnId, Fern, now.AddMinutes(-index))).ToList());
        await SeedNotesAsync(Enumerable.Range(0, 4)
            .Select(index => ($"project-elsewhere-{index}", $"user-e{index}", now.AddHours(-1).AddMinutes(-index)))
            .ToList());

        var projectPage = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{BarnId}", Limit: 20)));

        projectPage.Items.Select(card => card.Id).Should().Equal(barnNotes,
            "a one-writer project feed is recency only: neither the author cap (3) nor the subject cap (2) may thin or reorder it");
        projectPage.NextCursor.Should().BeNull();

        var homePage = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, Limit: 4)));
        homePage.Items.Should().HaveCount(4);
        homePage.Items.Count(card => card.Project!.Id == BarnId).Should().Be(FeedRanking.HomeSubjectCap,
            "home still caps a subject at two per page, though the barn's six notes are the newest");
    }

    [PostgresFact]
    public async Task AnOpenAsk_OffersAMemberThePickUp_ShowsAFollowerTheSameCardWithoutIt_AndNeverReachesAStranger()
    {
        await FollowProjectAsync(Ada, BarnId);
        const string words = "Framing the east wall Saturday, need two hands #carpentry";
        var barnTask = await SeedTaskAsync(BarnId, "Frame the east wall");
        var ask = await SeedAskAsync(Fern, BarnId, barnTask, words);
        var draftAsk = await SeedAskAsync(Fern, DraftId, await SeedTaskAsync(DraftId, "Test the prototype"), "Who can test the prototype? #carpentry");
        var teamOnlyAsk = await SeedAskAsync(Fern, BarnId, await SeedTaskAsync(BarnId, "Price the roof"), "Pricing the roof.", PostVisibility.PROJECT_MEMBERS);

        var member = (await PageAsync(Bo, limit: 50)).Items.Single(card => card.Id == ask);
        member.PostType.Should().Be(nameof(PostType.HELP_WANTED));
        member.Body.Should().Be(words);
        member.BodyIsUserWritten.Should().BeTrue();
        member.Task.Should().Be(new FeedTaskDto(barnTask, "OPEN", AssigneeName: null, Paid: false));
        member.Viewer.Should().Be(new FeedViewerRelationDto(IsMember: true, IsFollowing: false, CanClaim: true));
        member.Reasons.Should().Contain(reason => reason.Kind == nameof(FeedReasonKind.ProjectTeam) && reason.AboutId == BarnId);

        var follower = (await PageAsync(Ada, limit: 50)).Items.Single(card => card.Id == ask);
        follower.Task.Should().Be(member.Task, "a follower sees the same ask and the same task facts");
        follower.Viewer.Should().Be(new FeedViewerRelationDto(IsMember: false, IsFollowing: true, CanClaim: false));
        follower.Reasons.Should().Contain(reason => reason.Kind == nameof(FeedReasonKind.FollowedProject) && reason.AboutId == BarnId)
            .And.NotContain(reason => reason.Kind == nameof(FeedReasonKind.ProjectTeam));

        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Bo, $"project:{BarnId}", Limit: 50))).Items
            .Single(card => card.Id == ask).Viewer.CanClaim.Should().BeTrue("the project's own feed computes the same relation");

        (await WalkAsync(Sol)).Should().Contain(ask, "the walk is not vacuous: a published project's public ask reaches everyone")
            .And.NotContain(new[] { draftAsk, teamOnlyAsk });
        Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Sol, "tag:carpentry", Limit: 50))).Items
            .Select(card => card.Id).Should().Contain(ask).And.NotContain(draftAsk);
        (await Feed().GetPageAsync(new FeedPageRequest(Sol, $"project:{DraftId}"))).Type.Should().Be(ResultType.NotFound);
        (await WalkAsync(Ada)).Should().NotContain(teamOnlyAsk, "following a project is not membership");

        (await PageAsync(Tam, limit: 50)).Items.Single(card => card.Id == draftAsk).Viewer.CanClaim
            .Should().BeTrue("the draft's own team may pick its tasks up");
        (await PageAsync(Bo, limit: 50)).Items.Single(card => card.Id == teamOnlyAsk).Viewer.CanClaim.Should().BeTrue();
    }

    [PostgresFact]
    public async Task ATeammateIsOfferedNoPickUp_OnAPaidAFundedAnAssignedOrACompletedTask_AndEachCardSaysWhichState()
    {
        var paidTask = await SeedTaskAsync(BarnId, "Wire the barn", equityReward: 50m);
        var fundedTask = await SeedTaskAsync(BarnId, "Lay the floor", escrowStatus: EscrowStatus.FUNDED);
        var takenTask = await SeedTaskAsync(BarnId, "Hang the doors", assignedToId: Fern);
        var doneTask = await SeedTaskAsync(BarnId, "Pour the footings", assignedToId: Fern, status: TaskStatus.COMPLETED);
        var paid = await SeedAskAsync(Fern, BarnId, paidTask, "Need an electrician for the barn.");
        var funded = await SeedAskAsync(Fern, BarnId, fundedTask, "Floorboards are in.");
        var taken = await SeedAskAsync(Fern, BarnId, takenTask, "Doors need hanging.");
        var done = await SeedAskAsync(Fern, BarnId, doneTask, "Footings need pouring.");

        var cards = (await PageAsync(Bo, limit: 50)).Items.ToDictionary(card => card.Id);

        cards.Values.Should().OnlyContain(card => card.Viewer.IsMember && !card.Viewer.CanClaim,
            "Bo is on the barn's team, so every refusal here is the task's, never membership's");
        cards[paid].Task.Should().Be(new FeedTaskDto(paidTask, "OPEN", AssigneeName: null, Paid: true),
            "a paid task stays open on the card; its claim goes through the task page's payout readiness check");
        cards[funded].Task.Should().Be(new FeedTaskDto(fundedTask, "OPEN", AssigneeName: null, Paid: false));
        cards[taken].Task.Should().Be(new FeedTaskDto(takenTask, "PICKED_UP", AssigneeName: "fern", Paid: false));
        cards[done].Task!.State.Should().Be("DONE");
    }

    [PostgresFact]
    public async Task DeletingAnAsksTask_DropsTheAskFromEveryRead_AndTheScrollRefillsWithoutRepeatingACard()
    {
        var now = DateTime.UtcNow.AddMinutes(-10);
        var notes = await SeedNotesAsync(Enumerable.Range(1, 7)
            .Select(index => (BarnId, index % 2 == 0 ? Fern : Bo, now.AddMinutes(-index)))
            .ToList());
        var task = await SeedTaskAsync(BarnId, "Frame the east wall");
        var ask = await SeedAskAsync(Fern, BarnId, task, "Framing the east wall Saturday, need two hands.");

        (await WalkAsync(Bo)).Should().Contain(ask, "the ask is served while its task exists");

        await using (var context = NewContext())
            await context.ProjectTasks.Where(row => row.id == task).ExecuteDeleteAsync();
        await using (var check = NewContext())
            (await check.Posts.AsNoTracking().SingleAsync(post => post.id == ask)).taskId
                .Should().BeNull("deleting the task un-asks it; the asker's words stay (FK ON DELETE SET NULL)");

        foreach (var scope in new[] { "home", $"project:{BarnId}" })
        {
            var pages = await PagesAsync(Bo, scope, limit: 3);
            pages[0].Items.Should().HaveCount(3, $"{scope}: the newest card, the dropped ask, is replaced from further down");
            pages.SelectMany(page => page.Items).Select(card => card.Id).Should().OnlyHaveUniqueItems()
                .And.BeEquivalentTo(notes, $"{scope}: the scroll goes on past the dropped ask to every other card, once each");
        }
    }

    [PostgresFact]
    public async Task AnOpportunityCard_CarriesTheSlugItsPageResolvesBy()
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
        string slug;
        await using (var context = NewContext())
            slug = await context.Opportunities.Where(row => row.id == opportunityId).Select(row => row.slug).SingleAsync();

        var card = (await PageAsync(Ada, limit: 50)).Items.Single(item => item.PostType == nameof(PostType.OPPORTUNITY_OPENED));

        slug.Should().NotBeNullOrWhiteSpace();
        card.Opportunity.Should().Be(new FeedLinkDto(opportunityId, "Site carpenter", slug));
    }

    [PostgresFact]
    public async Task ASectorTag_StillServesAnUntaggedAskOnThatSectorsProject_WithItsTaskFacts()
    {
        var task = await SeedTaskAsync(BarnId, "Frame the east wall");
        var ask = await SeedAskAsync(Fern, BarnId, task, "Need two hands on Saturday.");
        var kitchenAsk = await SeedAskAsync(Sol, "project-kitchen", await SeedTaskAsync("project-kitchen", "Fix the oven"), "The oven needs a look.");

        var items = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(Ada, "tag:construction", Limit: 50))).Items;

        var card = items.Should().ContainSingle(item => item.Id == ask, "an untagged ask on a CONSTRUCTION project belongs to #construction").Subject;
        card.Hashtags.Should().BeEmpty();
        card.Reasons.Should().Contain(reason => reason.Kind == nameof(FeedReasonKind.InScope) && reason.Text == "In Construction");
        card.Task.Should().Be(new FeedTaskDto(task, "OPEN", AssigneeName: null, Paid: false));
        card.Viewer.CanClaim.Should().BeFalse("Ada is not on the barn's team");
        items.Select(item => item.Id).Should().NotContain(kitchenAsk, "the kitchen is not construction");
    }

    /// <summary>The S3 migration's Post.taskId rules that EnsureCreated cannot express: FK ON DELETE SET NULL, HELP_WANTED-only CHECK.</summary>
    private static Task MirrorHelpWantedMigrationAsync(ArdaNovaDbContext context)
        => context.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE existing text;
            BEGIN
              SELECT conname INTO existing FROM pg_constraint
               WHERE conrelid = '"Post"'::regclass AND contype = 'f'
                 AND conkey = ARRAY[(SELECT attnum FROM pg_attribute WHERE attrelid = '"Post"'::regclass AND attname = 'taskId')];
              IF existing IS NOT NULL THEN
                EXECUTE format('ALTER TABLE "Post" DROP CONSTRAINT %I', existing);
              END IF;
            END $$;
            ALTER TABLE "Post" ADD CONSTRAINT "Post_taskId_fkey"
              FOREIGN KEY ("taskId") REFERENCES "ProjectTask"("id") ON DELETE SET NULL ON UPDATE CASCADE;
            ALTER TABLE "Post" ADD CONSTRAINT "Post_taskId_help_wanted_only"
              CHECK ("taskId" IS NULL OR "type"::text = 'HELP_WANTED');
            """);

    private async Task<string> SeedTaskAsync(
        string projectId,
        string title,
        decimal? equityReward = null,
        string? assignedToId = null,
        TaskStatus status = TaskStatus.TODO,
        EscrowStatus escrowStatus = EscrowStatus.NONE)
    {
        await using var context = NewContext();
        var now = DateTime.UtcNow;
        var task = new ProjectTask
        {
            id = Guid.NewGuid().ToString(),
            projectId = projectId,
            title = title,
            status = status,
            priority = TaskPriority.MEDIUM,
            taskType = TaskType.FEATURE,
            equityReward = equityReward,
            escrowStatus = escrowStatus,
            assignedToId = assignedToId,
            completedAt = status == TaskStatus.COMPLETED ? now : null,
            createdAt = now,
            updatedAt = now,
        };
        context.ProjectTasks.Add(task);
        await context.SaveChangesAsync();
        return task.id;
    }

    /// <summary>A HELP_WANTED post as lane K1's composer writes it: the asker's own words, a task on the same project.</summary>
    private async Task<string> SeedAskAsync(
        string authorId,
        string projectId,
        string taskId,
        string content,
        PostVisibility visibility = PostVisibility.PUBLIC)
    {
        await using var context = NewContext();
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid().ToString();
        context.Posts.Add(new Post
        {
            id = id,
            authorId = authorId,
            projectId = projectId,
            type = PostType.HELP_WANTED,
            visibility = visibility,
            content = content,
            hashtags = Hashtags.Extract(content),
            taskId = taskId,
            createdAt = now,
            updatedAt = now,
        });
        await context.SaveChangesAsync();
        return id;
    }

    /// <summary>Every page of one scope's scroll, until the cursor runs out.</summary>
    private async Task<List<FeedPageDto>> PagesAsync(string viewerId, string scope, int limit)
    {
        var pages = new List<FeedPageDto>();
        string? cursor = null;
        do
        {
            var page = Succeeded(await Feed().GetPageAsync(new FeedPageRequest(viewerId, scope, cursor, limit)));
            pages.Add(page);
            cursor = page.NextCursor;
        }
        while (cursor is not null && pages.Count < 20);

        return pages;
    }
}
