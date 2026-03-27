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

public class LeaderboardServiceTests
{
    private readonly Mock<IRepository<Leaderboard>> _leaderboardRepoMock;
    private readonly Mock<IRepository<LeaderboardEntry>> _entryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly LeaderboardService _sut;

    public LeaderboardServiceTests()
    {
        _leaderboardRepoMock = new Mock<IRepository<Leaderboard>>();
        _entryRepoMock = new Mock<IRepository<LeaderboardEntry>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new LeaderboardService(
            _leaderboardRepoMock.Object,
            _entryRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    // =========================================================================
    // GetById Tests
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_WhenLeaderboardExists_ReturnsSuccessResult()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var leaderboard = new Leaderboard
        {
            id = id,
            period = LeaderboardPeriod.WEEKLY,
            category = LeaderboardCategory.XP,
            startDate = DateTime.UtcNow.AddDays(-7),
            endDate = DateTime.UtcNow,
            createdAt = DateTime.UtcNow
        };
        var dto = new LeaderboardDto
        {
            Id = id,
            Period = LeaderboardPeriod.WEEKLY,
            Category = LeaderboardCategory.XP
        };

        _leaderboardRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaderboard);
        _mapperMock.Setup(m => m.Map<LeaderboardDto>(leaderboard)).Returns(dto);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Period.Should().Be(LeaderboardPeriod.WEEKLY);
    }

    [Fact]
    public async Task GetByIdAsync_WhenLeaderboardNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _leaderboardRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leaderboard?)null);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // GetByPeriod Tests
    // =========================================================================

    [Fact]
    public async Task GetByPeriodAsync_ReturnsMatchingLeaderboards()
    {
        // Arrange
        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard { id = Guid.NewGuid().ToString(), period = LeaderboardPeriod.WEEKLY, category = LeaderboardCategory.XP, startDate = DateTime.UtcNow, endDate = DateTime.UtcNow, createdAt = DateTime.UtcNow },
            new Leaderboard { id = Guid.NewGuid().ToString(), period = LeaderboardPeriod.WEEKLY, category = LeaderboardCategory.TASKS_COMPLETED, startDate = DateTime.UtcNow, endDate = DateTime.UtcNow, createdAt = DateTime.UtcNow }
        };
        var dtos = new List<LeaderboardDto>
        {
            new LeaderboardDto { Period = LeaderboardPeriod.WEEKLY },
            new LeaderboardDto { Period = LeaderboardPeriod.WEEKLY }
        };

        _leaderboardRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Leaderboard, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaderboards);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LeaderboardDto>>(It.IsAny<IEnumerable<Leaderboard>>())).Returns(dtos);

        // Act
        var result = await _sut.GetByPeriodAsync(LeaderboardPeriod.WEEKLY);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // =========================================================================
    // GetByCategory Tests
    // =========================================================================

    [Fact]
    public async Task GetByCategoryAsync_ReturnsMatchingLeaderboards()
    {
        // Arrange
        var leaderboards = new List<Leaderboard>
        {
            new Leaderboard { id = Guid.NewGuid().ToString(), period = LeaderboardPeriod.MONTHLY, category = LeaderboardCategory.XP, startDate = DateTime.UtcNow, endDate = DateTime.UtcNow, createdAt = DateTime.UtcNow }
        };
        var dtos = new List<LeaderboardDto>
        {
            new LeaderboardDto { Category = LeaderboardCategory.XP }
        };

        _leaderboardRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Leaderboard, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaderboards);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LeaderboardDto>>(It.IsAny<IEnumerable<Leaderboard>>())).Returns(dtos);

        // Act
        var result = await _sut.GetByCategoryAsync(LeaderboardCategory.XP);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    // =========================================================================
    // Create Tests
    // =========================================================================

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedLeaderboard()
    {
        // Arrange
        var dto = new CreateLeaderboardDto
        {
            Period = LeaderboardPeriod.MONTHLY,
            Category = LeaderboardCategory.TASKS_COMPLETED,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        var leaderboardDto = new LeaderboardDto
        {
            Period = LeaderboardPeriod.MONTHLY,
            Category = LeaderboardCategory.TASKS_COMPLETED
        };

        _leaderboardRepoMock.Setup(r => r.AddAsync(It.IsAny<Leaderboard>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leaderboard lb, CancellationToken _) => lb);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<LeaderboardDto>(It.IsAny<Leaderboard>())).Returns(leaderboardDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Period.Should().Be(LeaderboardPeriod.MONTHLY);
        _leaderboardRepoMock.Verify(r => r.AddAsync(It.IsAny<Leaderboard>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // Delete Tests
    // =========================================================================

    [Fact]
    public async Task DeleteAsync_WhenLeaderboardExists_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var leaderboard = new Leaderboard
        {
            id = id,
            period = LeaderboardPeriod.DAILY,
            category = LeaderboardCategory.XP,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow,
            createdAt = DateTime.UtcNow
        };

        _leaderboardRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaderboard);
        _leaderboardRepoMock.Setup(r => r.DeleteAsync(leaderboard, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenLeaderboardNotExists_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _leaderboardRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leaderboard?)null);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // GetEntries Tests
    // =========================================================================

    [Fact]
    public async Task GetEntriesAsync_ReturnsEntriesForLeaderboard()
    {
        // Arrange
        var leaderboardId = Guid.NewGuid().ToString();
        var entries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry { id = Guid.NewGuid().ToString(), leaderboardId = leaderboardId, userId = "user1", rank = 1, score = 100 },
            new LeaderboardEntry { id = Guid.NewGuid().ToString(), leaderboardId = leaderboardId, userId = "user2", rank = 2, score = 80 }
        };
        var entryDtos = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { LeaderboardId = leaderboardId, UserId = "user1", Rank = 1, Score = 100 },
            new LeaderboardEntryDto { LeaderboardId = leaderboardId, UserId = "user2", Rank = 2, Score = 80 }
        };

        _entryRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LeaderboardEntry, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LeaderboardEntryDto>>(It.IsAny<IEnumerable<LeaderboardEntry>>())).Returns(entryDtos);

        // Act
        var result = await _sut.GetEntriesAsync(leaderboardId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // =========================================================================
    // AddEntry Tests
    // =========================================================================

    [Fact]
    public async Task AddEntryAsync_WithValidDto_CreatesEntryAndCalculatesRank()
    {
        // Arrange
        var leaderboardId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateLeaderboardEntryDto
        {
            LeaderboardId = leaderboardId,
            UserId = userId,
            Score = 150
        };

        // Existing entries with lower scores
        var existingEntries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry { id = "e1", leaderboardId = leaderboardId, userId = "other1", rank = 1, score = 100 },
            new LeaderboardEntry { id = "e2", leaderboardId = leaderboardId, userId = "other2", rank = 2, score = 80 }
        };

        var entryDto = new LeaderboardEntryDto
        {
            LeaderboardId = leaderboardId,
            UserId = userId,
            Score = 150,
            Rank = 1
        };

        _entryRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LeaderboardEntry, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntries);
        _entryRepoMock.Setup(r => r.AddAsync(It.IsAny<LeaderboardEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaderboardEntry e, CancellationToken _) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<LeaderboardEntryDto>(It.IsAny<LeaderboardEntry>())).Returns(entryDto);

        // Act
        var result = await _sut.AddEntryAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _entryRepoMock.Verify(r => r.AddAsync(It.Is<LeaderboardEntry>(e => e.leaderboardId == leaderboardId && e.score == 150), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UpdateEntry Tests
    // =========================================================================

    [Fact]
    public async Task UpdateEntryAsync_WhenEntryExists_UpdatesEntry()
    {
        // Arrange
        var entryId = Guid.NewGuid().ToString();
        var entry = new LeaderboardEntry
        {
            id = entryId,
            leaderboardId = "lb1",
            userId = "user1",
            rank = 2,
            score = 80,
            metadata = null
        };
        var updateDto = new UpdateLeaderboardEntryDto
        {
            Score = 120,
            Metadata = "updated"
        };
        var entryDto = new LeaderboardEntryDto
        {
            Id = entryId,
            Score = 120,
            Metadata = "updated"
        };

        _entryRepoMock.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _entryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<LeaderboardEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<LeaderboardEntryDto>(It.IsAny<LeaderboardEntry>())).Returns(entryDto);

        // Act
        var result = await _sut.UpdateEntryAsync(entryId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        entry.score.Should().Be(120);
        entry.metadata.Should().Be("updated");
    }

    [Fact]
    public async Task UpdateEntryAsync_WhenEntryNotExists_ReturnsNotFound()
    {
        // Arrange
        var entryId = Guid.NewGuid().ToString();
        _entryRepoMock.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaderboardEntry?)null);

        // Act
        var result = await _sut.UpdateEntryAsync(entryId, new UpdateLeaderboardEntryDto { Score = 100 });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // DeleteEntry Tests
    // =========================================================================

    [Fact]
    public async Task DeleteEntryAsync_WhenEntryExists_ReturnsSuccess()
    {
        // Arrange
        var entryId = Guid.NewGuid().ToString();
        var entry = new LeaderboardEntry
        {
            id = entryId,
            leaderboardId = "lb1",
            userId = "user1",
            rank = 1,
            score = 100
        };

        _entryRepoMock.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _entryRepoMock.Setup(r => r.DeleteAsync(entry, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteEntryAsync(entryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEntryAsync_WhenEntryNotExists_ReturnsNotFound()
    {
        // Arrange
        var entryId = Guid.NewGuid().ToString();
        _entryRepoMock.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeaderboardEntry?)null);

        // Act
        var result = await _sut.DeleteEntryAsync(entryId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // =========================================================================
    // GetUserRankings Tests
    // =========================================================================

    [Fact]
    public async Task GetUserRankingsAsync_ReturnsAllEntriesForUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var entries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry { id = Guid.NewGuid().ToString(), leaderboardId = "lb1", userId = userId, rank = 1, score = 200 },
            new LeaderboardEntry { id = Guid.NewGuid().ToString(), leaderboardId = "lb2", userId = userId, rank = 3, score = 80 }
        };
        var entryDtos = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { UserId = userId, LeaderboardId = "lb1", Rank = 1, Score = 200 },
            new LeaderboardEntryDto { UserId = userId, LeaderboardId = "lb2", Rank = 3, Score = 80 }
        };

        _entryRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LeaderboardEntry, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LeaderboardEntryDto>>(It.IsAny<IEnumerable<LeaderboardEntry>>())).Returns(entryDtos);

        // Act
        var result = await _sut.GetUserRankingsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
