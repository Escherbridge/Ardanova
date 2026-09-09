namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

/// <summary>OI-46: a decision one member can start, vote in and close is not a decision. Every assertion here is on the proposal row, not on the door that reached it.</summary>
public class GovernanceDecisionAuthorityTests
{
    private const string OptionsJson = "[\"Yes\",\"No\"]";

    private readonly Mock<IRepository<Proposal>> _proposalRepositoryMock = new();
    private readonly Mock<IRepository<Vote>> _voteRepositoryMock = new();
    private readonly Mock<IRepository<ProposalExecution>> _executionRepositoryMock = new();
    private readonly Mock<IRepository<User>> _userRepositoryMock = new();
    private readonly Mock<IRepository<Project>> _projectRepositoryMock = new();
    private readonly Mock<IRepository<ProposalComment>> _proposalCommentRepositoryMock = new();
    private readonly Mock<IRepository<MembershipCredential>> _membershipCredentialRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly GovernanceService _sut;

    public GovernanceDecisionAuthorityTests()
    {
        // Plumbing, not policy: an unstubbed ballot query must read as "no votes yet", and every
        // read path maps the proposal it returns. Electorate size is never defaulted - each test
        // states the member register it assumes.
        GivenBallots();
        // IMapper.Map<TDestination>(object source), so the callback takes object, not Proposal.
        _mapperMock
            .Setup(m => m.Map<ProposalDto>(It.IsAny<Proposal>()))
            .Returns((object source) => AsDto((Proposal)source));

        _sut = new GovernanceService(
            _proposalRepositoryMock.Object,
            _voteRepositoryMock.Object,
            _executionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _proposalCommentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _membershipCredentialRepositoryMock.Object);
    }

    // ===== THE AUTHOR CANNOT CLOSE THEIR OWN VOTE =====

    [Fact]
    public async Task CloseVoteAsync_ByTheProposalAuthor_IsRefusedAndTheWindowDoesNotMove()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));
        var windowBefore = proposal.votingEnd;

        var result = await _sut.CloseVoteAsync(
            proposal.id,
            ProposalCloseAuthority.ProjectAuthority(proposal.creatorId));

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        proposal.votingEnd.Should().Be(windowBefore);
        proposal.status.Should().Be(ProposalStatus.ACTIVE);
        _proposalRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseVoteAsync_ByAnIndependentProjectAuthority_ShutsTheWindow()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));

        var result = await _sut.CloseVoteAsync(
            proposal.id,
            ProposalCloseAuthority.ProjectAuthority("someone-else-who-runs-the-project"));

        result.IsSuccess.Should().BeTrue();
        proposal.votingEnd.Should().NotBeNull();
        proposal.votingEnd!.Value.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    /// <summary>Closing shuts the window; it does not declare the result. No single request does both.</summary>
    [Fact]
    public async Task CloseVoteAsync_DoesNotResolveTheOutcomeInTheSameRequest()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));
        GivenElectorateOf(40);
        GivenBallots(BallotFor(proposal.id, choice: 0));

        var result = await _sut.CloseVoteAsync(
            proposal.id,
            ProposalCloseAuthority.ProjectAuthority("someone-else-who-runs-the-project"));

        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.ACTIVE);
        result.Value!.Status.Should().Be(ProposalStatus.ACTIVE);
        _executionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<ProposalExecution>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>A blank closer id is the shape a misconfigured caller produces most easily, so it must resolve to the narrowest authority, never the widest.</summary>
    [Fact]
    public async Task CloseVoteAsync_WithABlankCloserId_IsRefused()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));

        var result = await _sut.CloseVoteAsync(
            proposal.id,
            ProposalCloseAuthority.ProjectAuthority("   "));

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        proposal.status.Should().Be(ProposalStatus.ACTIVE);
    }

    /// <summary>The other door into a close is the generic proposal update, which carries no caller identity - so it cannot end an open vote at all.</summary>
    [Fact]
    public async Task UpdateProposalAsync_CannotEndAnOpenVote()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));
        var windowBefore = proposal.votingEnd;

        var result = await _sut.UpdateProposalAsync(
            proposal.id,
            new UpdateProposalDto { VotingEnd = DateTime.UtcNow });

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        proposal.votingEnd.Should().Be(windowBefore);
        proposal.status.Should().Be(ProposalStatus.ACTIVE);
    }

    /// <summary>The gate is on ending a vote EARLY. A window that already ran out is simply recorded, and needs no authority.</summary>
    [Fact]
    public async Task UpdateProposalAsync_OnAWindowThatAlreadyRanOut_RecordsTheDueOutcome()
    {
        // This test is about the WINDOW resolving, so the ballots have to genuinely pass: the
        // fixture's default quorum is 50%, and two ballots only carry that in an electorate of
        // four. Sized against the electorate rather than the ballot count on purpose -- two is
        // also MinimumBallotsForQuorum, so this still proves the floor is satisfied, not bypassed.
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(-1));
        GivenElectorateOf(4);
        GivenBallots(
            BallotFor(proposal.id, choice: 0),
            BallotFor(proposal.id, choice: 0));

        var result = await _sut.UpdateProposalAsync(
            proposal.id,
            new UpdateProposalDto { VotingEnd = DateTime.UtcNow });

        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.PASSED);
    }

    // ===== ONE BALLOT CANNOT CARRY A DECISION =====

    /// <summary>OI-46's arithmetic: one percent of forty members rounds up to a single ballot, and the single ballot an attacker can produce is their own.</summary>
    [Fact]
    public async Task GetVoteSummaryAsync_WithFortyMembersAndOneBallot_CannotReachQuorum()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7), quorumPercent: 1);
        GivenElectorateOf(40);
        GivenBallots(BallotFor(proposal.id, choice: 0));

        var result = await _sut.GetVoteSummaryAsync(proposal.id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuorumReached.Should().BeFalse();
        result.Value.WinningOption.Should().BeNull();
    }

    /// <summary>The whole attack at the counter: raise it, cast the one ballot, let the window run out. The record must not read PASSED.</summary>
    [Fact]
    public async Task ASingleBallotFromFortyMembers_ResolvesToExpiredNotPassed()
    {
        var proposal = GivenAnOpenProposal(
            votingEnd: DateTime.UtcNow.AddMinutes(-1),
            quorumPercent: 1,
            thresholdPercent: 100);
        GivenElectorateOf(40);
        GivenBallots(BallotFor(proposal.id, choice: 0));

        var result = await _sut.GetProposalByIdAsync(proposal.id);

        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.EXPIRED);
    }

    /// <summary>The floor is a minimum, not a blockade: two members who agree still carry the decision.</summary>
    [Fact]
    public async Task TwoBallotsFromFortyMembers_ReachQuorumAndPass()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddMinutes(-1), quorumPercent: 1);
        GivenElectorateOf(40);
        GivenBallots(
            BallotFor(proposal.id, choice: 0),
            BallotFor(proposal.id, choice: 0));

        var result = await _sut.GetProposalByIdAsync(proposal.id);

        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.PASSED);
    }

    /// <summary>An electorate of one has no second member to find, and the whole register voting is not one member manufacturing a decision.</summary>
    [Fact]
    public async Task AnElectorateOfOne_StillResolves()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddMinutes(-1), quorumPercent: 1);
        GivenElectorateOf(1);
        GivenBallots(BallotFor(proposal.id, choice: 0));

        var result = await _sut.GetProposalByIdAsync(proposal.id);

        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.PASSED);
    }

    // ===== CLOSING IS NOT WITHDRAWING =====

    /// <summary>Withdrawing throws the proposal away; once a ballot exists the vote must be closed and recorded instead.</summary>
    [Fact]
    public async Task CancelProposalAsync_OnceAMemberHasVoted_IsRefused()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));
        GivenBallotCountOf(1);

        var result = await _sut.CancelProposalAsync(proposal.id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("close the vote");
        proposal.status.Should().Be(ProposalStatus.ACTIVE);
    }

    /// <summary>With no ballot on file a proposal is still withdrawable, so the refusal above turns on the ballots and not on the status.</summary>
    [Fact]
    public async Task CancelProposalAsync_WithNoBallotsOnFile_Withdraws()
    {
        var proposal = GivenAnOpenProposal(votingEnd: DateTime.UtcNow.AddDays(7));
        GivenBallotCountOf(0);

        var result = await _sut.CancelProposalAsync(proposal.id);

        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.CANCELLED);
    }

    // ===== fixtures =====

    private Proposal GivenAnOpenProposal(
        DateTime? votingEnd,
        int quorumPercent = 50,
        int thresholdPercent = 51)
    {
        var proposal = new Proposal
        {
            id = Guid.NewGuid().ToString(),
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Should we fix the roof?",
            description = "The roof leaks.",
            options = OptionsJson,
            quorum = quorumPercent,
            threshold = thresholdPercent,
            status = ProposalStatus.ACTIVE,
            votingStart = DateTime.UtcNow.AddDays(-1),
            votingEnd = votingEnd,
            createdAt = DateTime.UtcNow.AddDays(-1),
            updatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _proposalRepositoryMock
            .Setup(r => r.GetByIdAsync(proposal.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        return proposal;
    }

    /// <summary>The read surface a caller sees, taken from the row at the moment it is mapped.</summary>
    private static ProposalDto AsDto(Proposal proposal) => new()
    {
        Id = proposal.id,
        ProjectId = proposal.projectId,
        CreatorId = proposal.creatorId,
        Status = proposal.status,
        VotingEnd = proposal.votingEnd
    };

    private static Vote BallotFor(string proposalId, int choice) => new()
    {
        id = Guid.NewGuid().ToString(),
        proposalId = proposalId,
        voterId = Guid.NewGuid().ToString(),
        choice = choice,
        weight = 1m,
        createdAt = DateTime.UtcNow
    };

    private void GivenBallots(params Vote[] ballots) =>
        _voteRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Vote, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ballots.ToList());

    private void GivenBallotCountOf(int ballots) =>
        _voteRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Vote, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ballots);

    /// <summary>Quorum is a share of the confirmed members, so a tally needs an electorate to divide by.</summary>
    private void GivenElectorateOf(int confirmedMembers) =>
        _membershipCredentialRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedMembers);
}
