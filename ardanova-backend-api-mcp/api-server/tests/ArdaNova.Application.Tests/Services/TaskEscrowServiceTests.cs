namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class TaskEscrowServiceTests
{
    private readonly Mock<IRepository<TaskEscrow>> _repositoryMock;
    private readonly Mock<IRepository<ProjectTask>> _taskRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TaskEscrowService _sut;

    public TaskEscrowServiceTests()
    {
        _repositoryMock = new Mock<IRepository<TaskEscrow>>();
        _taskRepositoryMock = new Mock<IRepository<ProjectTask>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new TaskEscrowService(
            _repositoryMock.Object,
            _taskRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEscrowExists_ReturnsSuccessResult()
    {
        // Arrange
        var escrowId = Guid.NewGuid().ToString();
        var taskId = Guid.NewGuid().ToString();
        var funderId = Guid.NewGuid().ToString();
        var tokenId = Guid.NewGuid().ToString();
        var escrow = new TaskEscrow
        {
            id = escrowId,
            taskId = taskId,
            funderId = funderId,
            shareId = tokenId,
            amount = 100m,
            status = EscrowStatus.FUNDED,
            createdAt = DateTime.UtcNow
        };
        var escrowDto = new TaskEscrowDto { Id = escrowId, TaskId = taskId, FunderId = funderId, Amount = 100m };

        _repositoryMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrow);
        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(escrow)).Returns(escrowDto);

        // Act
        var result = await _sut.GetByIdAsync(escrowId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEscrowNotExists_ReturnsNotFound()
    {
        // Arrange
        var escrowId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskEscrow?)null);

        // Act
        var result = await _sut.GetByIdAsync(escrowId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByTaskIdAsync_WhenEscrowExists_ReturnsEscrow()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var funderId = Guid.NewGuid().ToString();
        var tokenId = Guid.NewGuid().ToString();
        var escrow = new TaskEscrow
        {
            id = Guid.NewGuid().ToString(),
            taskId = taskId,
            funderId = funderId,
            shareId = tokenId,
            amount = 200m,
            status = EscrowStatus.FUNDED,
            createdAt = DateTime.UtcNow
        };
        var escrowDto = new TaskEscrowDto { TaskId = taskId, Amount = 200m };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TaskEscrow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrow);
        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(escrow)).Returns(escrowDto);

        // Act
        var result = await _sut.GetByTaskIdAsync(taskId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task CreateAsync_WithoutTheVerificationJoins_FailsClosedWithoutWriting()
    {
        // A service with no project/agreement/allocation/share joins cannot verify the task, the
        // funding instrument, the amount or the reservation behind them, so it refuses by name
        // rather than opening a record that asserts money is involved.
        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TaskEscrow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new CreateTaskEscrowDto
        {
            TaskId = Guid.NewGuid().ToString(),
            FunderId = Guid.NewGuid().ToString(),
            ShareId = Guid.NewGuid().ToString(),
            Amount = 500m
        });

        result.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<TaskEscrow>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTaskEscrow_ReturnsValidationError()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var tokenId = Guid.NewGuid().ToString();
        var dto = new CreateTaskEscrowDto
        {
            TaskId = taskId,
            FunderId = Guid.NewGuid().ToString(),
            ShareId = tokenId,
            Amount = 100m
        };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TaskEscrow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task ReleaseAsync_FailsClosedWithoutReadingOrMutatingEscrow()
    {
        var result = await _sut.ReleaseAsync("escrow-1", new ReleaseEscrowDto { ReleasedByUserId = "actor-1" });

        result.IsFailure.Should().BeTrue();
        _repositoryMock.VerifyNoOtherCalls();
        _taskRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisputeAsync_WhenEscrowExists_DisputesEscrow()
    {
        // Arrange
        var escrowId = Guid.NewGuid().ToString();
        var taskId = Guid.NewGuid().ToString();
        var funderId = Guid.NewGuid().ToString();
        var tokenId = Guid.NewGuid().ToString();
        var escrow = new TaskEscrow
        {
            id = escrowId,
            taskId = taskId,
            funderId = funderId,
            shareId = tokenId,
            amount = 100m,
            status = EscrowStatus.FUNDED,
            createdAt = DateTime.UtcNow
        };
        var escrowDto = new TaskEscrowDto { Id = escrowId, Status = EscrowStatus.DISPUTED };

        _repositoryMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrow);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskEscrow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var task = new ProjectTask { id = taskId, escrowStatus = EscrowStatus.FUNDED };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(It.IsAny<TaskEscrow>())).Returns(escrowDto);

        // Act
        var result = await _sut.DisputeAsync(escrowId, new DisputeEscrowDto
        {
            Reason = "scope_dispute",
            Description = "The requested deliverable materially exceeded the agreed scope.",
            DisputedByUserId = funderId
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(EscrowStatus.DISPUTED);
        task.escrowStatus.Should().Be(EscrowStatus.DISPUTED);
        escrow.disputeReason.Should().Be("SCOPE_DISPUTE");
        escrow.disputeDescription.Should().Be("The requested deliverable materially exceeded the agreed scope.");
        escrow.disputedByUserId.Should().Be(funderId);
        escrow.disputedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("UNKNOWN", "The narrative is long enough to otherwise be accepted.")]
    [InlineData("OTHER", "Too short")]
    public async Task DisputeAsync_WithInvalidContext_ReturnsValidationError(string reason, string description)
    {
        var result = await _sut.DisputeAsync("escrow-1", new DisputeEscrowDto
        {
            Reason = reason,
            Description = description,
            DisputedByUserId = "actor-1"
        });

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        _repositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefundAsync_FailsClosedWithoutReadingOrMutatingEscrow()
    {
        var result = await _sut.RefundAsync("escrow-1", new RefundEscrowDto { RefundedByUserId = "actor-1" });

        result.IsFailure.Should().BeTrue();
        _repositoryMock.VerifyNoOtherCalls();
        _taskRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    // -- The lifecycle entry (OI-42) --------------------------------------------------------
    // Before this, every state after NONE required FUNDED and nothing could write FUNDED except
    // a dispute rejection, which itself required FUNDED. These assert the entry on the STORED
    // escrow rather than the DTO the service echoes back, so no test can pass on its own fixture.

    [Fact]
    public async Task Lifecycle_CreateThenFundThenRelease_CarriesTheEscrowIntoFundedAndOut()
    {
        var harness = new LifecycleHarness();

        var created = await harness.Service.CreateAsync(new CreateTaskEscrowDto
        {
            TaskId = LifecycleHarness.TaskId,
            FunderId = LifecycleHarness.OwnerId,
            // Deliberately wrong: both are derived server-side and must be ignored.
            ShareId = "not-this-share",
            Amount = 999_999m
        });

        created.IsSuccess.Should().BeTrue();
        harness.Stored.Should().NotBeNull();
        var escrow = harness.Stored!;
        escrow.status.Should().Be(EscrowStatus.NONE);
        escrow.shareId.Should().Be(LifecycleHarness.ShareId);
        escrow.amount.Should().Be(LifecycleHarness.AwardAmount);
        escrow.funderId.Should().Be(LifecycleHarness.OwnerId);

        var funded = await harness.Service.FundAsync(escrow.id, LifecycleHarness.OwnerId);

        funded.IsSuccess.Should().BeTrue();
        escrow.status.Should().Be(EscrowStatus.FUNDED);
        escrow.fundedAt.Should().NotBeNull();
        harness.ProjectTaskRow.escrowStatus.Should().Be(EscrowStatus.FUNDED);

        var released = await harness.Service.ReleaseAsync(
            escrow.id,
            new ReleaseEscrowDto { ReleasedByUserId = LifecycleHarness.OwnerId });

        released.IsSuccess.Should().BeTrue();
        escrow.status.Should().Be(EscrowStatus.RELEASED);
        harness.Awards.Verify(
            awards => awards.ApproveWorkCompletionAsync(
                LifecycleHarness.AllocationId,
                LifecycleHarness.OwnerId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FundAsync_WhenTheNodeHasNotCommittedTheUnits_LeavesTheEscrowUnfunded()
    {
        var harness = new LifecycleHarness(holdState: AzoaHoldStates.Pending);
        await harness.CreateAsOwnerAsync();

        var escrow = harness.Stored!;

        var funded = await harness.Service.FundAsync(escrow.id, LifecycleHarness.OwnerId);

        funded.IsSuccess.Should().BeFalse();
        funded.Type.Should().Be(ResultType.Conflict);
        escrow.status.Should().Be(EscrowStatus.NONE);
        escrow.fundedAt.Should().BeNull();
        harness.ProjectTaskRow.escrowStatus.Should().Be(EscrowStatus.NONE);
    }

    [Fact]
    public async Task FundAsync_WhenTheActorIsNotTheFunder_IsForbiddenAndNothingMoves()
    {
        var harness = new LifecycleHarness();
        await harness.CreateAsOwnerAsync();

        var escrow = harness.Stored!;

        var funded = await harness.Service.FundAsync(escrow.id, "someone-else");

        funded.Type.Should().Be(ResultType.Forbidden);
        escrow.status.Should().Be(EscrowStatus.NONE);
        harness.Holds.Verify(
            holds => holds.GetAsync(It.IsAny<AzoaHoldReference>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTheActorDidNotCreateTheProject_IsForbiddenAndWritesNothing()
    {
        var harness = new LifecycleHarness();

        var created = await harness.Service.CreateAsync(new CreateTaskEscrowDto
        {
            TaskId = LifecycleHarness.TaskId,
            FunderId = "someone-else",
            ShareId = LifecycleHarness.ShareId,
            Amount = LifecycleHarness.AwardAmount
        });

        created.Type.Should().Be(ResultType.Forbidden);
        harness.Stored.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenTheProjectHasNoShareOfRecord_RefusesByNameAndWritesNothing()
    {
        // The escrow row carries a NOT NULL foreign key to ProjectShare and nothing writes one.
        // The refusal is the honest shape of that seam; see NoShareOfRecordError.
        var harness = new LifecycleHarness(projectHasShare: false);

        var created = await harness.CreateAsOwnerAsync();

        created.Type.Should().Be(ResultType.Conflict);
        created.Error.Should().Contain("share of record");
        harness.Stored.Should().BeNull();
    }

    [Fact]
    public async Task DisputeAsync_FromAnEscrowFundedThroughTheNode_ReachesDisputedAndBack()
    {
        var harness = new LifecycleHarness();
        await harness.CreateAsOwnerAsync();
        var escrow = harness.Stored!;
        await harness.Service.FundAsync(escrow.id, LifecycleHarness.OwnerId);

        var disputed = await harness.Service.DisputeAsync(escrow.id, new DisputeEscrowDto
        {
            Reason = "non_delivery",
            Description = "The agreed deliverable was never handed over, and messages went unanswered.",
            DisputedByUserId = LifecycleHarness.OwnerId
        });

        disputed.IsSuccess.Should().BeTrue();
        escrow.status.Should().Be(EscrowStatus.DISPUTED);
        escrow.disputeReason.Should().Be("NON_DELIVERY");
        harness.ProjectTaskRow.escrowStatus.Should().Be(EscrowStatus.DISPUTED);

        var resolved = await harness.Service.ResolveAsync(escrow.id, new ResolveDisputeDto
        {
            Outcome = "REJECT",
            ResolvedByUserId = "administrator-1"
        });

        resolved.IsSuccess.Should().BeTrue();
        escrow.status.Should().Be(EscrowStatus.FUNDED);
    }

    [Fact]
    public void TaskEscrowService_ExposesTheFundingSurfaceTheControllerResolves()
    {
        // TaskEscrowsController reaches FundAsync by asking the instance the container resolved
        // for ITaskEscrowService for its funding surface. If these ever stop being one type the
        // fund route 501s and the lifecycle has no entry again.
        typeof(TaskEscrowService).Should().BeAssignableTo<ITaskEscrowService>();
        typeof(TaskEscrowService).Should().BeAssignableTo<ITaskEscrowFunding>();
    }

    /// <summary>A fully wired service over in-memory doubles, so a test can run a real lifecycle.</summary>
    private sealed class LifecycleHarness
    {
        internal const string ProjectId = "project-1";
        internal const string OwnerId = "owner-1";
        internal const string TaskId = "task-1";
        internal const string ShareId = "share-1";
        internal const string AgreementId = "agreement-1";
        internal const string AllocationId = "allocation-1";
        internal const string HoldChain = "Algorand";
        internal const string HoldId = "award-bid-1";
        internal const decimal AwardAmount = 500m;

        internal TaskEscrow? Stored { get; private set; }

        internal ProjectTask ProjectTaskRow { get; } = new()
        {
            id = TaskId,
            projectId = ProjectId,
            escrowStatus = EscrowStatus.NONE
        };

        internal Mock<IAzoaHoldGateway> Holds { get; } = new();

        internal Mock<IProjectTokenProvisioning> Awards { get; } = new();

        internal TaskEscrowService Service { get; }

        internal LifecycleHarness(
            string holdState = AzoaHoldStates.Held,
            bool projectHasShare = true)
        {
            var escrows = new Mock<IRepository<TaskEscrow>>();
            escrows.Setup(repository => repository.ExistsAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<TaskEscrow, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Stored is not null);
            escrows.Setup(repository => repository.AddAsync(
                    It.IsAny<TaskEscrow>(),
                    It.IsAny<CancellationToken>()))
                .Callback<TaskEscrow, CancellationToken>((escrow, _) => Stored = escrow)
                .ReturnsAsync((TaskEscrow escrow, CancellationToken _) => escrow);
            escrows.Setup(repository => repository.GetByIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Stored!);
            escrows.Setup(repository => repository.UpdateAsync(
                    It.IsAny<TaskEscrow>(),
                    It.IsAny<CancellationToken>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);

            var tasks = new Mock<IRepository<ProjectTask>>();
            tasks.Setup(repository => repository.GetByIdAsync(TaskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProjectTaskRow);
            tasks.Setup(repository => repository.UpdateAsync(
                    It.IsAny<ProjectTask>(),
                    It.IsAny<CancellationToken>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);

            var projects = new Mock<IRepository<Project>>();
            projects.Setup(repository => repository.GetByIdAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Project { id = ProjectId, createdById = OwnerId });

            var agreements = new Mock<IRepository<TaskCommerceAgreement>>();
            agreements.Setup(repository => repository.FindOneAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<TaskCommerceAgreement, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TaskCommerceAgreement
                {
                    id = AgreementId,
                    projectId = ProjectId,
                    taskId = TaskId,
                    awardAmount = AwardAmount
                });

            var allocations = new Mock<IRepository<TokenAllocation>>();
            allocations.Setup(repository => repository.FindOneAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<TokenAllocation, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TokenAllocation
                {
                    id = AllocationId,
                    taskCommerceAgreementId = AgreementId,
                    holdChain = HoldChain,
                    holdId = HoldId
                });

            var shares = new Mock<IRepository<ProjectShare>>();
            shares.Setup(repository => repository.FindOneAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<ProjectShare, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(projectHasShare
                    ? new ProjectShare { id = ShareId, projectId = ProjectId }
                    : (ProjectShare?)null);

            Holds.Setup(holds => holds.GetAsync(
                    It.IsAny<AzoaHoldReference>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AzoaHold>.Success(new AzoaHold(
                    HoldId,
                    HoldChain,
                    "asset-1",
                    "50000",
                    "contributor",
                    null,
                    holdState,
                    null,
                    false,
                    null,
                    null)));

            Awards.Setup(awards => awards.ApproveWorkCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenAllocationDto>.Success(new TokenAllocationDto()));

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapper = new Mock<IMapper>();
            mapper.Setup(instance => instance.Map<TaskEscrowDto>(It.IsAny<TaskEscrow>()))
                .Returns((object source) => ToDto((TaskEscrow)source));

            Service = new TaskEscrowService(
                escrows.Object,
                tasks.Object,
                unitOfWork.Object,
                mapper.Object,
                agreements.Object,
                allocations.Object,
                new[] { Holds.Object },
                Awards.Object,
                projects.Object,
                shares.Object);
        }

        /// <summary>Opens the escrow the way the controller does: task named, everything else derived.</summary>
        internal System.Threading.Tasks.Task<Result<TaskEscrowDto>> CreateAsOwnerAsync()
            => Service.CreateAsync(new CreateTaskEscrowDto
            {
                TaskId = TaskId,
                FunderId = OwnerId,
                ShareId = string.Empty,
                Amount = 0m
            });

        private static TaskEscrowDto ToDto(TaskEscrow escrow) => new()
        {
            Id = escrow.id,
            TaskId = escrow.taskId,
            FunderId = escrow.funderId,
            ShareId = escrow.shareId,
            Amount = escrow.amount,
            Status = escrow.status,
            DisputeReason = escrow.disputeReason,
            DisputeDescription = escrow.disputeDescription,
            DisputedByUserId = escrow.disputedByUserId,
            CreatedAt = escrow.createdAt,
            FundedAt = escrow.fundedAt,
            ReleasedAt = escrow.releasedAt,
            RefundedAt = escrow.refundedAt,
            DisputedAt = escrow.disputedAt
        };
    }
}
