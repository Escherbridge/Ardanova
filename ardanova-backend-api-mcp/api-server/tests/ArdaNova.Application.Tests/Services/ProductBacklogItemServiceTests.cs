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

public class ProductBacklogItemServiceTests
{
    private readonly Mock<IRepository<ProductBacklogItem>> _repositoryMock;
    private readonly Mock<IRepository<Feature>> _featureRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ProductBacklogItemService _sut;

    public ProductBacklogItemServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ProductBacklogItem>>();
        _featureRepositoryMock = new Mock<IRepository<Feature>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new ProductBacklogItemService(
            _repositoryMock.Object,
            _featureRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenItemExists_ReturnsSuccessResult()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        var item = new ProductBacklogItem
        {
            id = itemId,
            featureId = Guid.NewGuid().ToString(),
            title = "Test PBI",
            type = PBIType.FEATURE,
            status = PBIStatus.NEW,
            priority = Priority.MEDIUM,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var itemDto = new ProductBacklogItemDto { Id = itemId, Title = "Test PBI" };

        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(item)).Returns(itemDto);

        // Act
        var result = await _sut.GetByIdAsync(itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test PBI");
    }

    [Fact]
    public async Task GetByIdAsync_WhenItemNotExists_ReturnsNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductBacklogItem?)null);

        // Act
        var result = await _sut.GetByIdAsync(itemId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedItem()
    {
        // Arrange
        var dto = new CreateProductBacklogItemDto
        {
            FeatureId = Guid.NewGuid().ToString(),
            Title = "New PBI",
            Description = "A new backlog item",
            Type = PBIType.FEATURE,
            Priority = Priority.HIGH,
            StoryPoints = 5
        };
        var itemDto = new ProductBacklogItemDto { Title = "New PBI" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ProductBacklogItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductBacklogItem i, CancellationToken _) => i);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(It.IsAny<ProductBacklogItem>())).Returns(itemDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New PBI");
    }

    [Fact]
    public async Task CreateAsync_SetsDefaultStatusToNew()
    {
        // Arrange
        var dto = new CreateProductBacklogItemDto
        {
            FeatureId = Guid.NewGuid().ToString(),
            Title = "New PBI"
        };

        ProductBacklogItem? capturedItem = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ProductBacklogItem>(), It.IsAny<CancellationToken>()))
            .Callback<ProductBacklogItem, CancellationToken>((item, _) => capturedItem = item)
            .ReturnsAsync((ProductBacklogItem i, CancellationToken _) => i);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(It.IsAny<ProductBacklogItem>()))
            .Returns(new ProductBacklogItemDto { Title = "New PBI" });

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        capturedItem.Should().NotBeNull();
        capturedItem!.status.Should().Be(PBIStatus.NEW);
    }

    [Fact]
    public async Task UpdateAsync_WhenItemExists_ReturnsUpdatedItem()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        var item = new ProductBacklogItem
        {
            id = itemId,
            featureId = Guid.NewGuid().ToString(),
            title = "Old Title",
            type = PBIType.FEATURE,
            status = PBIStatus.NEW,
            priority = Priority.MEDIUM,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new UpdateProductBacklogItemDto { Title = "Updated Title" };
        var itemDto = new ProductBacklogItemDto { Id = itemId, Title = "Updated Title" };

        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBacklogItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(It.IsAny<ProductBacklogItem>())).Returns(itemDto);

        // Act
        var result = await _sut.UpdateAsync(itemId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_WhenItemNotExists_ReturnsNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductBacklogItem?)null);

        // Act
        var result = await _sut.UpdateAsync(itemId, new UpdateProductBacklogItemDto { Title = "Test" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenItemExists_ReturnsSuccess()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        var item = new ProductBacklogItem
        {
            id = itemId,
            featureId = Guid.NewGuid().ToString(),
            title = "Test PBI",
            type = PBIType.FEATURE,
            status = PBIStatus.NEW,
            priority = Priority.MEDIUM,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repositoryMock.Setup(r => r.DeleteAsync(item, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenItemNotExists_ReturnsNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductBacklogItem?)null);

        // Act
        var result = await _sut.DeleteAsync(itemId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task AssignAsync_WhenItemExists_UpdatesAssignee()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var item = new ProductBacklogItem
        {
            id = itemId,
            featureId = Guid.NewGuid().ToString(),
            title = "Test PBI",
            type = PBIType.FEATURE,
            status = PBIStatus.NEW,
            priority = Priority.MEDIUM,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var itemDto = new ProductBacklogItemDto { Id = itemId, AssigneeId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBacklogItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(It.IsAny<ProductBacklogItem>())).Returns(itemDto);

        // Act
        var result = await _sut.AssignAsync(itemId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.assigneeId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenItemExists_UpdatesStatus()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        var item = new ProductBacklogItem
        {
            id = itemId,
            featureId = Guid.NewGuid().ToString(),
            title = "Test PBI",
            type = PBIType.FEATURE,
            status = PBIStatus.NEW,
            priority = Priority.MEDIUM,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var itemDto = new ProductBacklogItemDto { Id = itemId, Status = PBIStatus.IN_PROGRESS };

        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBacklogItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(It.IsAny<ProductBacklogItem>())).Returns(itemDto);

        // Act
        var result = await _sut.UpdateStatusAsync(itemId, PBIStatus.IN_PROGRESS);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.status.Should().Be(PBIStatus.IN_PROGRESS);
    }

    [Fact]
    public async Task UpdateAsync_WithPriorityChange_UpdatesPriority()
    {
        // Arrange
        var itemId = Guid.NewGuid().ToString();
        var item = new ProductBacklogItem
        {
            id = itemId,
            featureId = Guid.NewGuid().ToString(),
            title = "Test PBI",
            type = PBIType.FEATURE,
            status = PBIStatus.NEW,
            priority = Priority.LOW,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new UpdateProductBacklogItemDto { Priority = Priority.CRITICAL };
        var itemDto = new ProductBacklogItemDto { Id = itemId, Priority = Priority.CRITICAL };

        _repositoryMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBacklogItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<ProductBacklogItemDto>(It.IsAny<ProductBacklogItem>())).Returns(itemDto);

        // Act
        var result = await _sut.UpdateAsync(itemId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.priority.Should().Be(Priority.CRITICAL);
    }
}
