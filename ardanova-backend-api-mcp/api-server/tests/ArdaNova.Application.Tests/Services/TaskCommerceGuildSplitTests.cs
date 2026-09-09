namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

/// <summary>
/// A guild bid that declares its split is payable: acceptance opens one hold per beneficiary.
/// The assertions are on the holds themselves — how many, for whom, and that they add up to the
/// award — because an acceptance that reports success without committing every share is the
/// failure this pays for.
/// </summary>
public class TaskCommerceGuildSplitTests
{
    private const string GuildId = "guild-1";
    private const string GuildOwnerId = "guild-owner-1";
    private const string ContributorId = "contributor-1";

    private static readonly Guid ContributorAvatarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GuildOwnerAvatarId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Mock<IRepository<OpportunityBid>> _bids = new();
    private readonly Mock<IRepository<Opportunity>> _opportunities = new();
    private readonly Mock<IRepository<Project>> _projects = new();
    private readonly Mock<IRepository<ProjectTokenConfig>> _tokenConfigs = new();
    private readonly Mock<IRepository<ProjectTask>> _tasks = new();
    private readonly Mock<IRepository<TaskCommerceAgreement>> _agreements = new();
    private readonly Mock<IRepository<Guild>> _guilds = new();
    private readonly Mock<IAzoaValueEligibilityService> _valueEligibility = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProjectTokenProvisioning> _awards = new();
    private readonly List<ProjectAwardReservationRequest> _reservations = new();
    private readonly TaskCommerceService _sut;

    private ProjectTask? _createdTask;
    private TaskCommerceAgreement? _createdAgreement;

    public TaskCommerceGuildSplitTests()
    {
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, string _, CancellationToken _) =>
                Result<AzoaCustodialAccountStatusDto>.Success(ReadyAccountStatus(AvatarFor(userId))));
        _awards
            .Setup(service => service.ReserveWorkAwardAsync(
                It.IsAny<ProjectAwardReservationRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProjectAwardReservationRequest, CancellationToken>(
                (request, _) => _reservations.Add(request))
            .ReturnsAsync((ProjectAwardReservationRequest request, CancellationToken _) =>
                Result<ProjectAwardReservation>.Success(
                    new ProjectAwardReservation(
                        $"allocation-{_reservations.Count}",
                        request.HoldId,
                        AzoaHoldStates.Held,
                        (int)request.AwardAmount)));
        _guilds
            .Setup(repository => repository.GetByIdAsync(GuildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { id = GuildId, name = "The Guild", slug = "the-guild", ownerId = GuildOwnerId });
        _tasks
            .Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectTask, CancellationToken>((task, _) => _createdTask = task)
            .ReturnsAsync((ProjectTask task, CancellationToken _) => task);
        _agreements
            .Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .Callback<TaskCommerceAgreement, CancellationToken>((agreement, _) => _createdAgreement = agreement)
            .ReturnsAsync((TaskCommerceAgreement agreement, CancellationToken _) => agreement);
        _unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = BuildService(_guilds.Object);
    }

    [Fact]
    public async Task AcceptBidAsync_ATwoWaySplitOpensOneHoldPerBeneficiarySummingToTheAward()
    {
        var fixture = ArrangeEligibleBid(guildId: GuildId, guildAwardSplit: 10m);
        var escrowHoldId = ProjectAwardHold.EscrowIdFor(fixture.Bid.id);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();

        // Two holds, and between them exactly the award: not more, which would promise units
        // the project never committed, and not less, which would keep part of the worker's.
        _reservations.Should().HaveCount(2);
        _reservations.Sum(reservation => reservation.AwardAmount).Should().Be(25m);
        _reservations.Select(reservation => reservation.HoldId).Should().OnlyHaveUniqueItems();
        _reservations.Should().ContainSingle(reservation =>
            reservation.RecipientUserId == ContributorId
            && reservation.RecipientAvatarId == ContributorAvatarId
            && reservation.AwardAmount == 15m
            && reservation.HoldId == escrowHoldId);
        _reservations.Should().ContainSingle(reservation =>
            reservation.RecipientUserId == GuildOwnerId
            && reservation.RecipientAvatarId == GuildOwnerAvatarId
            && reservation.AwardAmount == 10m
            && reservation.HoldId == escrowHoldId + "-guild");

        _createdAgreement.Should().NotBeNull();
        _createdAgreement!.status.Should().Be(TaskCommerceAgreementStatus.ESCROW_FUNDED);
        _createdAgreement.awardAmount.Should().Be(25m);
        _createdAgreement.acceptedTermsSnapshot.Should().Contain("guildAwardSplit");
        _createdTask!.escrowStatus.Should().Be(EscrowStatus.FUNDED);
    }

    [Fact]
    public async Task AcceptBidAsync_AnUnsplitBidStillOpensOneHoldForTheWholeAward()
    {
        var fixture = ArrangeEligibleBid();

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();
        _reservations.Should().ContainSingle();
        _reservations[0].AwardAmount.Should().Be(25m);
        _reservations[0].RecipientUserId.Should().Be(ContributorId);
        _reservations[0].HoldId.Should().Be(ProjectAwardHold.EscrowIdFor(fixture.Bid.id));
    }

    [Fact]
    public async Task AcceptBidAsync_ASplitIsRefusedWhenTheGuildCannotBeNamedAndNothingIsHeld()
    {
        var fixture = ArrangeEligibleBid(guildId: GuildId, guildAwardSplit: 10m);
        var withoutGuildDirectory = BuildService(guildRepository: null);

        var result = await withoutGuildDirectory.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TaskCommerceService.GuildSplitUnpayable);
        VerifyNothingWasHeld();
    }

    [Fact]
    public async Task AcceptBidAsync_ASplitIsRefusedWhenTheGuildOwnerCannotReceiveItAndNothingIsHeld()
    {
        var fixture = ArrangeEligibleBid(guildId: GuildId, guildAwardSplit: 10m);
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                GuildOwnerId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden(
                "Complete your account setup before receiving paid work."));

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        VerifyNothingWasHeld();
    }

    [Fact]
    public async Task AcceptBidAsync_ASplitBiggerThanTheAwardIsRefusedAndNothingIsHeld()
    {
        var fixture = ArrangeEligibleBid(guildId: GuildId, guildAwardSplit: 25m);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(OpportunityBidService.GuildSplitOutOfRange);
        VerifyNothingWasHeld();
    }

    private TaskCommerceService BuildService(IRepository<Guild>? guildRepository) => new(
        _bids.Object,
        _opportunities.Object,
        _projects.Object,
        _tokenConfigs.Object,
        _tasks.Object,
        _agreements.Object,
        _valueEligibility.Object,
        _unitOfWork.Object,
        _awards.Object,
        allocationRepository: null,
        guildRepository: guildRepository);

    private (OpportunityBid Bid, Opportunity Opportunity, Project Project) ArrangeEligibleBid(
        string? guildId = null,
        decimal? guildAwardSplit = null)
    {
        var project = new Project { id = "project-1", createdById = "owner-1" };
        var opportunity = new Opportunity
        {
            id = "opportunity-1",
            projectId = project.id,
            posterId = "poster-1",
            title = "Build the thing",
            description = "Deliver the thing",
        };
        var bid = new OpportunityBid
        {
            id = "bid-1",
            opportunityId = opportunity.id,
            bidderId = ContributorId,
            guildId = guildId,
            proposal = "I can deliver it",
            proposedAmount = 25m,
            guildAwardSplit = guildAwardSplit,
            status = BidStatus.SUBMITTED,
        };
        _bids.Setup(repository => repository.GetByIdAsync(bid.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bid);
        _opportunities.Setup(repository => repository.GetByIdAsync(opportunity.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _projects.Setup(repository => repository.GetByIdAsync(project.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _agreements.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TaskCommerceAgreement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskCommerceAgreement?)null);
        _tokenConfigs.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProvisionedConfig(project.id));
        _tasks.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<ProjectTask, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);
        _bids.Setup(repository => repository.UpdateAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (bid, opportunity, project);
    }

    /// <summary>A stake whose three node legs are all on record, so acceptance reaches the split.</summary>
    private static ProjectTokenConfig ProvisionedConfig(string projectId) => new()
    {
        id = "config-1",
        projectId = projectId,
        unitName = "BLD",
        assetScale = 6,
        assetId = "asset-1",
        assetChain = "Algorand",
        treasuryAddress = "TREASURY-ADDRESS",
        partitionsDeclaredAt = DateTime.UtcNow,
        status = ProjectTokenStatus.ACTIVE,
        totalSupply = 1000,
        contributorPartitionSupply = 500,
        investorPartitionSupply = 500,
    };

    private static Guid AvatarFor(string userId)
        => string.Equals(userId, GuildOwnerId, StringComparison.Ordinal)
            ? GuildOwnerAvatarId
            : ContributorAvatarId;

    private static AzoaCustodialAccountStatusDto ReadyAccountStatus(Guid avatarId) => new()
    {
        AvatarId = avatarId.ToString(),
        WalletId = "wallet-1",
        WalletAddress = "wallet-address-1",
        KycStatus = AzoaKycStatus.Approved,
        IdentityReady = true,
        KycReady = true,
        WalletReady = true,
        Ready = true,
    };

    private void VerifyNothingWasHeld()
    {
        _reservations.Should().BeEmpty();
        _awards.Verify(
            service => service.ReserveWorkAwardAsync(
                It.IsAny<ProjectAwardReservationRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _agreements.Verify(
            repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _tasks.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
