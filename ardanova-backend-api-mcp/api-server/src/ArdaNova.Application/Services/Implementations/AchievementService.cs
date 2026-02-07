namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class AchievementService : IAchievementService
{
    private readonly IRepository<Achievement> _achievementRepository;
    private readonly IRepository<UserAchievement> _userAchievementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AchievementService(
        IRepository<Achievement> achievementRepository,
        IRepository<UserAchievement> userAchievementRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _achievementRepository = achievementRepository;
        _userAchievementRepository = userAchievementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<AchievementDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetByIdAsync(id, ct);
        if (achievement is null)
            return Result<AchievementDto>.NotFound($"Achievement with id {id} not found");

        return Result<AchievementDto>.Success(_mapper.Map<AchievementDto>(achievement));
    }

    public async Task<Result<IReadOnlyList<AchievementDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.GetAllAsync(ct);
        return Result<IReadOnlyList<AchievementDto>>.Success(
            _mapper.Map<IReadOnlyList<AchievementDto>>(achievements));
    }

    public async Task<Result<IReadOnlyList<AchievementDto>>> GetByCategoryAsync(
        AchievementCategory category, CancellationToken ct = default)
    {
        var achievements = await _achievementRepository.FindAsync(
            a => a.category == category, ct);

        return Result<IReadOnlyList<AchievementDto>>.Success(
            _mapper.Map<IReadOnlyList<AchievementDto>>(achievements));
    }

    public async Task<Result<AchievementDto>> CreateAsync(CreateAchievementDto dto, CancellationToken ct = default)
    {
        var nameExists = await _achievementRepository.ExistsAsync(
            a => a.name == dto.Name, ct);
        if (nameExists)
            return Result<AchievementDto>.ValidationError($"Achievement with name '{dto.Name}' already exists");

        var achievement = new Achievement
        {
            id = Guid.NewGuid().ToString(),
            name = dto.Name,
            description = dto.Description,
            category = dto.Category,
            criteria = dto.Criteria,
            xpReward = dto.XpReward,
            equityReward = dto.EquityReward,
            rarity = dto.Rarity,
            icon = dto.Icon,
            isActive = true,
            createdAt = DateTime.UtcNow
        };

        await _achievementRepository.AddAsync(achievement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AchievementDto>.Success(_mapper.Map<AchievementDto>(achievement));
    }

    public async Task<Result<AchievementDto>> UpdateAsync(string id, UpdateAchievementDto dto, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetByIdAsync(id, ct);
        if (achievement is null)
            return Result<AchievementDto>.NotFound($"Achievement with id {id} not found");

        if (dto.Name is not null) achievement.name = dto.Name;
        if (dto.Description is not null) achievement.description = dto.Description;
        if (dto.Criteria is not null) achievement.criteria = dto.Criteria;
        if (dto.XpReward.HasValue) achievement.xpReward = dto.XpReward.Value;
        if (dto.EquityReward.HasValue) achievement.equityReward = dto.EquityReward.Value;
        if (dto.Rarity.HasValue) achievement.rarity = dto.Rarity.Value;
        if (dto.Icon is not null) achievement.icon = dto.Icon;
        if (dto.IsActive.HasValue) achievement.isActive = dto.IsActive.Value;

        await _achievementRepository.UpdateAsync(achievement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AchievementDto>.Success(_mapper.Map<AchievementDto>(achievement));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetByIdAsync(id, ct);
        if (achievement is null)
            return Result<bool>.NotFound($"Achievement with id {id} not found");

        await _achievementRepository.DeleteAsync(achievement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<UserAchievementDto>>> GetUserAchievementsAsync(
        string userId, CancellationToken ct = default)
    {
        var userAchievements = await _userAchievementRepository.FindAsync(
            ua => ua.userId == userId, ct);

        return Result<IReadOnlyList<UserAchievementDto>>.Success(
            _mapper.Map<IReadOnlyList<UserAchievementDto>>(userAchievements));
    }

    public async Task<Result<UserAchievementDto>> UpdateProgressAsync(
        string userId, string achievementId, UpdateProgressDto dto, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetByIdAsync(achievementId, ct);
        if (achievement is null)
            return Result<UserAchievementDto>.NotFound($"Achievement with id {achievementId} not found");

        var existing = await _userAchievementRepository.FindOneAsync(
            ua => ua.userId == userId && ua.achievementId == achievementId, ct);

        if (existing is null)
        {
            // Create new UserAchievement
            var userAchievement = new UserAchievement
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                achievementId = achievementId,
                progress = dto.Progress,
                earnedAt = dto.Progress >= 100 ? DateTime.UtcNow : null
            };

            await _userAchievementRepository.AddAsync(userAchievement, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<UserAchievementDto>.Success(_mapper.Map<UserAchievementDto>(userAchievement));
        }

        // Update existing
        existing.progress = dto.Progress;
        if (dto.Progress >= 100 && existing.earnedAt is null)
        {
            existing.earnedAt = DateTime.UtcNow;
        }

        await _userAchievementRepository.UpdateAsync(existing, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<UserAchievementDto>.Success(_mapper.Map<UserAchievementDto>(existing));
    }

    public async Task<Result<UserAchievementDto>> AwardAchievementAsync(
        string userId, string achievementId, CancellationToken ct = default)
    {
        var achievement = await _achievementRepository.GetByIdAsync(achievementId, ct);
        if (achievement is null)
            return Result<UserAchievementDto>.NotFound($"Achievement with id {achievementId} not found");

        var existing = await _userAchievementRepository.FindOneAsync(
            ua => ua.userId == userId && ua.achievementId == achievementId, ct);

        if (existing is not null && existing.earnedAt is not null)
            return Result<UserAchievementDto>.ValidationError("User has already earned this achievement");

        if (existing is not null)
        {
            // Update existing to fully awarded
            existing.progress = 100;
            existing.earnedAt = DateTime.UtcNow;

            await _userAchievementRepository.UpdateAsync(existing, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<UserAchievementDto>.Success(_mapper.Map<UserAchievementDto>(existing));
        }

        // Create new fully-awarded user achievement
        var userAchievement = new UserAchievement
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            achievementId = achievementId,
            progress = 100,
            earnedAt = DateTime.UtcNow
        };

        await _userAchievementRepository.AddAsync(userAchievement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<UserAchievementDto>.Success(_mapper.Map<UserAchievementDto>(userAchievement));
    }
}
