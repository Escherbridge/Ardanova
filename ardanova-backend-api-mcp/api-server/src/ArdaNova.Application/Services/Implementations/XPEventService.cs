namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class XPEventService : IXPEventService
{
    private readonly IRepository<XPEvent> _xpEventRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private static readonly Dictionary<string, int> DefaultRewards = new()
    {
        ["TASK_COMPLETED"] = 50,
        ["PROPOSAL_CREATED"] = 30,
        ["PROPOSAL_PASSED"] = 100,
        ["VOTE_CAST"] = 10,
        ["PROJECT_FUNDED"] = 200,
        ["MEMBER_REFERRED"] = 75,
        ["ACHIEVEMENT_EARNED"] = 50,
        ["STREAK_MAINTAINED"] = 15,
        ["LEVEL_UP"] = 0,
        ["REVIEW_GIVEN"] = 20,
        ["CONTRIBUTION_MADE"] = 25
    };

    public XPEventService(
        IRepository<XPEvent> xpEventRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _xpEventRepository = xpEventRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<XPEventDto>> AwardXPAsync(AwardXPDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0)
            return Result<XPEventDto>.ValidationError("XP amount must be a positive integer");

        var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
        if (user is null)
            return Result<XPEventDto>.NotFound($"User with id {dto.UserId} not found");

        // Create XP event
        var xpEvent = new XPEvent
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            eventType = dto.EventType,
            amount = dto.Amount,
            source = dto.Source,
            sourceId = dto.SourceId,
            metadata = dto.Metadata,
            createdAt = DateTime.UtcNow
        };

        await _xpEventRepository.AddAsync(xpEvent, ct);

        // Update user XP, level, and tier
        user.totalXP += dto.Amount;
        user.level = CalculateLevel(user.totalXP);
        user.tier = CalculateTier(user.totalXP);
        user.updatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<XPEventDto>.Success(_mapper.Map<XPEventDto>(xpEvent));
    }

    public async Task<Result<int>> GetTotalXPAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<int>.NotFound($"User with id {userId} not found");

        return Result<int>.Success(user.totalXP);
    }

    public async Task<Result<IReadOnlyList<XPEventDto>>> GetHistoryAsync(
        string userId,
        XPEventType? eventType = null,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        IReadOnlyList<XPEvent> events;

        if (eventType.HasValue)
        {
            var filterType = eventType.Value;
            events = await _xpEventRepository.FindAsync(
                e => e.userId == userId && e.eventType == filterType, ct);
        }
        else
        {
            events = await _xpEventRepository.FindAsync(
                e => e.userId == userId, ct);
        }

        // Apply ordering, offset, and limit in memory
        var paged = events
            .OrderByDescending(e => e.createdAt)
            .Skip(offset)
            .Take(limit)
            .ToList();

        return Result<IReadOnlyList<XPEventDto>>.Success(
            _mapper.Map<IReadOnlyList<XPEventDto>>(paged));
    }

    public async Task<Result<int>> GetXPByEventTypeAsync(
        string userId,
        XPEventType eventType,
        CancellationToken ct = default)
    {
        var events = await _xpEventRepository.FindAsync(
            e => e.userId == userId && e.eventType == eventType, ct);

        var total = events.Sum(e => e.amount);
        return Result<int>.Success(total);
    }

    public async Task<Result<XPSummaryDto>> GetXPSummaryAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<XPSummaryDto>.NotFound($"User with id {userId} not found");

        var currentLevel = CalculateLevel(user.totalXP);
        var xpForCurrentLevel = GetXPRequiredForLevel(currentLevel);
        var xpForNextLevel = GetXPRequiredForLevel(currentLevel + 1);

        var xpIntoCurrentLevel = user.totalXP - xpForCurrentLevel;
        var xpNeededForNextLevel = xpForNextLevel - xpForCurrentLevel;

        var progressPercent = xpNeededForNextLevel > 0
            ? Math.Round((double)xpIntoCurrentLevel / xpNeededForNextLevel * 100, 2)
            : 100.0;

        var summary = new XPSummaryDto
        {
            UserId = userId,
            TotalXP = user.totalXP,
            Level = currentLevel,
            Tier = CalculateTier(user.totalXP),
            XPForCurrentLevel = xpForCurrentLevel,
            XPForNextLevel = xpForNextLevel,
            ProgressPercent = progressPercent
        };

        return Result<XPSummaryDto>.Success(summary);
    }

    public XPRewardsConfigDto GetRewardsConfig()
    {
        return new XPRewardsConfigDto
        {
            Rewards = new Dictionary<string, int>(DefaultRewards)
        };
    }

    public LevelInfoDto GetLevelInfo(int level)
    {
        return new LevelInfoDto
        {
            Level = level,
            XPRequired = GetXPRequiredForLevel(level)
        };
    }

    /// <summary>
    /// Calculate level from total XP using exponential curve: XP for level N = floor(100 * N^1.5)
    /// </summary>
    public int CalculateLevel(int totalXP)
    {
        if (totalXP < 100)
            return 0;

        var level = 0;
        while (GetXPRequiredForLevel(level + 1) <= totalXP)
        {
            level++;
        }
        return level;
    }

    /// <summary>
    /// Calculate tier from total XP based on XP ranges.
    /// BRONZE: 0-999, SILVER: 1000-4999, GOLD: 5000-14999, PLATINUM: 15000-49999, DIAMOND: 50000+
    /// </summary>
    public UserTier CalculateTier(int totalXP)
    {
        return totalXP switch
        {
            >= 50000 => UserTier.DIAMOND,
            >= 15000 => UserTier.PLATINUM,
            >= 5000 => UserTier.GOLD,
            >= 1000 => UserTier.SILVER,
            _ => UserTier.BRONZE
        };
    }

    /// <summary>
    /// Get the XP required to reach a specific level.
    /// Formula: floor(100 * N^1.5)
    /// </summary>
    private static int GetXPRequiredForLevel(int level)
    {
        if (level <= 0)
            return 0;

        return (int)Math.Floor(100 * Math.Pow(level, 1.5));
    }
}
