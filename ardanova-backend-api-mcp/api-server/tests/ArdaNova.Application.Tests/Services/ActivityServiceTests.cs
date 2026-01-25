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

public class ActivityServiceTests
{
    private readonly Mock<IRepository<Activity>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ActivityService _sut;

    public ActivityServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Activity>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new ActivityService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenActivityExists_ReturnsSuccessResult()
    {
        // Arrange
        var activityId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var activity = new Activity
        {
            id = activityId,
            userId = userId,
            type = ActivityType.CREATED,
            entityType = "Project",
            entityId = "1",
            action = "Created a new project",
            createdAt = DateTime.UtcNow
        };
        var activityDto = new ActivityDto { Id = activityId, UserId = userId, Action = "Created a new project" };

        _repositoryMock.Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);
        _mapperMock.Setup(m => m.Map<ActivityDto>(activity)).Returns(activityDto);

        // Act
        var result = await _sut.GetByIdAsync(activityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Action.Should().Be("Created a new project");
    }

    [Fact]
    public async Task GetByIdAsync_WhenActivityNotExists_ReturnsNotFound()
    {
        // Arrange
        var activityId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity?)null);

        // Act
        var result = await _sut.GetByIdAsync(activityId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsActivitiesForUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var activities = new List<Activity>
        {
            new Activity { id = Guid.NewGuid().ToString(), userId = userId, type = ActivityType.CREATED, entityType = "Project", entityId = "1", action = "Created project", createdAt = DateTime.UtcNow },
            new Activity { id = Guid.NewGuid().ToString(), userId = userId, type = ActivityType.COMPLETED, entityType = "Task", entityId = "2", action = "Completed task", createdAt = DateTime.UtcNow }
        };
        var activityDtos = new List<ActivityDto>
        {
            new ActivityDto { UserId = userId, Action = "Created project" },
            new ActivityDto { UserId = userId, Action = "Completed task" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Activity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ActivityDto>>(It.IsAny<IEnumerable<Activity>>())).Returns(activityDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsActivitiesForProject()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var activities = new List<Activity>
        {
            new Activity { id = Guid.NewGuid().ToString(), userId = userId, type = ActivityType.CREATED, entityType = "Project", entityId = projectId, action = "Created", projectId = projectId, createdAt = DateTime.UtcNow },
            new Activity { id = Guid.NewGuid().ToString(), userId = userId, type = ActivityType.UPDATED, entityType = "Task", entityId = "1", action = "Assigned", projectId = projectId, createdAt = DateTime.UtcNow }
        };
        var activityDtos = new List<ActivityDto>
        {
            new ActivityDto { ProjectId = projectId, Action = "Created" },
            new ActivityDto { ProjectId = projectId, Action = "Assigned" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Activity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ActivityDto>>(It.IsAny<IEnumerable<Activity>>())).Returns(activityDtos);

        // Act
        var result = await _sut.GetByProjectIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedActivity()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateActivityDto
        {
            UserId = userId,
            Type = ActivityType.CREATED,
            EntityType = "Project",
            EntityId = "1",
            Action = "Created a new project"
        };
        var activityDto = new ActivityDto { UserId = userId, Action = "Created a new project" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Activity a, CancellationToken _) => a);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ActivityDto>(It.IsAny<Activity>())).Returns(activityDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Action.Should().Be("Created a new project");
    }
}
