namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class XPEventServiceTests
{
    private readonly Mock<IRepository<XPEvent>> _xpEventRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly XPEventService _sut;

    public XPEventServiceTests()
    {
        _xpEventRepoMock = new Mock<IRepository<XPEvent>>();
        _userRepoMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new XPEventService(
            _xpEventRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    // ---- AwardXP Tests ----

    [Fact]
    public async Task AwardXP_WithValidInput_CreatesEventAndUpdatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            id = userId,
            email = "test@example.com",
            totalXP = 50,
            level = 1,
            tier = UserTier.BRONZE,
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            isVerified = false,
            verificationLevel = VerificationLevel.ANONYMOUS,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        var dto = new AwardXPDto
        {
            UserId = userId,
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 50,
            Source = "task-service"
        };

        var expectedEventDto = new XPEventDto
        {
            Id = "some-id",
            UserId = userId,
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 50,
            Source = "task-service",
            CreatedAt = DateTime.UtcNow
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _xpEventRepoMock.Setup(r => r.AddAsync(It.IsAny<XPEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XPEvent e, CancellationToken _) => e);

        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<XPEventDto>(It.IsAny<XPEvent>())).Returns(expectedEventDto);

        // Act
        var result = await _sut.AwardXPAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(50);
        result.Value.EventType.Should().Be(XPEventType.TASK_COMPLETED);

        // Verify user was updated
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.totalXP == 100), It.IsAny<CancellationToken>()), Times.Once);

        // Verify event was created
        _xpEventRepoMock.Verify(r => r.AddAsync(It.Is<XPEvent>(e =>
            e.userId == userId &&
            e.eventType == XPEventType.TASK_COMPLETED &&
            e.amount == 50), It.IsAny<CancellationToken>()), Times.Once);

        // Verify changes were saved
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardXP_WithZeroAmount_ReturnsValidationError()
    {
        // Arrange
        var dto = new AwardXPDto
        {
            UserId = Guid.NewGuid().ToString(),
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 0,
            Source = "task-service"
        };

        // Act
        var result = await _sut.AwardXPAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public async Task AwardXP_WithNegativeAmount_ReturnsValidationError()
    {
        // Arrange
        var dto = new AwardXPDto
        {
            UserId = Guid.NewGuid().ToString(),
            EventType = XPEventType.TASK_COMPLETED,
            Amount = -10,
            Source = "task-service"
        };

        // Act
        var result = await _sut.AwardXPAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task AwardXP_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new AwardXPDto
        {
            UserId = userId,
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 50,
            Source = "task-service"
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.AwardXPAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task AwardXP_UpdatesUserLevelWhenThresholdReached()
    {
        // Arrange: user starts at 90 XP (level 1), awarding 20 XP should push to level 1 (100 XP threshold for level 1)
        // Actually level 1 threshold = 100 XP, so 90 + 20 = 110 means user is level 1 (100..282 range)
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            id = userId,
            email = "test@example.com",
            totalXP = 0,
            level = 0,
            tier = UserTier.BRONZE,
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            isVerified = false,
            verificationLevel = VerificationLevel.ANONYMOUS,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        var dto = new AwardXPDto
        {
            UserId = userId,
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 150,
            Source = "task-service"
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _xpEventRepoMock.Setup(r => r.AddAsync(It.IsAny<XPEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XPEvent e, CancellationToken _) => e);

        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<XPEventDto>(It.IsAny<XPEvent>()))
            .Returns(new XPEventDto { Amount = 150 });

        // Act
        var result = await _sut.AwardXPAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 150 XP > 100 (level 1 threshold), so user should be level 1
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.totalXP == 150 && u.level >= 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardXP_UpdatesUserTierWhenThresholdReached()
    {
        // Arrange: user at 990 XP (BRONZE), awarding 20 XP should push to SILVER (1000+)
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            id = userId,
            email = "test@example.com",
            totalXP = 990,
            level = 3,
            tier = UserTier.BRONZE,
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            isVerified = false,
            verificationLevel = VerificationLevel.ANONYMOUS,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        var dto = new AwardXPDto
        {
            UserId = userId,
            EventType = XPEventType.PROJECT_FUNDED,
            Amount = 20,
            Source = "project-service"
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _xpEventRepoMock.Setup(r => r.AddAsync(It.IsAny<XPEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XPEvent e, CancellationToken _) => e);

        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<XPEventDto>(It.IsAny<XPEvent>()))
            .Returns(new XPEventDto { Amount = 20 });

        // Act
        var result = await _sut.AwardXPAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 990 + 20 = 1010, which is >= 1000 so SILVER tier
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.totalXP == 1010 && u.tier == UserTier.SILVER), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- GetTotalXP Tests ----

    [Fact]
    public async Task GetTotalXP_ReturnsCorrectTotal()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            id = userId,
            email = "test@example.com",
            totalXP = 1500,
            level = 5,
            tier = UserTier.SILVER,
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            isVerified = false,
            verificationLevel = VerificationLevel.ANONYMOUS,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetTotalXPAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1500);
    }

    [Fact]
    public async Task GetTotalXP_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetTotalXPAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ---- GetHistory Tests ----

    [Fact]
    public async Task GetHistory_ReturnsFilteredEvents()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var events = new List<XPEvent>
        {
            new XPEvent { id = "1", userId = userId, eventType = XPEventType.TASK_COMPLETED, amount = 50, source = "test", createdAt = DateTime.UtcNow },
            new XPEvent { id = "2", userId = userId, eventType = XPEventType.VOTE_CAST, amount = 10, source = "test", createdAt = DateTime.UtcNow }
        };
        var eventDtos = new List<XPEventDto>
        {
            new XPEventDto { Id = "1", UserId = userId, EventType = XPEventType.TASK_COMPLETED, Amount = 50 },
            new XPEventDto { Id = "2", UserId = userId, EventType = XPEventType.VOTE_CAST, Amount = 10 }
        };

        _xpEventRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<XPEvent, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events.AsReadOnly());

        _mapperMock.Setup(m => m.Map<IReadOnlyList<XPEventDto>>(It.IsAny<IEnumerable<XPEvent>>()))
            .Returns(eventDtos.AsReadOnly());

        // Act
        var result = await _sut.GetHistoryAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetHistory_WithEventTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var events = new List<XPEvent>
        {
            new XPEvent { id = "1", userId = userId, eventType = XPEventType.TASK_COMPLETED, amount = 50, source = "test", createdAt = DateTime.UtcNow }
        };
        var eventDtos = new List<XPEventDto>
        {
            new XPEventDto { Id = "1", UserId = userId, EventType = XPEventType.TASK_COMPLETED, Amount = 50 }
        };

        _xpEventRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<XPEvent, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events.AsReadOnly());

        _mapperMock.Setup(m => m.Map<IReadOnlyList<XPEventDto>>(It.IsAny<IEnumerable<XPEvent>>()))
            .Returns(eventDtos.AsReadOnly());

        // Act
        var result = await _sut.GetHistoryAsync(userId, XPEventType.TASK_COMPLETED);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].EventType.Should().Be(XPEventType.TASK_COMPLETED);
    }

    // ---- GetXPByEventType Tests ----

    [Fact]
    public async Task GetXPByEventType_ReturnsCorrectSum()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var events = new List<XPEvent>
        {
            new XPEvent { id = "1", userId = userId, eventType = XPEventType.TASK_COMPLETED, amount = 50, source = "test", createdAt = DateTime.UtcNow },
            new XPEvent { id = "2", userId = userId, eventType = XPEventType.TASK_COMPLETED, amount = 30, source = "test", createdAt = DateTime.UtcNow }
        };

        _xpEventRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<XPEvent, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events.AsReadOnly());

        // Act
        var result = await _sut.GetXPByEventTypeAsync(userId, XPEventType.TASK_COMPLETED);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(80);
    }

    // ---- CalculateLevel Tests ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 0)]
    [InlineData(99, 0)]
    [InlineData(100, 1)]
    [InlineData(281, 1)]
    [InlineData(282, 2)]
    [InlineData(518, 2)]
    [InlineData(519, 3)]
    [InlineData(1118, 5)]
    [InlineData(3162, 10)]
    public void CalculateLevel_ReturnsCorrectLevel(int totalXP, int expectedLevel)
    {
        // Act
        var level = _sut.CalculateLevel(totalXP);

        // Assert
        level.Should().Be(expectedLevel);
    }

    // ---- CalculateTier Tests ----

    [Theory]
    [InlineData(0, UserTier.BRONZE)]
    [InlineData(500, UserTier.BRONZE)]
    [InlineData(999, UserTier.BRONZE)]
    [InlineData(1000, UserTier.SILVER)]
    [InlineData(4999, UserTier.SILVER)]
    [InlineData(5000, UserTier.GOLD)]
    [InlineData(14999, UserTier.GOLD)]
    [InlineData(15000, UserTier.PLATINUM)]
    [InlineData(49999, UserTier.PLATINUM)]
    [InlineData(50000, UserTier.DIAMOND)]
    [InlineData(100000, UserTier.DIAMOND)]
    public void CalculateTier_ReturnsCorrectTier(int totalXP, UserTier expectedTier)
    {
        // Act
        var tier = _sut.CalculateTier(totalXP);

        // Assert
        tier.Should().Be(expectedTier);
    }

    // ---- GetXPSummary Tests ----

    [Fact]
    public async Task GetXPSummary_ReturnsCorrectSummary()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            id = userId,
            email = "test@example.com",
            totalXP = 300,
            level = 2,
            tier = UserTier.BRONZE,
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            isVerified = false,
            verificationLevel = VerificationLevel.ANONYMOUS,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetXPSummaryAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(userId);
        result.Value.TotalXP.Should().Be(300);
        result.Value.Level.Should().Be(2);
        result.Value.Tier.Should().Be(UserTier.BRONZE);
        result.Value.XPForCurrentLevel.Should().BeGreaterThan(0);
        result.Value.XPForNextLevel.Should().BeGreaterThan(result.Value.XPForCurrentLevel);
        result.Value.ProgressPercent.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(100);
    }

    [Fact]
    public async Task GetXPSummary_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetXPSummaryAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ---- GetRewardsConfig Tests ----

    [Fact]
    public void GetRewardsConfig_ReturnsNonEmptyConfig()
    {
        // Act
        var config = _sut.GetRewardsConfig();

        // Assert
        config.Should().NotBeNull();
        config.Rewards.Should().NotBeEmpty();
        config.Rewards.Should().ContainKey("TASK_COMPLETED");
        config.Rewards["TASK_COMPLETED"].Should().Be(50);
    }

    // ---- GetLevelInfo Tests ----

    [Fact]
    public void GetLevelInfo_ReturnsCorrectInfo()
    {
        // Act
        var info = _sut.GetLevelInfo(1);

        // Assert
        info.Should().NotBeNull();
        info.Level.Should().Be(1);
        info.XPRequired.Should().Be(100);
    }

    [Fact]
    public void GetLevelInfo_Level5_ReturnsCorrectXP()
    {
        // Act
        var info = _sut.GetLevelInfo(5);

        // Assert
        info.Level.Should().Be(5);
        info.XPRequired.Should().Be(1118);
    }
}
