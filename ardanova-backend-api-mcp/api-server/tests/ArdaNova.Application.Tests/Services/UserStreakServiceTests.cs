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

public class UserStreakServiceTests
{
    private readonly Mock<IRepository<UserStreak>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserStreakService _sut;

    public UserStreakServiceTests()
    {
        _repositoryMock = new Mock<IRepository<UserStreak>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new UserStreakService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenStreakExists_ReturnsSuccessResult()
    {
        // Arrange
        var streakId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var streak = UserStreak.Create(userId, StreakType.DAILY_LOGIN);
        var streakDto = new UserStreakDto { Id = streakId, UserId = userId, StreakType = StreakType.DAILY_LOGIN };

        _repositoryMock.Setup(r => r.GetByIdAsync(streakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);
        _mapperMock.Setup(m => m.Map<UserStreakDto>(streak)).Returns(streakDto);

        // Act
        var result = await _sut.GetByIdAsync(streakId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StreakType.Should().Be(StreakType.DAILY_LOGIN);
    }

    [Fact]
    public async Task GetByIdAsync_WhenStreakNotExists_ReturnsNotFound()
    {
        // Arrange
        var streakId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(streakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        // Act
        var result = await _sut.GetByIdAsync(streakId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsStreakForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var streak = UserStreak.Create(userId, StreakType.DAILY_LOGIN);
        var streakDto = new UserStreakDto { UserId = userId, StreakType = StreakType.DAILY_LOGIN };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);
        _mapperMock.Setup(m => m.Map<UserStreakDto>(streak)).Returns(streakDto);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateUserStreakDto
        {
            UserId = userId,
            StreakType = StreakType.DAILY_LOGIN
        };
        var streakDto = new UserStreakDto { UserId = userId, StreakType = StreakType.DAILY_LOGIN, CurrentStreak = 0 };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak s, CancellationToken _) => s);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>())).Returns(streakDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordActivityAsync_WhenStreakExists_UpdatesStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var streak = UserStreak.Create(userId, StreakType.DAILY_LOGIN);
        var streakDto = new UserStreakDto { UserId = userId, CurrentStreak = 1 };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>())).Returns(streakDto);

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
