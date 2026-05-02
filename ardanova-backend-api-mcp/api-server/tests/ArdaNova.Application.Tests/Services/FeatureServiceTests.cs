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

public class FeatureServiceTests
{
    private readonly Mock<IRepository<Feature>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly FeatureService _sut;

    public FeatureServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Feature>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new FeatureService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFeatureExists_ReturnsSuccessResult()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        var feature = new Feature
        {
            id = featureId,
            sprintId = Guid.NewGuid().ToString(),
            title = "Test Feature",
            status = FeatureStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var featureDto = new FeatureDto { Id = featureId, Title = "Test Feature" };

        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feature);
        _mapperMock.Setup(m => m.Map<FeatureDto>(feature)).Returns(featureDto);

        // Act
        var result = await _sut.GetByIdAsync(featureId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Feature");
    }

    [Fact]
    public async Task GetByIdAsync_WhenFeatureNotExists_ReturnsNotFound()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Feature?)null);

        // Act
        var result = await _sut.GetByIdAsync(featureId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetBySprintIdAsync_ReturnsFeatures()
    {
        // Arrange
        var sprintId = Guid.NewGuid().ToString();
        var features = new List<Feature>
        {
            new Feature { id = Guid.NewGuid().ToString(), sprintId = sprintId, title = "Feature 1", status = FeatureStatus.PLANNED, priority = Priority.HIGH, order = 1, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Feature { id = Guid.NewGuid().ToString(), sprintId = sprintId, title = "Feature 2", status = FeatureStatus.IN_PROGRESS, priority = Priority.MEDIUM, order = 2, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var featureDtos = new List<FeatureDto>
        {
            new FeatureDto { Id = features[0].id, Title = "Feature 1" },
            new FeatureDto { Id = features[1].id, Title = "Feature 2" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Feature, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(features);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<FeatureDto>>(features)).Returns(featureDtos);

        // Act
        var result = await _sut.GetBySprintIdAsync(sprintId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedFeature()
    {
        // Arrange
        var dto = new CreateFeatureDto
        {
            SprintId = Guid.NewGuid().ToString(),
            Title = "New Feature",
            Description = "A new feature",
            Priority = Priority.HIGH,
            Order = 1
        };
        var featureDto = new FeatureDto { Title = "New Feature" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Feature>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Feature f, CancellationToken _) => f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FeatureDto>(It.IsAny<Feature>())).Returns(featureDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Feature");
    }

    [Fact]
    public async Task UpdateAsync_WhenFeatureExists_ReturnsUpdatedFeature()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        var feature = new Feature
        {
            id = featureId,
            sprintId = Guid.NewGuid().ToString(),
            title = "Old Title",
            status = FeatureStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new UpdateFeatureDto { Title = "Updated Title" };
        var featureDto = new FeatureDto { Id = featureId, Title = "Updated Title" };

        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feature);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Feature>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FeatureDto>(It.IsAny<Feature>())).Returns(featureDto);

        // Act
        var result = await _sut.UpdateAsync(featureId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_WhenFeatureNotExists_ReturnsNotFound()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Feature?)null);

        // Act
        var result = await _sut.UpdateAsync(featureId, new UpdateFeatureDto { Title = "Test" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenFeatureExists_ReturnsSuccess()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        var feature = new Feature
        {
            id = featureId,
            sprintId = Guid.NewGuid().ToString(),
            title = "Test Feature",
            status = FeatureStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feature);
        _repositoryMock.Setup(r => r.DeleteAsync(feature, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(featureId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenFeatureNotExists_ReturnsNotFound()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Feature?)null);

        // Act
        var result = await _sut.DeleteAsync(featureId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task AssignAsync_WhenFeatureExists_UpdatesAssignee()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var feature = new Feature
        {
            id = featureId,
            sprintId = Guid.NewGuid().ToString(),
            title = "Test Feature",
            status = FeatureStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var featureDto = new FeatureDto { Id = featureId, AssigneeId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feature);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Feature>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FeatureDto>(It.IsAny<Feature>())).Returns(featureDto);

        // Act
        var result = await _sut.AssignAsync(featureId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        feature.assigneeId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenFeatureExists_UpdatesStatus()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        var feature = new Feature
        {
            id = featureId,
            sprintId = Guid.NewGuid().ToString(),
            title = "Test Feature",
            status = FeatureStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var featureDto = new FeatureDto { Id = featureId, Status = FeatureStatus.IN_PROGRESS };

        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feature);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Feature>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FeatureDto>(It.IsAny<Feature>())).Returns(featureDto);

        // Act
        var result = await _sut.UpdateStatusAsync(featureId, FeatureStatus.IN_PROGRESS);

        // Assert
        result.IsSuccess.Should().BeTrue();
        feature.status.Should().Be(FeatureStatus.IN_PROGRESS);
    }

    [Fact]
    public async Task UpdatePriorityAsync_WhenFeatureExists_UpdatesPriority()
    {
        // Arrange
        var featureId = Guid.NewGuid().ToString();
        var feature = new Feature
        {
            id = featureId,
            sprintId = Guid.NewGuid().ToString(),
            title = "Test Feature",
            status = FeatureStatus.PLANNED,
            priority = Priority.MEDIUM,
            order = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var featureDto = new FeatureDto { Id = featureId, Priority = Priority.CRITICAL };

        _repositoryMock.Setup(r => r.GetByIdAsync(featureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feature);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Feature>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FeatureDto>(It.IsAny<Feature>())).Returns(featureDto);

        // Act
        var result = await _sut.UpdatePriorityAsync(featureId, Priority.CRITICAL);

        // Assert
        result.IsSuccess.Should().BeTrue();
        feature.priority.Should().Be(Priority.CRITICAL);
    }
}
