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

public class BusinessServiceTests
{
    private readonly Mock<IRepository<Business>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly BusinessService _sut;

    public BusinessServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Business>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new BusinessService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBusinessExists_ReturnsSuccessResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = Business.Create(ownerId, "Test Business");
        var businessDto = new BusinessDto { Id = businessId, Name = "Test Business" };

        _repositoryMock.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);
        _mapperMock.Setup(m => m.Map<BusinessDto>(business)).Returns(businessDto);

        // Act
        var result = await _sut.GetByIdAsync(businessId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Business");
    }

    [Fact]
    public async Task GetByIdAsync_WhenBusinessNotExists_ReturnsNotFound()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        // Act
        var result = await _sut.GetByIdAsync(businessId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBusinesses()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var businesses = new List<Business>
        {
            Business.Create(ownerId, "Business 1"),
            Business.Create(ownerId, "Business 2")
        };
        var businessDtos = new List<BusinessDto>
        {
            new BusinessDto { Name = "Business 1" },
            new BusinessDto { Name = "Business 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(businesses);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<BusinessDto>>(businesses)).Returns(businessDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedBusiness()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var dto = new CreateBusinessDto
        {
            Name = "New Business",
            OwnerId = ownerId,
            Description = "A new business",
            Industry = "Technology"
        };
        var businessDto = new BusinessDto { Name = "New Business" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business b, CancellationToken _) => b);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<BusinessDto>(It.IsAny<Business>())).Returns(businessDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Business");
    }

    [Fact]
    public async Task GetByOwnerIdAsync_ReturnsOwnerBusinesses()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var businesses = new List<Business>
        {
            Business.Create(ownerId, "Owner Business")
        };
        var businessDtos = new List<BusinessDto> { new BusinessDto { Name = "Owner Business" } };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Business, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(businesses);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<BusinessDto>>(businesses)).Returns(businessDtos);

        // Act
        var result = await _sut.GetByOwnerIdAsync(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenBusinessExists_ReturnsSuccess()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = Business.Create(ownerId, "Test Business");

        _repositoryMock.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _repositoryMock.Setup(r => r.DeleteAsync(business, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(businessId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpgradePlanAsync_WhenBusinessExists_UpgradesPlan()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = Business.Create(ownerId, "Test Business");
        var businessDto = new BusinessDto { Name = "Test Business", Plan = SubscriptionPlan.PRO };

        _repositoryMock.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<BusinessDto>(It.IsAny<Business>())).Returns(businessDto);

        // Act
        var result = await _sut.UpgradePlanAsync(businessId, SubscriptionPlan.PRO);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveAsync_WhenBusinessExists_TogglesActive()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var business = Business.Create(ownerId, "Test Business");
        var businessDto = new BusinessDto { Name = "Test Business", IsActive = false };

        _repositoryMock.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<BusinessDto>(It.IsAny<Business>())).Returns(businessDto);

        // Act
        var result = await _sut.ToggleActiveAsync(businessId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
