namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class AgencyServiceTests
{
    private readonly Mock<IRepository<Agency>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly AgencyService _sut;

    public AgencyServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Agency>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new AgencyService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAgencyExists_ReturnsSuccessResult()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var agency = Agency.Create(ownerId, "Test Agency", "A test agency description", "test@agency.com");
        var agencyDto = new AgencyDto { Id = agencyId, Name = "Test Agency" };

        _repositoryMock.Setup(r => r.GetByIdAsync(agencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        _mapperMock.Setup(m => m.Map<AgencyDto>(agency)).Returns(agencyDto);

        // Act
        var result = await _sut.GetByIdAsync(agencyId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Agency");
    }

    [Fact]
    public async Task GetByIdAsync_WhenAgencyNotExists_ReturnsNotFound()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(agencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agency?)null);

        // Act
        var result = await _sut.GetByIdAsync(agencyId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAgencies()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var agencies = new List<Agency>
        {
            Agency.Create(ownerId, "Agency 1", "Desc 1", "agency1@test.com"),
            Agency.Create(ownerId, "Agency 2", "Desc 2", "agency2@test.com")
        };
        var agencyDtos = new List<AgencyDto>
        {
            new AgencyDto { Name = "Agency 1" },
            new AgencyDto { Name = "Agency 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(agencies);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<AgencyDto>>(agencies)).Returns(agencyDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedAgency()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var dto = new CreateAgencyDto
        {
            Name = "New Agency",
            Description = "New agency description",
            Email = "new@agency.com",
            OwnerId = ownerId
        };
        var agencyDto = new AgencyDto { Name = "New Agency" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agency a, CancellationToken _) => a);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<AgencyDto>(It.IsAny<Agency>())).Returns(agencyDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Agency");
    }

    [Fact]
    public async Task GetBySlugAsync_WhenAgencyExists_ReturnsAgency()
    {
        // Arrange
        var slug = "test-agency";
        var ownerId = Guid.NewGuid();
        var agency = Agency.Create(ownerId, "Test Agency", "Description", "test@agency.com");
        var agencyDto = new AgencyDto { Name = "Test Agency" };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Agency, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        _mapperMock.Setup(m => m.Map<AgencyDto>(agency)).Returns(agencyDto);

        // Act
        var result = await _sut.GetBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenAgencyExists_ReturnsSuccess()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var agency = Agency.Create(ownerId, "Test Agency", "Desc", "test@agency.com");

        _repositoryMock.Setup(r => r.GetByIdAsync(agencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);

        _repositoryMock.Setup(r => r.DeleteAsync(agency, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(agencyId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WhenAgencyExists_VerifiesAgency()
    {
        // Arrange
        var agencyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var agency = Agency.Create(ownerId, "Test Agency", "Desc", "test@agency.com");
        var agencyDto = new AgencyDto { Name = "Test Agency", IsVerified = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(agencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<AgencyDto>(It.IsAny<Agency>())).Returns(agencyDto);

        // Act
        var result = await _sut.VerifyAsync(agencyId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
