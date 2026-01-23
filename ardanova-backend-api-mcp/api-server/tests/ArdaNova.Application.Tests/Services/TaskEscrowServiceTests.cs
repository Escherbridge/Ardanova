namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class TaskEscrowServiceTests
{
    private readonly Mock<IRepository<TaskEscrow>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TaskEscrowService _sut;

    public TaskEscrowServiceTests()
    {
        _repositoryMock = new Mock<IRepository<TaskEscrow>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new TaskEscrowService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEscrowExists_ReturnsSuccessResult()
    {
        // Arrange
        var escrowId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var funderId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var escrow = TaskEscrow.Create(taskId, funderId, tokenId, 100m);
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
        var escrowId = Guid.NewGuid();
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
        var taskId = Guid.NewGuid();
        var funderId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var escrow = TaskEscrow.Create(taskId, funderId, tokenId, 200m);
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
    public async Task CreateAsync_WithValidDto_ReturnsCreatedEscrow()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var funderId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var dto = new CreateTaskEscrowDto
        {
            TaskId = taskId,
            FunderId = funderId,
            TokenId = tokenId,
            Amount = 500m
        };
        var escrowDto = new TaskEscrowDto { TaskId = taskId, Amount = 500m, Status = EscrowStatus.PENDING };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TaskEscrow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskEscrow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskEscrow e, CancellationToken _) => e);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(It.IsAny<TaskEscrow>())).Returns(escrowDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTaskEscrow_ReturnsValidationError()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new CreateTaskEscrowDto
        {
            TaskId = taskId,
            FunderId = Guid.NewGuid(),
            TokenId = Guid.NewGuid(),
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
    public async Task ReleaseAsync_WhenEscrowExists_ReleasesEscrow()
    {
        // Arrange
        var escrowId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var funderId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var escrow = TaskEscrow.Create(taskId, funderId, tokenId, 100m);
        var escrowDto = new TaskEscrowDto { Id = escrowId, Status = EscrowStatus.RELEASED };

        _repositoryMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrow);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskEscrow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(It.IsAny<TaskEscrow>())).Returns(escrowDto);

        // Act
        var result = await _sut.ReleaseAsync(escrowId, new ReleaseEscrowDto { TxHash = "TX123" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(EscrowStatus.RELEASED);
    }

    [Fact]
    public async Task DisputeAsync_WhenEscrowExists_DisputesEscrow()
    {
        // Arrange
        var escrowId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var funderId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var escrow = TaskEscrow.Create(taskId, funderId, tokenId, 100m);
        var escrowDto = new TaskEscrowDto { Id = escrowId, Status = EscrowStatus.DISPUTED };

        _repositoryMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrow);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskEscrow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(It.IsAny<TaskEscrow>())).Returns(escrowDto);

        // Act
        var result = await _sut.DisputeAsync(escrowId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(EscrowStatus.DISPUTED);
    }

    [Fact]
    public async Task RefundAsync_WhenEscrowExists_RefundsEscrow()
    {
        // Arrange
        var escrowId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var funderId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var escrow = TaskEscrow.Create(taskId, funderId, tokenId, 100m);
        var escrowDto = new TaskEscrowDto { Id = escrowId, Status = EscrowStatus.REFUNDED };

        _repositoryMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escrow);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskEscrow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TaskEscrowDto>(It.IsAny<TaskEscrow>())).Returns(escrowDto);

        // Act
        var result = await _sut.RefundAsync(escrowId, new RefundEscrowDto { TxHash = "TX456" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(EscrowStatus.REFUNDED);
    }
}
