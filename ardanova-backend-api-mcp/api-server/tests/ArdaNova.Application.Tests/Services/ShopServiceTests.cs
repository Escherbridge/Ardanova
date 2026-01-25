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

public class ShopServiceTests
{
    private readonly Mock<IRepository<Shop>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ShopService _sut;

    public ShopServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Shop>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new ShopService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenShopExists_ReturnsSuccessResult()
    {
        // Arrange
        var shopId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var shop = new Shop
        {
            id = shopId,
            ownerId = ownerId,
            name = "Test Shop",
            description = "A test shop",
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var shopDto = new ShopDto { Id = shopId, Name = "Test Shop" };

        _repositoryMock.Setup(r => r.GetByIdAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);
        _mapperMock.Setup(m => m.Map<ShopDto>(shop)).Returns(shopDto);

        // Act
        var result = await _sut.GetByIdAsync(shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Shop");
    }

    [Fact]
    public async Task GetByIdAsync_WhenShopNotExists_ReturnsNotFound()
    {
        // Arrange
        var shopId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var result = await _sut.GetByIdAsync(shopId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllShops()
    {
        // Arrange
        var ownerId = Guid.NewGuid().ToString();
        var shops = new List<Shop>
        {
            new Shop { id = Guid.NewGuid().ToString(), ownerId = ownerId, name = "Shop 1", description = "Desc 1", isActive = true, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Shop { id = Guid.NewGuid().ToString(), ownerId = ownerId, name = "Shop 2", description = "Desc 2", isActive = true, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var shopDtos = new List<ShopDto>
        {
            new ShopDto { Name = "Shop 1" },
            new ShopDto { Name = "Shop 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(shops);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ShopDto>>(shops)).Returns(shopDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedShop()
    {
        // Arrange
        var ownerId = Guid.NewGuid().ToString();
        var dto = new CreateShopDto
        {
            Name = "New Shop",
            OwnerId = ownerId,
            Description = "A new shop"
        };
        var shopDto = new ShopDto { Name = "New Shop" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Shop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop s, CancellationToken _) => s);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ShopDto>(It.IsAny<Shop>())).Returns(shopDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Shop");
    }

    [Fact]
    public async Task GetByOwnerIdAsync_ReturnsOwnerShops()
    {
        // Arrange
        var ownerId = Guid.NewGuid().ToString();
        var shops = new List<Shop>
        {
            new Shop { id = Guid.NewGuid().ToString(), ownerId = ownerId, name = "Owner Shop", description = "Desc", isActive = true, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var shopDtos = new List<ShopDto> { new ShopDto { Name = "Owner Shop" } };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Shop, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shops);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ShopDto>>(shops)).Returns(shopDtos);

        // Act
        var result = await _sut.GetByOwnerIdAsync(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenShopExists_ReturnsSuccess()
    {
        // Arrange
        var shopId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var shop = new Shop
        {
            id = shopId,
            ownerId = ownerId,
            name = "Test Shop",
            description = "Desc",
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock.Setup(r => r.DeleteAsync(shop, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpgradePlanAsync_WhenShopExists_ReturnsSuccess()
    {
        // Arrange
        var shopId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var shop = new Shop
        {
            id = shopId,
            ownerId = ownerId,
            name = "Test Shop",
            description = "Desc",
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var shopDto = new ShopDto { Name = "Test Shop" };

        _repositoryMock.Setup(r => r.GetByIdAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _mapperMock.Setup(m => m.Map<ShopDto>(It.IsAny<Shop>())).Returns(shopDto);

        // Act
        var result = await _sut.UpgradePlanAsync(shopId, SubscriptionPlan.PRO);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveAsync_WhenShopExists_TogglesActive()
    {
        // Arrange
        var shopId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var shop = new Shop
        {
            id = shopId,
            ownerId = ownerId,
            name = "Test Shop",
            description = "Desc",
            isActive = true,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var shopDto = new ShopDto { Name = "Test Shop", IsActive = false };

        _repositoryMock.Setup(r => r.GetByIdAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Shop>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ShopDto>(It.IsAny<Shop>())).Returns(shopDto);

        // Act
        var result = await _sut.ToggleActiveAsync(shopId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
