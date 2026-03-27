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

public class AchievementServiceTests
{
    private readonly Mock<IRepository<Achievement>> _achievementRepoMock;
    private readonly Mock<IRepository<UserAchievement>> _userAchievementRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly AchievementService _sut;

    public AchievementServiceTests()
    {
        _achievementRepoMock = new Mock<IRepository<Achievement>>();
        _userAchievementRepoMock = new Mock<IRepository<UserAchievement>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new AchievementService(
            _achievementRepoMock.Object,
            _userAchievementRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    // ---- GetById Tests ----

    [Fact]
    public async Task GetByIdAsync_WhenAchievementExists_ReturnsSuccess()
    {
        // Arrange
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "First Contribution",
            description = "Make your first contribution",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{\"type\":\"contribution\",\"count\":1}",
            xpReward = 100,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var expectedDto = new AchievementDto
        {
            Id = achievementId,
            Name = "First Contribution",
            Description = "Make your first contribution",
            Category = AchievementCategory.CONTRIBUTOR,
            Criteria = "{\"type\":\"contribution\",\"count\":1}",
            XpReward = 100,
            Rarity = AchievementRarity.COMMON,
            IsActive = true,
            CreatedAt = achievement.createdAt
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _mapperMock.Setup(m => m.Map<AchievementDto>(achievement)).Returns(expectedDto);

        // Act
        var result = await _sut.GetByIdAsync(achievementId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(achievementId);
        result.Value.Name.Should().Be("First Contribution");
    }

    [Fact]
    public async Task GetByIdAsync_WhenAchievementNotFound_ReturnsNotFound()
    {
        // Arrange
        var achievementId = Guid.NewGuid().ToString();
        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);

        // Act
        var result = await _sut.GetByIdAsync(achievementId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ---- GetAll Tests ----

    [Fact]
    public async Task GetAllAsync_ReturnsAllAchievements()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new Achievement { id = "1", name = "First", description = "Desc", category = AchievementCategory.CONTRIBUTOR, criteria = "{}", xpReward = 50, rarity = AchievementRarity.COMMON, isActive = true, createdAt = DateTime.UtcNow },
            new Achievement { id = "2", name = "Second", description = "Desc2", category = AchievementCategory.COLLABORATOR, criteria = "{}", xpReward = 100, rarity = AchievementRarity.RARE, isActive = true, createdAt = DateTime.UtcNow }
        };
        var dtos = new List<AchievementDto>
        {
            new AchievementDto { Id = "1", Name = "First" },
            new AchievementDto { Id = "2", Name = "Second" }
        };

        _achievementRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievements.AsReadOnly());
        _mapperMock.Setup(m => m.Map<IReadOnlyList<AchievementDto>>(It.IsAny<IReadOnlyList<Achievement>>()))
            .Returns(dtos.AsReadOnly());

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ---- GetByCategory Tests ----

    [Fact]
    public async Task GetByCategoryAsync_ReturnsFilteredAchievements()
    {
        // Arrange
        var category = AchievementCategory.CONTRIBUTOR;
        var achievements = new List<Achievement>
        {
            new Achievement { id = "1", name = "First", description = "Desc", category = AchievementCategory.CONTRIBUTOR, criteria = "{}", xpReward = 50, rarity = AchievementRarity.COMMON, isActive = true, createdAt = DateTime.UtcNow }
        };
        var dtos = new List<AchievementDto>
        {
            new AchievementDto { Id = "1", Name = "First", Category = AchievementCategory.CONTRIBUTOR }
        };

        _achievementRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Achievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievements.AsReadOnly());
        _mapperMock.Setup(m => m.Map<IReadOnlyList<AchievementDto>>(It.IsAny<IReadOnlyList<Achievement>>()))
            .Returns(dtos.AsReadOnly());

        // Act
        var result = await _sut.GetByCategoryAsync(category);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Category.Should().Be(AchievementCategory.CONTRIBUTOR);
    }

    // ---- Create Tests ----

    [Fact]
    public async Task CreateAsync_WithValidInput_ReturnsCreatedAchievement()
    {
        // Arrange
        var dto = new CreateAchievementDto
        {
            Name = "First Contribution",
            Description = "Make your first contribution",
            Category = AchievementCategory.CONTRIBUTOR,
            Criteria = "{\"type\":\"contribution\",\"count\":1}",
            XpReward = 100,
            Rarity = AchievementRarity.COMMON
        };
        var expectedDto = new AchievementDto
        {
            Id = "new-id",
            Name = "First Contribution",
            Description = "Make your first contribution",
            Category = AchievementCategory.CONTRIBUTOR,
            XpReward = 100,
            IsActive = true
        };

        _achievementRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Achievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _achievementRepoMock.Setup(r => r.AddAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement a, CancellationToken _) => a);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<AchievementDto>(It.IsAny<Achievement>())).Returns(expectedDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("First Contribution");
        result.Value.IsActive.Should().BeTrue();

        _achievementRepoMock.Verify(r => r.AddAsync(It.Is<Achievement>(a =>
            a.name == "First Contribution" &&
            a.isActive == true), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateAchievementDto
        {
            Name = "Existing Achievement",
            Description = "Already exists",
            Category = AchievementCategory.CONTRIBUTOR,
            Criteria = "{}",
            XpReward = 50
        };

        _achievementRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Achievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("name");
    }

    // ---- Update Tests ----

    [Fact]
    public async Task UpdateAsync_WhenExists_ReturnsUpdatedAchievement()
    {
        // Arrange
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "Old Name",
            description = "Old desc",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var updateDto = new UpdateAchievementDto
        {
            Name = "New Name",
            XpReward = 200
        };
        var expectedDto = new AchievementDto
        {
            Id = achievementId,
            Name = "New Name",
            XpReward = 200
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<AchievementDto>(It.IsAny<Achievement>())).Returns(expectedDto);

        // Act
        var result = await _sut.UpdateAsync(achievementId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("New Name");

        _achievementRepoMock.Verify(r => r.UpdateAsync(It.Is<Achievement>(a =>
            a.name == "New Name" &&
            a.xpReward == 200), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var achievementId = Guid.NewGuid().ToString();
        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);

        // Act
        var result = await _sut.UpdateAsync(achievementId, new UpdateAchievementDto { Name = "New" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ---- Delete Tests ----

    [Fact]
    public async Task DeleteAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "To Delete",
            description = "Will be deleted",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(achievementId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _achievementRepoMock.Verify(r => r.DeleteAsync(achievement, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var achievementId = Guid.NewGuid().ToString();
        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);

        // Act
        var result = await _sut.DeleteAsync(achievementId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ---- GetUserAchievements Tests ----

    [Fact]
    public async Task GetUserAchievementsAsync_ReturnsUserAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userAchievements = new List<UserAchievement>
        {
            new UserAchievement { id = "1", userId = userId, achievementId = "ach-1", progress = 100, earnedAt = DateTime.UtcNow },
            new UserAchievement { id = "2", userId = userId, achievementId = "ach-2", progress = 50, earnedAt = null }
        };
        var dtos = new List<UserAchievementDto>
        {
            new UserAchievementDto { Id = "1", UserId = userId, AchievementId = "ach-1", Progress = 100, EarnedAt = DateTime.UtcNow },
            new UserAchievementDto { Id = "2", UserId = userId, AchievementId = "ach-2", Progress = 50, EarnedAt = null }
        };

        _userAchievementRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAchievements.AsReadOnly());
        _mapperMock.Setup(m => m.Map<IReadOnlyList<UserAchievementDto>>(It.IsAny<IReadOnlyList<UserAchievement>>()))
            .Returns(dtos.AsReadOnly());

        // Act
        var result = await _sut.GetUserAchievementsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ---- UpdateProgress Tests ----

    [Fact]
    public async Task UpdateProgressAsync_WhenNewUserAchievement_CreatesAndReturns()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "Test",
            description = "Test desc",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var progressDto = new UpdateProgressDto { Progress = 50 };
        var expectedDto = new UserAchievementDto
        {
            Id = "new-id",
            UserId = userId,
            AchievementId = achievementId,
            Progress = 50,
            EarnedAt = null
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _userAchievementRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<UserAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAchievement?)null);
        _userAchievementRepoMock.Setup(r => r.AddAsync(It.IsAny<UserAchievement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAchievement ua, CancellationToken _) => ua);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserAchievementDto>(It.IsAny<UserAchievement>())).Returns(expectedDto);

        // Act
        var result = await _sut.UpdateProgressAsync(userId, achievementId, progressDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Progress.Should().Be(50);
        result.Value.EarnedAt.Should().BeNull();

        _userAchievementRepoMock.Verify(r => r.AddAsync(It.Is<UserAchievement>(ua =>
            ua.userId == userId &&
            ua.achievementId == achievementId &&
            ua.progress == 50), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenExistingUserAchievement_UpdatesProgress()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "Test",
            description = "Test desc",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var existingUserAchievement = new UserAchievement
        {
            id = "existing-id",
            userId = userId,
            achievementId = achievementId,
            progress = 30,
            earnedAt = null
        };
        var progressDto = new UpdateProgressDto { Progress = 75 };
        var expectedDto = new UserAchievementDto
        {
            Id = "existing-id",
            UserId = userId,
            AchievementId = achievementId,
            Progress = 75,
            EarnedAt = null
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _userAchievementRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<UserAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUserAchievement);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserAchievementDto>(It.IsAny<UserAchievement>())).Returns(expectedDto);

        // Act
        var result = await _sut.UpdateProgressAsync(userId, achievementId, progressDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Progress.Should().Be(75);

        _userAchievementRepoMock.Verify(r => r.UpdateAsync(It.Is<UserAchievement>(ua =>
            ua.progress == 75), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenProgressReaches100_AutoAwards()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "Test",
            description = "Test desc",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var existingUserAchievement = new UserAchievement
        {
            id = "existing-id",
            userId = userId,
            achievementId = achievementId,
            progress = 80,
            earnedAt = null
        };
        var progressDto = new UpdateProgressDto { Progress = 100 };
        var expectedDto = new UserAchievementDto
        {
            Id = "existing-id",
            UserId = userId,
            AchievementId = achievementId,
            Progress = 100,
            EarnedAt = DateTime.UtcNow
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _userAchievementRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<UserAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUserAchievement);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserAchievementDto>(It.IsAny<UserAchievement>())).Returns(expectedDto);

        // Act
        var result = await _sut.UpdateProgressAsync(userId, achievementId, progressDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Progress.Should().Be(100);
        result.Value.EarnedAt.Should().NotBeNull();

        _userAchievementRepoMock.Verify(r => r.UpdateAsync(It.Is<UserAchievement>(ua =>
            ua.progress == 100 &&
            ua.earnedAt != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenAchievementNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();
        var progressDto = new UpdateProgressDto { Progress = 50 };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);

        // Act
        var result = await _sut.UpdateProgressAsync(userId, achievementId, progressDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ---- AwardAchievement Tests ----

    [Fact]
    public async Task AwardAchievementAsync_WhenValid_AwardsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "Test",
            description = "Test desc",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var expectedDto = new UserAchievementDto
        {
            Id = "new-id",
            UserId = userId,
            AchievementId = achievementId,
            Progress = 100,
            EarnedAt = DateTime.UtcNow
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _userAchievementRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<UserAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAchievement?)null);
        _userAchievementRepoMock.Setup(r => r.AddAsync(It.IsAny<UserAchievement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAchievement ua, CancellationToken _) => ua);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserAchievementDto>(It.IsAny<UserAchievement>())).Returns(expectedDto);

        // Act
        var result = await _sut.AwardAchievementAsync(userId, achievementId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Progress.Should().Be(100);
        result.Value.EarnedAt.Should().NotBeNull();

        _userAchievementRepoMock.Verify(r => r.AddAsync(It.Is<UserAchievement>(ua =>
            ua.userId == userId &&
            ua.achievementId == achievementId &&
            ua.progress == 100 &&
            ua.earnedAt != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardAchievementAsync_WhenAlreadyEarned_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();
        var achievement = new Achievement
        {
            id = achievementId,
            name = "Test",
            description = "Test desc",
            category = AchievementCategory.CONTRIBUTOR,
            criteria = "{}",
            xpReward = 50,
            rarity = AchievementRarity.COMMON,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var existingUserAchievement = new UserAchievement
        {
            id = "existing-id",
            userId = userId,
            achievementId = achievementId,
            progress = 100,
            earnedAt = DateTime.UtcNow.AddDays(-1)
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(achievement);
        _userAchievementRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<UserAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUserAchievement);

        // Act
        var result = await _sut.AwardAchievementAsync(userId, achievementId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already earned");
    }

    [Fact]
    public async Task AwardAchievementAsync_WhenAchievementNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var achievementId = Guid.NewGuid().ToString();

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Achievement?)null);

        // Act
        var result = await _sut.AwardAchievementAsync(userId, achievementId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }
}
