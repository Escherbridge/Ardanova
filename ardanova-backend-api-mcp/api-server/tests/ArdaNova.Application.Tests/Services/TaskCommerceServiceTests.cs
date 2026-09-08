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
using Microsoft.EntityFrameworkCore;
using Moq;

public class TaskCommerceServiceTests
{
    private static readonly Guid ContributorAvatarId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IRepository<OpportunityBid>> _bids = new();
    private readonly Mock<IRepository<Opportunity>> _opportunities = new();
    private readonly Mock<IRepository<Project>> _projects = new();
    private readonly Mock<IRepository<ProjectTokenConfig>> _tokenConfigs = new();
    private readonly Mock<IRepository<ProjectTask>> _tasks = new();
    private readonly Mock<IRepository<TaskCommerceAgreement>> _agreements = new();
    private readonly Mock<IAzoaValueEligibilityService> _valueEligibility = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProjectTokenProvisioning> _awards = new();
    private readonly TaskCommerceService _sut;

    public TaskCommerceServiceTests()
    {
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(ReadyAccountStatus()));
        _awards
            .Setup(service => service.ReserveWorkAwardAsync(
                It.IsAny<ProjectAwardReservationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAwardReservationRequest request, CancellationToken _) =>
                Result<ProjectAwardReservation>.Success(
                    new ProjectAwardReservation("allocation-1", request.HoldId, AzoaHoldStates.Held, 25)));
        _sut = new TaskCommerceService(
            _bids.Object,
            _opportunities.Object,
            _projects.Object,
            _tokenConfigs.Object,
            _tasks.Object,
            _agreements.Object,
            _valueEligibility.Object,
            _unitOfWork.Object,
            _awards.Object);
    }

    [Fact]
    public async Task AcceptBidAsync_SetsTheAwardAsideOnTheNodeAndFundsTheAssignment()
    {
        var fixture = ArrangeEligibleBid();
        ProjectTask? createdTask = null;
        TaskCommerceAgreement? createdAgreement = null;
        _tasks.Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectTask, CancellationToken>((task, _) => createdTask = task)
            .ReturnsAsync((ProjectTask task, CancellationToken _) => task);
        _agreements.Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .Callback<TaskCommerceAgreement, CancellationToken>((agreement, _) => createdAgreement = agreement)
            .ReturnsAsync((TaskCommerceAgreement agreement, CancellationToken _) => agreement);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CommerceUrl.Should().Be($"/tasks/{result.Value.TaskId}/commerce");
        createdTask.Should().NotBeNull();
        createdTask!.assignedToId.Should().Be(fixture.Bid.bidderId);
        createdTask.opportunityId.Should().Be(fixture.Opportunity.id);
        createdAgreement.Should().NotBeNull();
        createdAgreement!.taskId.Should().Be(createdTask.id);
        createdAgreement.bidId.Should().Be(fixture.Bid.id);
        createdAgreement.awardAmount.Should().Be(25m);
        createdAgreement.scale.Should().Be(6);
        createdAgreement.acceptedTermsSnapshot.Should().Contain("schemaVersion");
        createdAgreement.termsHash.Should().HaveLength(64);

        // The award collapse: the task quotes the agreement, never its own separate figure.
        createdTask.equityReward.Should().Be(createdAgreement.awardAmount);

        // The units are actually committed on the node before the assignment reads as funded.
        _awards.Verify(service => service.ReserveWorkAwardAsync(
            It.Is<ProjectAwardReservationRequest>(request =>
                request.ProjectTokenConfigId == "config-1"
                && request.AgreementId == createdAgreement.id
                && request.HoldId == ProjectAwardHold.EscrowIdFor(fixture.Bid.id)
                && request.AwardAmount == 25m
                && request.RecipientUserId == fixture.Bid.bidderId
                && request.RecipientAvatarId == ContributorAvatarId),
            It.IsAny<CancellationToken>()), Times.Once);
        createdAgreement.status.Should().Be(TaskCommerceAgreementStatus.ESCROW_FUNDED);
        createdTask.escrowStatus.Should().Be(EscrowStatus.FUNDED);

        _unitOfWork.Verify(work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(work => work.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(work => work.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptBidAsync_ForeignActorFailsClosedWithoutWrites()
    {
        var fixture = ArrangeEligibleBid();

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, "foreign-user");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_IncompleteProjectOwnerFailsClosedBeforeTokenOrTaskWrites()
    {
        var fixture = ArrangeEligibleBid();
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                fixture.Project.createdById,
                "accept paid project work",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Complete your account setup before accepting paid work."));

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.Forbidden);
        _tokenConfigs.Verify(repository => repository.FindOneAsync(
            It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_IncompleteContributorFailsClosedBeforeTokenOrTaskWrites()
    {
        var fixture = ArrangeEligibleBid();
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                fixture.Bid.bidderId,
                "receive a paid project assignment",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Complete your account setup before receiving paid work."));

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.Forbidden);
        _tokenConfigs.Verify(repository => repository.FindOneAsync(
            It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_ContributorWithNoAccountOnTheNodeFailsClosed()
    {
        var fixture = ArrangeEligibleBid();
        _valueEligibility
            .Setup(service => service.RequireReadyAsync(
                fixture.Bid.bidderId,
                "receive a paid project assignment",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(
                ReadyAccountStatus() with { AvatarId = null }));

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.Conflict);
        VerifyNoMutation();
        VerifyNoReservation();
    }

    [Fact]
    public async Task AcceptBidAsync_GuildBidIsRefusedBecauseAnAwardHasOneBeneficiary()
    {
        var fixture = ArrangeEligibleBid();
        fixture.Bid.guildId = "guild-1";

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(TaskCommerceService.GuildSplitUnsupported);
        VerifyNoMutation();
        VerifyNoReservation();
    }

    [Fact]
    public async Task AcceptBidAsync_ProjectWithUnfinishedFundingSetupCannotSetStakeAside()
    {
        var fixture = ArrangeEligibleBid();
        _tokenConfigs.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig
            {
                id = "config-1",
                projectId = fixture.Project.id,
                unitName = "BLD",
                assetScale = 6,
                // No assetId, no treasury, no declared allowances: nothing to draw from.
            });

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("funding setup is not finished");
        VerifyNoMutation();
        VerifyNoReservation();
    }

    [Fact]
    public async Task AcceptBidAsync_NodeRefusalLeavesTheAgreementAcceptedAndUnfunded()
    {
        var fixture = ArrangeEligibleBid();
        TaskCommerceAgreement? createdAgreement = null;
        ProjectTask? createdTask = null;
        _tasks.Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectTask, CancellationToken>((task, _) => createdTask = task)
            .ReturnsAsync((ProjectTask task, CancellationToken _) => task);
        _agreements.Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .Callback<TaskCommerceAgreement, CancellationToken>((agreement, _) => createdAgreement = agreement)
            .ReturnsAsync((TaskCommerceAgreement agreement, CancellationToken _) => agreement);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _awards
            .Setup(service => service.ReserveWorkAwardAsync(
                It.IsAny<ProjectAwardReservationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectAwardReservation>.ValidationError(
                "Partition 'contributor' would be oversubscribed: 25000000 base units requested, 0 available."));

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("oversubscribed");
        createdAgreement!.status.Should().Be(TaskCommerceAgreementStatus.ACCEPTED);
        createdTask!.escrowStatus.Should().Be(EscrowStatus.NONE);
    }

    [Fact]
    public async Task AcceptBidAsync_ExactReplayOfAFundedAssignmentReturnsTheOriginalWithoutWrites()
    {
        var fixture = ArrangeEligibleBid();
        var (task, agreement) = ArrangeExistingAssignment(fixture, TaskCommerceAgreementStatus.ESCROW_FUNDED);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaskId.Should().Be(task.id);
        result.Value.AgreementId.Should().Be(agreement.id);
        VerifyNoMutation();
        VerifyNoReservation();
    }

    [Fact]
    public async Task AcceptBidAsync_ReplayOfAnUnfundedAssignmentResumesTheReservation()
    {
        var fixture = ArrangeEligibleBid();
        var (task, agreement) = ArrangeExistingAssignment(fixture, TaskCommerceAgreementStatus.ACCEPTED);
        _tokenConfigs.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProvisionedConfig(fixture.Project.id));
        _tasks.Setup(repository => repository.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _agreements.Setup(repository => repository.UpdateAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();
        // The hold id is derived from the bid, so the resumed call addresses the SAME hold.
        _awards.Verify(service => service.ReserveWorkAwardAsync(
            It.Is<ProjectAwardReservationRequest>(request =>
                request.HoldId == ProjectAwardHold.EscrowIdFor(fixture.Bid.id)),
            It.IsAny<CancellationToken>()), Times.Once);
        agreement.status.Should().Be(TaskCommerceAgreementStatus.ESCROW_FUNDED);
        task.escrowStatus.Should().Be(EscrowStatus.FUNDED);
        _unitOfWork.Verify(work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptBidAsync_WithNoAwardGatewayLeavesNothingPromisedToTheWorker()
    {
        var unwired = new TaskCommerceService(
            _bids.Object,
            _opportunities.Object,
            _projects.Object,
            _tokenConfigs.Object,
            _tasks.Object,
            _agreements.Object,
            _valueEligibility.Object,
            _unitOfWork.Object);
        var fixture = ArrangeEligibleBid();
        TaskCommerceAgreement? createdAgreement = null;
        _tasks.Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask task, CancellationToken _) => task);
        _agreements.Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .Callback<TaskCommerceAgreement, CancellationToken>((agreement, _) => createdAgreement = agreement)
            .ReturnsAsync((TaskCommerceAgreement agreement, CancellationToken _) => agreement);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await unwired.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no shared-node award gateway");
        createdAgreement!.status.Should().Be(TaskCommerceAgreementStatus.ACCEPTED);
    }

    [Fact]
    public async Task AcceptBidAsync_CompetingBidForTheSameOpportunityIsRejectedWithoutWrites()
    {
        var fixture = ArrangeEligibleBid();
        _tasks.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<ProjectTask, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTask
            {
                id = "other-task",
                projectId = fixture.Project.id,
                opportunityId = fixture.Opportunity.id,
                assignedToId = "different-bidder",
            });

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_MissingAwardFailsBeforeTransactionOrWrites()
    {
        var fixture = ArrangeEligibleBid();
        fixture.Bid.proposedAmount = null;

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_UnsupportedTokenScaleFailsBeforeTransactionOrWrites()
    {
        var fixture = ArrangeEligibleBid();
        _tokenConfigs.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig
            {
                id = "config-1",
                projectId = fixture.Project.id,
                unitName = "BLD",
                assetScale = 19,
            });

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("valid asset scale");
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_ConcurrentUniqueViolationNormalizesAnExactReplay()
    {
        var fixture = ArrangeEligibleBid();
        var task = new ProjectTask
        {
            id = "task-1",
            projectId = fixture.Project.id,
            opportunityId = fixture.Opportunity.id,
            assignedToId = fixture.Bid.bidderId,
        };
        var agreement = new TaskCommerceAgreement
        {
            id = "agreement-1",
            bidId = fixture.Bid.id,
            projectId = fixture.Project.id,
            taskId = task.id,
            contributorUserId = fixture.Bid.bidderId,
            status = TaskCommerceAgreementStatus.ESCROW_FUNDED,
        };
        _tasks.Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask item, CancellationToken _) => item);
        _agreements.Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskCommerceAgreement item, CancellationToken _) => item);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        _agreements.SetupSequence(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TaskCommerceAgreement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskCommerceAgreement?)null)
            .ReturnsAsync(agreement);
        _tasks.Setup(repository => repository.GetByIdAsync(task.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AgreementId.Should().Be(agreement.id);
        _unitOfWork.Verify(work => work.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWork.Verify(work => work.ClearTrackedChanges(), Times.Once);
        _unitOfWork.Verify(work => work.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptBidAsync_SaveFailureRollsBackAndDoesNotReturnACommerceResult()
    {
        var fixture = ArrangeEligibleBid();
        _tasks.Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask item, CancellationToken _) => item);
        _agreements.Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskCommerceAgreement item, CancellationToken _) => item);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var action = () => _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.Verify(work => work.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWork.Verify(work => work.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoReservation();
    }

    [Fact]
    public async Task GetByTaskIdAsync_ForeignActorCannotReadCommerceAgreement()
    {
        var task = new ProjectTask { id = "task-1", projectId = "project-1", assignedToId = "contributor-1" };
        _tasks.Setup(repository => repository.GetByIdAsync(task.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _agreements.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TaskCommerceAgreement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskCommerceAgreement { id = "agreement-1", taskId = task.id, contributorUserId = "contributor-1" });
        _projects.Setup(repository => repository.GetByIdAsync(task.projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { id = task.projectId, createdById = "owner-1" });

        var result = await _sut.GetByTaskIdAsync(task.id, "foreign-user");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        VerifyNoMutation();
    }

    private (OpportunityBid Bid, Opportunity Opportunity, Project Project) ArrangeEligibleBid()
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
            bidderId = "contributor-1",
            proposal = "I can deliver it",
            proposedAmount = 25m,
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

    /// <summary>An agreement and task already on record for this bid, at the given stage.</summary>
    private (ProjectTask Task, TaskCommerceAgreement Agreement) ArrangeExistingAssignment(
        (OpportunityBid Bid, Opportunity Opportunity, Project Project) fixture,
        TaskCommerceAgreementStatus status)
    {
        var task = new ProjectTask
        {
            id = "task-1",
            projectId = fixture.Project.id,
            opportunityId = fixture.Opportunity.id,
            assignedToId = fixture.Bid.bidderId,
            escrowStatus = status == TaskCommerceAgreementStatus.ESCROW_FUNDED
                ? EscrowStatus.FUNDED
                : EscrowStatus.NONE,
        };
        var agreement = new TaskCommerceAgreement
        {
            id = "agreement-1",
            bidId = fixture.Bid.id,
            projectId = fixture.Project.id,
            taskId = task.id,
            contributorUserId = fixture.Bid.bidderId,
            projectTokenConfigId = "config-1",
            awardAmount = 25m,
            status = status,
        };
        _agreements.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TaskCommerceAgreement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agreement);
        _tasks.Setup(repository => repository.GetByIdAsync(task.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        return (task, agreement);
    }

    /// <summary>
    /// A stake whose three node legs are all on record. Without them the project is not
    /// fundable and acceptance is refused before it reaches the rule under test.
    /// </summary>
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

    private void VerifyNoMutation()
    {
        _tasks.Verify(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _tasks.Verify(repository => repository.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _agreements.Verify(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()), Times.Never);
        _bids.Verify(repository => repository.UpdateAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyNoReservation()
        => _awards.Verify(service => service.ReserveWorkAwardAsync(
            It.IsAny<ProjectAwardReservationRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);

    private static AzoaCustodialAccountStatusDto ReadyAccountStatus() => new()
    {
        AvatarId = ContributorAvatarId.ToString(),
        WalletId = "wallet-1",
        WalletAddress = "wallet-address-1",
        KycStatus = AzoaKycStatus.Approved,
        IdentityReady = true,
        KycReady = true,
        WalletReady = true,
        Ready = true,
    };
}
