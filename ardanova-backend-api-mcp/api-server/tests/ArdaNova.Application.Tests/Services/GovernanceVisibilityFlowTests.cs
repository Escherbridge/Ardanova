namespace ArdaNova.Application.Tests.Services;

using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;

/// <summary>What the anonymous-readable governance surface shows to whom, and the discussion round trip the proposal page drives.</summary>
public class GovernanceVisibilityFlowTests
{
    private const string PublicProject = "public-project";
    private const string DraftProject = "draft-project";

    private readonly InMemoryRepository<Proposal> _proposals = new();
    private readonly InMemoryRepository<Vote> _votes = new();
    private readonly InMemoryRepository<ProposalExecution> _executions = new();
    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryRepository<Project> _projects = new();
    private readonly InMemoryRepository<ProposalComment> _comments = new();
    private readonly InMemoryRepository<MembershipCredential> _credentials = new();
    private readonly InMemoryUnitOfWork _unitOfWork = new();
    private readonly GovernanceService _sut;

    public GovernanceVisibilityFlowTests()
    {
        foreach (var id in new[] { "founder", "member", "outsider", "stealth" })
            _users.Rows.Add(new User { id = id, email = $"{id}@example.test", name = id });

        var publicProject = new Project { id = PublicProject, title = "Community roof", slug = "community-roof", status = ProjectStatus.PUBLISHED, createdById = "founder" };
        publicProject.ProjectMembers.Add(new ProjectMember { id = "pm-founder", projectId = PublicProject, userId = "founder", role = ProjectRole.FOUNDER });
        publicProject.ProjectMembers.Add(new ProjectMember { id = "pm-member", projectId = PublicProject, userId = "member", role = ProjectRole.CONTRIBUTOR });
        _projects.Rows.Add(publicProject);
        _projects.Rows.Add(new Project { id = DraftProject, title = "Unannounced", slug = "unannounced", status = ProjectStatus.DRAFT, createdById = "stealth" });

        AddProposal("open-vote", PublicProject, "founder", ProposalStatus.ACTIVE);
        AddProposal("members-draft", PublicProject, "member", ProposalStatus.DRAFT);
        AddProposal("hidden-project-vote", DraftProject, "stealth", ProposalStatus.ACTIVE);
        _votes.Rows.Add(new Vote { id = "v1", proposalId = "hidden-project-vote", voterId = "stealth", choice = 0, weight = 1m, reason = "private reasoning", createdAt = DateTime.UtcNow });

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _sut = new GovernanceService(_proposals, _votes, _executions, _users, _projects, _comments, _unitOfWork, mapper, _credentials);
    }

    private void AddProposal(string id, string projectId, string creatorId, ProposalStatus status)
        => _proposals.Rows.Add(new Proposal
        {
            id = id,
            projectId = projectId,
            creatorId = creatorId,
            type = ProposalType.GOVERNANCE,
            title = id,
            description = "d",
            options = "[\"Yes\",\"No\"]",
            quorum = 1,
            threshold = 51,
            status = status,
            votingEnd = DateTime.UtcNow.AddDays(3),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        });

    public static TheoryData<string?, string[]> WhoSeesWhat => new()
    {
        // Anonymous: only published proposals on public projects.
        { null, new[] { "open-vote" } },
        // A non-manager member does not see a colleague's draft... unless it is their own.
        { "outsider", new[] { "open-vote" } },
        { "member", new[] { "open-vote", "members-draft" } },
        // The people who run the project review drafts before they open.
        { "founder", new[] { "open-vote", "members-draft" } },
        // A DRAFT project's creator sees its proposals; nobody else does.
        { "stealth", new[] { "open-vote", "hidden-project-vote" } },
    };

    [Theory]
    [MemberData(nameof(WhoSeesWhat))]
    public async Task Every_listing_and_detail_read_answers_only_with_what_the_viewer_may_see(string? viewerId, string[] expected)
    {
        var viewer = viewerId is null ? ProjectViewer.Anonymous : new ProjectViewer(viewerId, IsAdmin: false);

        var all = await _sut.GetAllVisibleProposalsAsync(viewer);
        var searched = await _sut.SearchVisibleProposalsAsync(viewer, null, null, null, null, 1, 50);
        var detailIds = new List<string>();
        foreach (var id in new[] { "open-vote", "members-draft", "hidden-project-vote" })
            if ((await _sut.GetVisibleProposalByIdAsync(viewer, id)).IsSuccess)
                detailIds.Add(id);

        all.Value!.Select(p => p.Id).Should().BeEquivalentTo(expected);
        searched.Value!.Items.Select(p => p.Id).Should().BeEquivalentTo(expected);
        searched.Value!.TotalCount.Should().Be(expected.Length, "a hidden proposal must not even be counted");
        detailIds.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task A_project_filter_cannot_widen_what_an_anonymous_viewer_sees()
    {
        var byProject = await _sut.SearchVisibleProposalsAsync(ProjectViewer.Anonymous, null, null, null, DraftProject, 1, 50);
        var active = await _sut.GetVisibleActiveProposalsAsync(ProjectViewer.Anonymous);
        var byProposer = await _sut.GetVisibleByProposerIdAsync(ProjectViewer.Anonymous, "stealth");

        byProject.Value!.Items.Should().BeEmpty();
        active.Value!.Select(p => p.Id).Should().Equal("open-vote");
        byProposer.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task A_platform_admin_sees_every_proposal()
    {
        var all = await _sut.GetAllVisibleProposalsAsync(new ProjectViewer("admin", IsAdmin: true));

        all.Value!.Should().HaveCount(3);
    }

    [Fact]
    public async Task A_comment_body_without_route_fields_binds_is_stored_and_reads_back_after_a_reload()
    {
        // The exact body ProjectsController/GovernanceController receive from the client: the proposal id is in the route, the author is the actor.
        var body = JsonSerializer.Deserialize<CreateProposalCommentDto>(
            """{"userId":"member","content":"Should we use white membrane?","parentId":null}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var noUser = JsonSerializer.Deserialize<CreateProposalCommentDto>(
            """{"content":"Only content"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var posted = await _sut.CreateProposalCommentAsync(body with { ProposalId = "open-vote", UserId = "member" });
        var anonymousAuthor = await _sut.CreateProposalCommentAsync(noUser with { ProposalId = "open-vote" });
        var blank = await _sut.CreateProposalCommentAsync(new CreateProposalCommentDto { ProposalId = "open-vote", UserId = "member", Content = "   " });

        posted.IsSuccess.Should().BeTrue(posted.Error);
        anonymousAuthor.Type.Should().Be(ResultType.Unauthorized);
        blank.Type.Should().Be(ResultType.ValidationError);

        var thread = await _sut.GetProposalCommentsAsync("open-vote");
        thread.Value!.Should().ContainSingle()
            .Which.Content.Should().Be("Should we use white membrane?");
    }
    [Fact]
    public async Task Only_someone_who_may_read_a_proposal_may_write_into_its_discussion()
    {
        _users.Rows.Add(new User { id = "platform-admin", email = "admin@example.test", name = "Admin", role = UserRole.ADMIN });

        var outsiderOnDraft = await _sut.CreateProposalCommentAsync(Comment("members-draft", "outsider", "Injected into a private draft"));
        var outsiderOnHiddenProject = await _sut.CreateProposalCommentAsync(Comment("hidden-project-vote", "outsider", "Injected into a private project"));
        var outsiderOnOpenVote = await _sut.CreateProposalCommentAsync(Comment("open-vote", "outsider", "A public question"));
        var founderOnDraft = await _sut.CreateProposalCommentAsync(Comment("members-draft", "founder", "Reviewing before it opens"));
        var adminOnHiddenProject = await _sut.CreateProposalCommentAsync(Comment("hidden-project-vote", "platform-admin", "Moderation note"));

        outsiderOnDraft.Type.Should().Be(ResultType.NotFound, "a write must not reveal or reach a proposal the reader cannot see");
        outsiderOnHiddenProject.Type.Should().Be(ResultType.NotFound);
        outsiderOnOpenVote.IsSuccess.Should().BeTrue(outsiderOnOpenVote.Error);
        founderOnDraft.IsSuccess.Should().BeTrue(founderOnDraft.Error);
        adminOnHiddenProject.IsSuccess.Should().BeTrue(adminOnHiddenProject.Error);
        _comments.Rows.Should().NotContain(c => c.userId == "outsider" && c.proposalId != "open-vote");
    }

    [Fact]
    public async Task A_reply_must_thread_under_a_comment_on_the_same_proposal_and_stay_within_the_length_limit()
    {
        var onDraft = await _sut.CreateProposalCommentAsync(Comment("members-draft", "member", "Private thread"));
        var crossThreaded = await _sut.CreateProposalCommentAsync(Comment("open-vote", "member", "Reply", parentId: onDraft.Value!.Id));
        var missingParent = await _sut.CreateProposalCommentAsync(Comment("open-vote", "member", "Reply", parentId: "no-such-comment"));
        var oversized = await _sut.CreateProposalCommentAsync(Comment("open-vote", "member", new string('x', GovernanceService.MaxCommentLength + 1)));
        var top = await _sut.CreateProposalCommentAsync(Comment("open-vote", "member", "Top"));
        var reply = await _sut.CreateProposalCommentAsync(Comment("open-vote", "founder", "Reply", parentId: top.Value!.Id));
        var oversizedEdit = await _sut.UpdateProposalCommentAsync("open-vote", top.Value.Id, "member", new UpdateProposalCommentDto { Content = new string('x', GovernanceService.MaxCommentLength + 1) });
        var othersEdit = await _sut.UpdateProposalCommentAsync("open-vote", top.Value.Id, "founder", new UpdateProposalCommentDto { Content = "Rewritten" });
        var othersDelete = await _sut.DeleteProposalCommentAsync("open-vote", top.Value.Id, "founder");

        crossThreaded.Type.Should().Be(ResultType.ValidationError);
        missingParent.Type.Should().Be(ResultType.ValidationError);
        oversized.Type.Should().Be(ResultType.ValidationError);
        reply.IsSuccess.Should().BeTrue(reply.Error);
        oversizedEdit.Type.Should().Be(ResultType.ValidationError);
        othersEdit.Type.Should().Be(ResultType.Forbidden);
        othersDelete.Type.Should().Be(ResultType.Forbidden);

        var thread = await _sut.GetProposalCommentsAsync("open-vote");
        thread.Value!.Should().ContainSingle().Which.Should().Match<ProposalCommentDto>(c =>
            c.Content == "Top" && c.Replies!.Single().Content == "Reply");
    }

    private static CreateProposalCommentDto Comment(string proposalId, string userId, string content, string? parentId = null)
        => new() { ProposalId = proposalId, UserId = userId, Content = content, ParentId = parentId };
}
