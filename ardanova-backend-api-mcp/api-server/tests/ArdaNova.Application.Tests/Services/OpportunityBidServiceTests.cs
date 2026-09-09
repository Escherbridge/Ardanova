namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>
/// The two rules a proposal has to satisfy before it is written: the opportunity is still
/// taking proposals, and a guild bid says how the award splits. Both are asserted at the
/// counter — no bid row, no save — because a refusal that still writes is not a refusal.
/// </summary>
public class OpportunityBidServiceTests
{
    private readonly Mock<IRepository<OpportunityBid>> _bids = new();
    private readonly Mock<IRepository<Opportunity>> _opportunities = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<Guild>> _guilds = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly OpportunityBidService _sut;

    private OpportunityBid? _written;

    public OpportunityBidServiceTests()
    {
        _mapper
            .Setup(mapper => mapper.Map<OpportunityBidDto>(It.IsAny<object>()))
            .Returns((object source) => Describe((OpportunityBid)source));
        _bids
            .Setup(repository => repository.AddAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()))
            .Callback<OpportunityBid, CancellationToken>((bid, _) => _written = bid)
            .ReturnsAsync((OpportunityBid bid, CancellationToken _) => bid);
        _bids
            .Setup(repository => repository.UpdateAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new OpportunityBidService(
            _bids.Object,
            _opportunities.Object,
            _users.Object,
            _guilds.Object,
            _unitOfWork.Object,
            _mapper.Object);
    }

    [Theory]
    [InlineData(OpportunityStatus.DRAFT)]
    [InlineData(OpportunityStatus.IN_REVIEW)]
    [InlineData(OpportunityStatus.FILLED)]
    [InlineData(OpportunityStatus.CLOSED)]
    [InlineData(OpportunityStatus.CANCELLED)]
    public async Task CreateAsync_RefusesAProposalWhenTheOpportunityIsNotOpen(OpportunityStatus status)
    {
        ArrangeOpportunity(status);

        var result = await _sut.CreateAsync(PaidBid());

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(OpportunityBidService.OpportunityNotOpen);
        VerifyNothingWasWritten();
    }

    [Fact]
    public async Task CreateAsync_AcceptsAProposalWhileTheOpportunityIsOpen()
    {
        ArrangeOpportunity(OpportunityStatus.OPEN);

        var result = await _sut.CreateAsync(PaidBid());

        result.IsSuccess.Should().BeTrue();
        _written.Should().NotBeNull();
        _written!.proposedAmount.Should().Be(25m);
        _written.guildAwardSplit.Should().BeNull();
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RefusesAPaidGuildBidThatDoesNotSayHowItSplits()
    {
        ArrangeOpportunity(OpportunityStatus.OPEN);

        var result = await _sut.CreateAsync(PaidBid(guildId: "guild-1"));

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(OpportunityBidService.GuildStakeBidUnsupported);
        VerifyNothingWasWritten();
    }

    [Fact]
    public async Task CreateAsync_RecordsTheShareADeclaredGuildSplitNames()
    {
        ArrangeOpportunity(OpportunityStatus.OPEN);

        var result = await _sut.CreateAsync(PaidBid(guildId: "guild-1", guildAwardSplit: 10m));

        result.IsSuccess.Should().BeTrue();
        _written.Should().NotBeNull();
        _written!.guildId.Should().Be("guild-1");
        _written.guildAwardSplit.Should().Be(10m);
        result.Value!.GuildAwardSplit.Should().Be(10m);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(40)]
    public async Task CreateAsync_RefusesASplitThatLeavesTheWorkerNothing(int guildAwardSplit)
    {
        ArrangeOpportunity(OpportunityStatus.OPEN);

        var result = await _sut.CreateAsync(
            PaidBid(guildId: "guild-1", guildAwardSplit: guildAwardSplit));

        result.Error.Should().Be(OpportunityBidService.GuildSplitOutOfRange);
        VerifyNothingWasWritten();
    }

    [Fact]
    public async Task CreateAsync_RefusesASplitThatNamesNoGuild()
    {
        ArrangeOpportunity(OpportunityStatus.OPEN);

        var result = await _sut.CreateAsync(PaidBid(guildAwardSplit: 10m));

        result.Error.Should().Be(OpportunityBidService.GuildSplitNeedsGuild);
        VerifyNothingWasWritten();
    }

    [Fact]
    public async Task CreateAsync_RefusesAPartUnitGuildShare()
    {
        ArrangeOpportunity(OpportunityStatus.OPEN);

        var result = await _sut.CreateAsync(PaidBid(guildId: "guild-1", guildAwardSplit: 10.5m));

        result.Error.Should().Be(OpportunityBidService.PartUnitSplitUnsupported);
        VerifyNothingWasWritten();
    }

    [Fact]
    public async Task UpdateAsync_RefusesAnEditOnceTheOpportunityHasStoppedTakingProposals()
    {
        ArrangeOpportunity(OpportunityStatus.CLOSED);
        var bid = new OpportunityBid
        {
            id = "bid-1",
            opportunityId = "opportunity-1",
            bidderId = "contributor-1",
            proposal = "I can deliver it",
            proposedAmount = 25m,
            status = BidStatus.SUBMITTED,
        };
        _bids
            .Setup(repository => repository.GetByIdAsync(bid.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bid);

        var result = await _sut.UpdateAsync(bid.id, new UpdateOpportunityBidDto { ProposedAmount = 40m });

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(OpportunityBidService.OpportunityNotOpen);
        bid.proposedAmount.Should().Be(25m);
        _bids.Verify(
            repository => repository.UpdateAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The real mapping profile, not this class's stand-in: a split the service stores is worth
    /// nothing if the read surface drops it on the way out.
    /// </summary>
    [Fact]
    public void OpportunityBidMapping_CarriesTheDeclaredGuildShare()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var dto = mapper.Map<OpportunityBidDto>(new OpportunityBid
        {
            id = "bid-1",
            opportunityId = "opportunity-1",
            bidderId = "contributor-1",
            guildId = "guild-1",
            proposal = "I can deliver it",
            proposedAmount = 25m,
            guildAwardSplit = 10m,
            status = BidStatus.SUBMITTED,
        });

        dto.GuildAwardSplit.Should().Be(10m);
        dto.ProposedAmount.Should().Be(25m);
    }

    private Opportunity ArrangeOpportunity(OpportunityStatus status)
    {
        var opportunity = new Opportunity
        {
            id = "opportunity-1",
            projectId = "project-1",
            posterId = "poster-1",
            title = "Build the thing",
            description = "Deliver the thing",
            status = status,
        };
        _opportunities
            .Setup(repository => repository.GetByIdAsync(opportunity.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        return opportunity;
    }

    private static CreateOpportunityBidDto PaidBid(
        string? guildId = null,
        decimal? guildAwardSplit = null) => new()
        {
            OpportunityId = "opportunity-1",
            BidderId = "contributor-1",
            GuildId = guildId,
            ProposedAmount = 25m,
            GuildAwardSplit = guildAwardSplit,
            Proposal = "I can deliver it",
        };

    private static OpportunityBidDto Describe(OpportunityBid bid) => new()
    {
        Id = bid.id,
        OpportunityId = bid.opportunityId,
        BidderId = bid.bidderId,
        GuildId = bid.guildId,
        ProposedAmount = bid.proposedAmount,
        GuildAwardSplit = bid.guildAwardSplit,
        Proposal = bid.proposal,
        Status = bid.status,
        CreatedAt = bid.createdAt,
    };

    private void VerifyNothingWasWritten()
    {
        _written.Should().BeNull();
        _bids.Verify(
            repository => repository.AddAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
