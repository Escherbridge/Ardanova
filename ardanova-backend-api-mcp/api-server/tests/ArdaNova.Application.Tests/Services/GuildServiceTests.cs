namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class GuildServiceTests
{
    private readonly Mock<IRepository<Guild>> _repositoryMock;
    private readonly Mock<IRepository<GuildMember>> _memberRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GuildService _sut;

    public GuildServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Guild>>();
        _memberRepositoryMock = new Mock<IRepository<GuildMember>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new GuildService(
            _repositoryMock.Object,
            _memberRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenGuildExists_ReturnsSuccessResult()
    {
        // Arrange
        var guildId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var guild = new Guild
        {
            id = guildId,
            ownerId = ownerId,
            name = "Test Guild",
            description = "A test guild description",
            isVerified = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var guildDto = new GuildDto { Id = guildId, Name = "Test Guild" };

        _repositoryMock.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guild);
        _mapperMock.Setup(m => m.Map<GuildDto>(guild)).Returns(guildDto);

        // Act
        var result = await _sut.GetByIdAsync(guildId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Guild");
    }

    [Fact]
    public async Task GetByIdAsync_WhenGuildNotExists_ReturnsNotFound()
    {
        // Arrange
        var guildId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guild?)null);

        // Act
        var result = await _sut.GetByIdAsync(guildId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllGuilds()
    {
        // Arrange
        var ownerId = Guid.NewGuid().ToString();
        var guilds = new List<Guild>
        {
            new Guild { id = Guid.NewGuid().ToString(), ownerId = ownerId, name = "Guild 1", description = "Desc 1", isVerified = false, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Guild { id = Guid.NewGuid().ToString(), ownerId = ownerId, name = "Guild 2", description = "Desc 2", isVerified = false, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var guildDtos = new List<GuildDto>
        {
            new GuildDto { Name = "Guild 1" },
            new GuildDto { Name = "Guild 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(guilds);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<GuildDto>>(guilds)).Returns(guildDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedGuild()
    {
        // Arrange
        var ownerId = Guid.NewGuid().ToString();
        var dto = new CreateGuildDto
        {
            Name = "New Guild",
            Description = "New guild description",
            OwnerId = ownerId,
            Email = "guild@test.com"
        };
        var guildDto = new GuildDto { Name = "New Guild" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Guild>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guild g, CancellationToken _) => g);

        _memberRepositoryMock.Setup(r => r.AddAsync(It.IsAny<GuildMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildMember m, CancellationToken _) => m);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<GuildDto>(It.IsAny<Guild>())).Returns(guildDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Guild");
        _memberRepositoryMock.Verify(
            r => r.AddAsync(It.Is<GuildMember>(m => m.userId == ownerId && m.role == "OWNER"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBySlugAsync_WhenGuildExists_ReturnsGuild()
    {
        // Arrange
        var slug = "test-guild";
        var ownerId = Guid.NewGuid().ToString();
        var guild = new Guild
        {
            id = Guid.NewGuid().ToString(),
            ownerId = ownerId,
            name = "Test Guild",
            description = "Description",
            slug = slug,
            isVerified = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var guildDto = new GuildDto { Name = "Test Guild" };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Guild, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guild);
        _mapperMock.Setup(m => m.Map<GuildDto>(guild)).Returns(guildDto);

        // Act
        var result = await _sut.GetBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenGuildExists_ReturnsSuccess()
    {
        // Arrange
        var guildId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var guild = new Guild
        {
            id = guildId,
            ownerId = ownerId,
            name = "Test Guild",
            description = "Desc",
            isVerified = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guild);

        _repositoryMock.Setup(r => r.DeleteAsync(guild, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(guildId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WhenGuildExists_VerifiesGuild()
    {
        // Arrange
        var guildId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        var guild = new Guild
        {
            id = guildId,
            ownerId = ownerId,
            name = "Test Guild",
            description = "Desc",
            isVerified = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var guildDto = new GuildDto { Name = "Test Guild", IsVerified = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guild);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guild>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<GuildDto>(It.IsAny<Guild>())).Returns(guildDto);

        // Act
        var result = await _sut.VerifyAsync(guildId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
