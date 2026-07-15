namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

public class TaskCommerceServiceTests
{
    private readonly Mock<IRepository<OpportunityBid>> _bids = new();
    private readonly Mock<IRepository<Opportunity>> _opportunities = new();
    private readonly Mock<IRepository<Project>> _projects = new();
    private readonly Mock<IRepository<ProjectTokenConfig>> _tokenConfigs = new();
    private readonly Mock<IRepository<ProjectTask>> _tasks = new();
    private readonly Mock<IRepository<TaskCommerceAgreement>> _agreements = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TaskCommerceService _sut;

    public TaskCommerceServiceTests()
    {
        _sut = new TaskCommerceService(
            _bids.Object,
            _opportunities.Object,
            _projects.Object,
            _tokenConfigs.Object,
            _tasks.Object,
            _agreements.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task AcceptBidAsync_CreatesOneLocalTaskAndAgreementWithoutSettlementEffects()
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
        createdAgreement.awardAmount.Should().Be(25.5m);
        createdAgreement.scale.Should().Be(6);
        createdAgreement.acceptedTermsSnapshot.Should().Contain("schemaVersion");
        createdAgreement.termsHash.Should().HaveLength(64);
        _unitOfWork.Verify(work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task AcceptBidAsync_ExactReplayReturnsTheOriginalAgreementWithoutWrites()
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
        };
        _agreements.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TaskCommerceAgreement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agreement);
        _tasks.Setup(repository => repository.GetByIdAsync(task.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.AcceptBidAsync(fixture.Bid.id, fixture.Project.createdById);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaskId.Should().Be(task.id);
        result.Value.AgreementId.Should().Be(agreement.id);
        VerifyNoMutation();
    }

    [Fact]
    public async Task AcceptBidAsync_CompetingBidForTheSameOpportunityIsRejectedWithoutWrites()
    {
        var fixture = ArrangeEligibleBid();
        _tasks.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTask, bool>>>(),
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
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTokenConfig, bool>>>(),
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
        };
        _tasks.Setup(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask item, CancellationToken _) => item);
        _agreements.Setup(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskCommerceAgreement item, CancellationToken _) => item);
        _unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        _agreements.SetupSequence(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TaskCommerceAgreement, bool>>>(),
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
    }

    [Fact]
    public async Task GetByTaskIdAsync_ForeignActorCannotReadCommerceAgreement()
    {
        var task = new ProjectTask { id = "task-1", projectId = "project-1", assignedToId = "contributor-1" };
        _tasks.Setup(repository => repository.GetByIdAsync(task.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _agreements.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TaskCommerceAgreement, bool>>>(),
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
            proposedAmount = 25.5m,
            status = BidStatus.SUBMITTED,
        };
        _bids.Setup(repository => repository.GetByIdAsync(bid.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bid);
        _opportunities.Setup(repository => repository.GetByIdAsync(opportunity.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _projects.Setup(repository => repository.GetByIdAsync(project.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _agreements.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TaskCommerceAgreement, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskCommerceAgreement?)null);
        _tokenConfigs.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig { id = "config-1", projectId = project.id, unitName = "BLD", assetScale = 6 });
        _tasks.Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTask, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);
        _bids.Setup(repository => repository.UpdateAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (bid, opportunity, project);
    }

    private void VerifyNoMutation()
    {
        _tasks.Verify(repository => repository.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
        _agreements.Verify(repository => repository.AddAsync(It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()), Times.Never);
        _bids.Verify(repository => repository.UpdateAsync(It.IsAny<OpportunityBid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
