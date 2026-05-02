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

public class SprintServiceTests
{
    private readonly Mock<IRepository<Sprint>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly SprintService _sut;

    public SprintServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Sprint>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new SprintService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSprintExists_ReturnsSuccessResult()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var sprint = new Sprint
        {
            id = sprintId,
            epicId = Guid.NewGuid().ToString(),
            name = "Sprint 1",
            status = SprintStatus.PLANNED,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(14),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var sprintDto = new SprintDto { Id = sprintId, Name = "Sprint 1" };

        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _mapperMock.Setup(m => m.Map<SprintDto>(sprint)).Returns(sprintDto);

        // Act
        var result = await _sut.GetByIdAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Sprint 1");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSprintNotExists_ReturnsNotFound()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var result = await _sut.GetByIdAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByEpicIdAsync_ReturnsSprints()
    {
        // Arrange
        var epicId = Guid.NewGuid().ToString();
        var sprints = new List<Sprint>
        {
            new Sprint { id = Guid.NewGuid().ToString(), epicId = epicId, name = "Sprint 1", status = SprintStatus.PLANNED, startDate = DateTime.UtcNow, endDate = DateTime.UtcNow.AddDays(14), createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Sprint { id = Guid.NewGuid().ToString(), epicId = epicId, name = "Sprint 2", status = SprintStatus.ACTIVE, startDate = DateTime.UtcNow, endDate = DateTime.UtcNow.AddDays(14), createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var sprintDtos = new List<SprintDto>
        {
            new SprintDto { Id = sprints[0].id, Name = "Sprint 1" },
            new SprintDto { Id = sprints[1].id, Name = "Sprint 2" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sprint, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprints);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<SprintDto>>(sprints)).Returns(sprintDtos);

        // Act
        var result = await _sut.GetByEpicIdAsync(epicId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedSprint()
    {
        // Arrange
        var dto = new CreateSprintDto
        {
            EpicId = Guid.NewGuid().ToString(),
            Name = "New Sprint",
            Goal = "Complete feature X"
        };
        var sprintDto = new SprintDto { Name = "New Sprint" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint s, CancellationToken _) => s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SprintDto>(It.IsAny<Sprint>())).Returns(sprintDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Sprint");
    }

    [Fact]
    public async Task UpdateAsync_WhenSprintExists_ReturnsUpdatedSprint()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var sprint = new Sprint
        {
            id = sprintId,
            epicId = Guid.NewGuid().ToString(),
            name = "Old Name",
            status = SprintStatus.PLANNED,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(14),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new UpdateSprintDto { Name = "Updated Name" };
        var sprintDto = new SprintDto { Id = sprintId, Name = "Updated Name" };

        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SprintDto>(It.IsAny<Sprint>())).Returns(sprintDto);

        // Act
        var result = await _sut.UpdateAsync(sprintId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_WhenSprintNotExists_ReturnsNotFound()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var result = await _sut.UpdateAsync(sprintId, new UpdateSprintDto { Name = "Test" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenSprintExists_ReturnsSuccess()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var sprint = new Sprint
        {
            id = sprintId,
            epicId = Guid.NewGuid().ToString(),
            name = "Test Sprint",
            status = SprintStatus.PLANNED,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(14),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _repositoryMock.Setup(r => r.DeleteAsync(sprint, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenSprintNotExists_ReturnsNotFound()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint?)null);

        // Act
        var result = await _sut.DeleteAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task StartAsync_WhenSprintExists_SetsActiveStatus()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var sprint = new Sprint
        {
            id = sprintId,
            epicId = Guid.NewGuid().ToString(),
            name = "Test Sprint",
            status = SprintStatus.PLANNED,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(14),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var sprintDto = new SprintDto { Id = sprintId, Status = SprintStatus.ACTIVE };

        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SprintDto>(It.IsAny<Sprint>())).Returns(sprintDto);

        // Act
        var result = await _sut.StartAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sprint.status.Should().Be(SprintStatus.ACTIVE);
    }

    [Fact]
    public async Task CompleteAsync_WhenSprintExists_SetsCompletedStatus()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var sprint = new Sprint
        {
            id = sprintId,
            epicId = Guid.NewGuid().ToString(),
            name = "Test Sprint",
            status = SprintStatus.ACTIVE,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(14),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var sprintDto = new SprintDto { Id = sprintId, Status = SprintStatus.COMPLETED };

        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SprintDto>(It.IsAny<Sprint>())).Returns(sprintDto);

        // Act
        var result = await _sut.CompleteAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sprint.status.Should().Be(SprintStatus.COMPLETED);
    }

    [Fact]
    public async Task AssignAsync_WhenSprintExists_UpdatesAssignee()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var sprint = new Sprint
        {
            id = sprintId,
            epicId = Guid.NewGuid().ToString(),
            name = "Test Sprint",
            status = SprintStatus.PLANNED,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddDays(14),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var sprintDto = new SprintDto { Id = sprintId, AssigneeId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SprintDto>(It.IsAny<Sprint>())).Returns(sprintDto);

        // Act
        var result = await _sut.AssignAsync(sprintId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sprint.assigneeId.Should().Be(userId);
    }
}
