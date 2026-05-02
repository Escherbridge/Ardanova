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

public class EpicServiceTests
{
    private readonly Mock<IRepository<Epic>> _repositoryMock;
    private readonly Mock<IRepository<ProjectMilestone>> _milestoneRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly EpicService _sut;

    public EpicServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Epic>>();
        _milestoneRepositoryMock = new Mock<IRepository<ProjectMilestone>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new EpicService(
            _repositoryMock.Object,
            _milestoneRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEpicExists_ReturnsSuccessResult()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var epic = new Epic
        {
            id = epicId,
            milestoneId = Guid.NewGuid().ToString(),
            title = "Test Epic",
            status = EpicStatus.PLANNED,
            priority = Priority.MEDIUM,
            progress = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var epicDto = new EpicDto { Id = epicId, Title = "Test Epic" };

        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _mapperMock.Setup(m => m.Map<EpicDto>(epic)).Returns(epicDto);

        // Act
        var result = await _sut.GetByIdAsync(epicId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Epic");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEpicNotExists_ReturnsNotFound()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Epic?)null);

        // Act
        var result = await _sut.GetByIdAsync(epicId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByMilestoneIdAsync_ReturnsEpics()
    {
        // Arrange
        var milestoneId = Guid.NewGuid().ToString();
        var epics = new List<Epic>
        {
            new Epic { id = Guid.NewGuid().ToString(), milestoneId = milestoneId, title = "Epic 1", status = EpicStatus.PLANNED, priority = Priority.HIGH, progress = 0, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var epicDtos = new List<EpicDto>
        {
            new EpicDto { Id = epics[0].id, Title = "Epic 1" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Epic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epics);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<EpicDto>>(epics)).Returns(epicDtos);

        // Act
        var result = await _sut.GetByMilestoneIdAsync(milestoneId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedEpic()
    {
        // Arrange
        var dto = new CreateEpicDto
        {
            MilestoneId = Guid.NewGuid().ToString(),
            Title = "New Epic",
            Description = "A new epic",
            Priority = Priority.HIGH
        };
        var epicDto = new EpicDto { Title = "New Epic" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Epic e, CancellationToken _) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<EpicDto>(It.IsAny<Epic>())).Returns(epicDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Epic");
    }

    [Fact]
    public async Task UpdateAsync_WhenEpicExists_ReturnsUpdatedEpic()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var epic = new Epic
        {
            id = epicId,
            milestoneId = Guid.NewGuid().ToString(),
            title = "Old Title",
            status = EpicStatus.PLANNED,
            priority = Priority.MEDIUM,
            progress = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new UpdateEpicDto { Title = "Updated Title" };
        var epicDto = new EpicDto { Id = epicId, Title = "Updated Title" };

        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<EpicDto>(It.IsAny<Epic>())).Returns(epicDto);

        // Act
        var result = await _sut.UpdateAsync(epicId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_WhenEpicNotExists_ReturnsNotFound()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Epic?)null);

        // Act
        var result = await _sut.UpdateAsync(epicId, new UpdateEpicDto { Title = "Test" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenEpicExists_ReturnsSuccess()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var epic = new Epic
        {
            id = epicId,
            milestoneId = Guid.NewGuid().ToString(),
            title = "Test Epic",
            status = EpicStatus.PLANNED,
            priority = Priority.MEDIUM,
            progress = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _repositoryMock.Setup(r => r.DeleteAsync(epic, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(epicId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenEpicNotExists_ReturnsNotFound()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Epic?)null);

        // Act
        var result = await _sut.DeleteAsync(epicId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task AssignAsync_WhenEpicExists_UpdatesAssignee()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var epic = new Epic
        {
            id = epicId,
            milestoneId = Guid.NewGuid().ToString(),
            title = "Test Epic",
            status = EpicStatus.PLANNED,
            priority = Priority.MEDIUM,
            progress = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var epicDto = new EpicDto { Id = epicId, AssigneeId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<EpicDto>(It.IsAny<Epic>())).Returns(epicDto);

        // Act
        var result = await _sut.AssignAsync(epicId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        epic.assigneeId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenEpicExists_UpdatesStatus()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var epic = new Epic
        {
            id = epicId,
            milestoneId = Guid.NewGuid().ToString(),
            title = "Test Epic",
            status = EpicStatus.PLANNED,
            priority = Priority.MEDIUM,
            progress = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var epicDto = new EpicDto { Id = epicId, Status = EpicStatus.IN_PROGRESS };

        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<EpicDto>(It.IsAny<Epic>())).Returns(epicDto);

        // Act
        var result = await _sut.UpdateStatusAsync(epicId, EpicStatus.IN_PROGRESS);

        // Assert
        result.IsSuccess.Should().BeTrue();
        epic.status.Should().Be(EpicStatus.IN_PROGRESS);
    }

    [Fact]
    public async Task UpdatePriorityAsync_WhenEpicExists_UpdatesPriority()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var epic = new Epic
        {
            id = epicId,
            milestoneId = Guid.NewGuid().ToString(),
            title = "Test Epic",
            status = EpicStatus.PLANNED,
            priority = Priority.MEDIUM,
            progress = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var epicDto = new EpicDto { Id = epicId, Priority = Priority.CRITICAL };

        _repositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<EpicDto>(It.IsAny<Epic>())).Returns(epicDto);

        // Act
        var result = await _sut.UpdatePriorityAsync(epicId, Priority.CRITICAL);

        // Assert
        result.IsSuccess.Should().BeTrue();
        epic.priority.Should().Be(Priority.CRITICAL);
    }
}
