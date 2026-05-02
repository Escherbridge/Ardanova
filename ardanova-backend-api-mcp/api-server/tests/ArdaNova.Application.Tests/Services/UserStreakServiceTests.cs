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

    // =========================================================================
    // GetById Tests
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_WhenStreakExists_ReturnsSuccessResult()
    {
        // Arrange
        var streakId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var streak = new UserStreak
        {
            id = streakId,
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 0,
            longestStreak = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
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
        var streakId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(streakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        // Act
        var result = await _sut.GetByIdAsync(streakId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // GetByUserId Tests
    // =========================================================================

    [Fact]
    public async Task GetByUserIdAsync_WhenStreakExists_ReturnsStreakForUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 5,
            longestStreak = 10,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var streakDto = new UserStreakDto { UserId = userId, StreakType = StreakType.DAILY_LOGIN, CurrentStreak = 5, LongestStreak = 10 };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);
        _mapperMock.Setup(m => m.Map<UserStreakDto>(streak)).Returns(streakDto);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CurrentStreak.Should().Be(5);
        result.Value!.LongestStreak.Should().Be(10);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenStreakNotExists_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // Create Tests
    // =========================================================================

    [Fact]
    public async Task CreateAsync_WhenUserHasNoStreak_CreatesStreak()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
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
        result.Value!.CurrentStreak.Should().Be(0);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<UserStreak>(s => s.userId == userId && s.currentStreak == 0), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUserAlreadyHasStreak_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateUserStreakDto
        {
            UserId = userId,
            StreakType = StreakType.DAILY_LOGIN
        };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // RecordActivity Tests
    // =========================================================================

    [Fact]
    public async Task RecordActivityAsync_WhenNoStreak_AutoCreatesAndRecords()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var streakDto = new UserStreakDto { UserId = userId, CurrentStreak = 1 };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak s, CancellationToken _) => s);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>())).Returns(streakDto);

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddAsync(It.Is<UserStreak>(s => s.userId == userId && s.streakType == StreakType.DAILY_LOGIN), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordActivityAsync_ConsecutiveDay_IncrementsStreak()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1).AddHours(12);
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 3,
            longestStreak = 5,
            lastActivityDate = yesterday,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>()))
            .Returns((UserStreak s) => new UserStreakDto { UserId = s.userId, CurrentStreak = s.currentStreak, LongestStreak = s.longestStreak });

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        streak.currentStreak.Should().Be(4);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<UserStreak>(s => s.currentStreak == 4), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordActivityAsync_SameDay_NoChange()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var today = DateTime.UtcNow.Date.AddHours(2);
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 5,
            longestStreak = 10,
            lastActivityDate = today,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>()))
            .Returns((UserStreak s) => new UserStreakDto { UserId = s.userId, CurrentStreak = s.currentStreak, LongestStreak = s.longestStreak });

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        streak.currentStreak.Should().Be(5);
    }

    [Fact]
    public async Task RecordActivityAsync_SkippedDay_ResetsToOne()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2).AddHours(12);
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 7,
            longestStreak = 10,
            lastActivityDate = twoDaysAgo,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>()))
            .Returns((UserStreak s) => new UserStreakDto { UserId = s.userId, CurrentStreak = s.currentStreak, LongestStreak = s.longestStreak });

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        streak.currentStreak.Should().Be(1);
        streak.longestStreak.Should().Be(10);
    }

    [Fact]
    public async Task RecordActivityAsync_ConsecutiveDay_UpdatesLongestStreak()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1).AddHours(12);
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 5,
            longestStreak = 5,
            lastActivityDate = yesterday,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>()))
            .Returns((UserStreak s) => new UserStreakDto { UserId = s.userId, CurrentStreak = s.currentStreak, LongestStreak = s.longestStreak });

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        streak.currentStreak.Should().Be(6);
        streak.longestStreak.Should().Be(6);
    }

    // =========================================================================
    // ResetStreak Tests
    // =========================================================================

    [Fact]
    public async Task ResetStreakAsync_WhenStreakExists_ResetsToZero()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 10,
            longestStreak = 15,
            lastActivityDate = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var streakDto = new UserStreakDto { UserId = userId, CurrentStreak = 0, LongestStreak = 15 };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>())).Returns(streakDto);

        // Act
        var result = await _sut.ResetStreakAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        streak.currentStreak.Should().Be(0);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<UserStreak>(s => s.currentStreak == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetStreakAsync_WhenStreakNotExists_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        // Act
        var result = await _sut.ResetStreakAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // Delete Tests
    // =========================================================================

    [Fact]
    public async Task DeleteAsync_WhenStreakExists_ReturnsSuccess()
    {
        // Arrange
        var streakId = Guid.NewGuid().ToString();
        var streak = new UserStreak
        {
            id = streakId,
            userId = Guid.NewGuid().ToString(),
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 3,
            longestStreak = 5,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(streakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.DeleteAsync(streak, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(streakId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(streak, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenStreakNotExists_ReturnsNotFound()
    {
        // Arrange
        var streakId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(streakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        // Act
        var result = await _sut.DeleteAsync(streakId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // RecordActivity - Edge Case: First activity ever (no lastActivityDate)
    // =========================================================================

    [Fact]
    public async Task RecordActivityAsync_ExistingStreakNoLastActivity_SetsStreakToOne()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            streakType = StreakType.DAILY_LOGIN,
            currentStreak = 0,
            longestStreak = 0,
            lastActivityDate = null,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserStreak, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserStreakDto>(It.IsAny<UserStreak>()))
            .Returns((UserStreak s) => new UserStreakDto { UserId = s.userId, CurrentStreak = s.currentStreak, LongestStreak = s.longestStreak });

        // Act
        var result = await _sut.RecordActivityAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        streak.currentStreak.Should().Be(1);
        streak.lastActivityDate.Should().NotBeNull();
    }
}
