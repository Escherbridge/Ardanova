namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;

/// <summary>A proposal's life from draft to tally, asserting on stored ballots and on what the read surface reports.</summary>
public class GovernanceBallotIntegrityFlowTests
{
    private const string ProjectId = "project-1";

    private readonly InMemoryRepository<Proposal> _proposals = new();
    private readonly InMemoryRepository<Vote> _votes = new();
    private readonly InMemoryRepository<ProposalExecution> _executions = new();
    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryRepository<Project> _projects = new();
    private readonly InMemoryRepository<ProposalComment> _comments = new();
    private readonly InMemoryRepository<MembershipCredential> _credentials = new();
    private readonly InMemoryUnitOfWork _unitOfWork = new();
    private readonly GovernanceService _sut;

    public GovernanceBallotIntegrityFlowTests()
    {
        // The (proposalId, voterId) unique index from migration 20261017120000_vote_one_ballot_per_member.
        _unitOfWork.Unique(_votes, v => (v.proposalId, v.voterId));

        _projects.Rows.Add(new Project { id = ProjectId, title = "Community roof", slug = "community-roof" });
        foreach (var member in new[] { "ana", "ben", "cy" })
        {
            _users.Rows.Add(new User { id = member, email = $"{member}@example.test", name = member });
            _credentials.Rows.Add(new MembershipCredential
            {
                id = $"cred-{member}",
                projectId = ProjectId,
                userId = member,
                status = MembershipCredentialStatus.ACTIVE
            });
        }

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _sut = new GovernanceService(_proposals, _votes, _executions, _users, _projects, _comments, _unitOfWork, mapper, _credentials);
    }

    private async Task<ProposalDto> OpenProposal(int quorumPercent = 1)
    {
        var draft = await _sut.CreateProposalAsync(new CreateProposalDto
        {
            ProjectId = ProjectId,
            CreatorId = "ana",
            Type = ProposalType.GOVERNANCE,
            Title = "Fix the roof?",
            Description = "It leaks.",
            Options = "[\"Yes\",\"No\"]",
            Quorum = quorumPercent,
            Threshold = 51,
            VotingEnd = DateTime.UtcNow.AddDays(3)
        });
        draft.IsSuccess.Should().BeTrue(draft.Error);
        var published = await _sut.PublishProposalAsync(draft.Value!.Id);
        published.IsSuccess.Should().BeTrue(published.Error);
        return published.Value!;
    }

    [Fact]
    public async Task A_second_ballot_that_slips_past_the_existence_check_is_refused_and_counted_once()
    {
        var proposal = await OpenProposal();

        // Ben's other tab records his ballot after this request has already looked and found none.
        _votes.AfterFindOne = () =>
        {
            _votes.AfterFindOne = null;
            _votes.Rows.Add(new Vote { id = "concurrent", proposalId = proposal.Id, voterId = "ben", choice = 0, weight = 1m, createdAt = DateTime.UtcNow });
        };

        var raced = await _sut.CastVoteAsync(proposal.Id, new CastVoteDto { VoterId = "ben", Choice = 1 });

        raced.IsSuccess.Should().BeFalse();
        raced.Error.Should().Contain("already voted");
        _votes.Rows.Where(v => v.voterId == "ben").Should().ContainSingle().Which.choice.Should().Be(0);
        var summary = await _sut.GetVoteSummaryAsync(proposal.Id);
        summary.Value!.TotalVotes.Should().Be(1);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(11.9)]
    public async Task A_voting_window_too_short_for_members_to_see_is_refused_at_draft_and_at_publish(double hours)
    {
        var tooSoon = DateTime.UtcNow.AddHours(hours);

        var created = await _sut.CreateProposalAsync(new CreateProposalDto
        {
            ProjectId = ProjectId, CreatorId = "ana", Type = ProposalType.GOVERNANCE,
            Title = "Quick one", Description = "Blink and you miss it.", Options = "[\"Yes\",\"No\"]",
            VotingEnd = tooSoon
        });
        created.Type.Should().Be(ResultType.ValidationError);

        // A draft whose stated close has drifted too near by the time it is opened.
        _proposals.Rows.Add(new Proposal
        {
            id = "drifted", projectId = ProjectId, creatorId = "ana", type = ProposalType.GOVERNANCE,
            title = "Drifted", description = "Old draft", options = "[\"Yes\",\"No\"]", quorum = 50, threshold = 51,
            status = ProposalStatus.DRAFT, votingEnd = tooSoon, createdAt = DateTime.UtcNow.AddDays(-2), updatedAt = DateTime.UtcNow.AddDays(-2)
        });
        var published = await _sut.PublishProposalAsync("drifted");

        published.Type.Should().Be(ResultType.ValidationError);
        _proposals.Rows.Single(p => p.id == "drifted").status.Should().Be(ProposalStatus.DRAFT);
        _proposals.Rows.Should().ContainSingle();
    }

    [Fact]
    public async Task The_read_surface_reports_quorum_in_members_with_the_two_ballot_floor_and_counts_per_option()
    {
        var proposal = await OpenProposal(quorumPercent: 1);

        await _sut.CastVoteAsync(proposal.Id, new CastVoteDto { VoterId = "ben", Choice = 1 });
        var afterOne = await _sut.GetProposalByIdAsync(proposal.Id);
        var summaryAfterOne = await _sut.GetVoteSummaryAsync(proposal.Id);

        afterOne.Value!.EligibleMembers.Should().Be(3);
        afterOne.Value.VotesNeededForQuorum.Should().Be(2, "1% of three members rounds up to one ballot, and one member alone is never a quorum");
        afterOne.Value.QuorumReached.Should().BeFalse();
        afterOne.Value.VotesByChoice.Should().BeEquivalentTo(new Dictionary<int, int> { [1] = 1 });
        summaryAfterOne.Value!.EligibleMembers.Should().Be(3);
        summaryAfterOne.Value.VotesNeededForQuorum.Should().Be(2);

        await _sut.CastVoteAsync(proposal.Id, new CastVoteDto { VoterId = "cy", Choice = 1 });
        var afterTwo = await _sut.GetProposalByIdAsync(proposal.Id);
        afterTwo.Value!.QuorumReached.Should().BeTrue();
        afterTwo.Value.VotesByChoice[1].Should().Be(2);
    }
}
