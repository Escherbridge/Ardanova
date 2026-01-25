namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class UserStreakService : IUserStreakService
{
    private readonly IRepository<UserStreak> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserStreakService(IRepository<UserStreak> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<UserStreakDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var streak = await _repository.GetByIdAsync(id, ct);
        if (streak is null)
            return Result<UserStreakDto>.NotFound($"UserStreak with id {id} not found");
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var streak = await _repository.FindOneAsync(s => s.userId == userId, ct);
        if (streak is null)
            return Result<UserStreakDto>.NotFound($"UserStreak for user {userId} not found");
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> CreateAsync(CreateUserStreakDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(s => s.userId == dto.UserId, ct);
        if (exists)
            return Result<UserStreakDto>.ValidationError($"Streak already exists for user {dto.UserId}");

        var streak = new UserStreak
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            streakType = dto.StreakType,
            currentStreak = 0,
            longestStreak = 0,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> RecordActivityAsync(string userId, CancellationToken ct = default)
    {
        var streak = await _repository.FindOneAsync(s => s.userId == userId, ct);
        if (streak is null)
        {
            // Auto-create streak if doesn't exist
            streak = new UserStreak
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                streakType = StreakType.DAILY_LOGIN,
                currentStreak = 0,
                longestStreak = 0,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };
            await _repository.AddAsync(streak, ct);
        }

        // Check if this is a consecutive day
        var today = DateTime.UtcNow.Date;
        var lastActivity = streak.lastActivityDate?.Date;

        if (lastActivity == today)
        {
            // Already recorded today, no change
        }
        else if (lastActivity == today.AddDays(-1))
        {
            // Consecutive day
            streak.currentStreak++;
            if (streak.currentStreak > streak.longestStreak)
                streak.longestStreak = streak.currentStreak;
        }
        else
        {
            // Streak broken, reset to 1
            streak.currentStreak = 1;
        }

        streak.lastActivityDate = DateTime.UtcNow;
        streak.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> ResetStreakAsync(string userId, CancellationToken ct = default)
    {
        var streak = await _repository.FindOneAsync(s => s.userId == userId, ct);
        if (streak is null)
            return Result<UserStreakDto>.NotFound($"UserStreak for user {userId} not found");

        streak.currentStreak = 0;
        streak.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var streak = await _repository.GetByIdAsync(id, ct);
        if (streak is null)
            return Result<bool>.NotFound($"UserStreak with id {id} not found");

        await _repository.DeleteAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class ReferralService : IReferralService
{
    private readonly IRepository<Referral> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReferralService(IRepository<Referral> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ReferralDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<IReadOnlyList<ReferralDto>>> GetByReferrerIdAsync(string referrerId, CancellationToken ct = default)
    {
        var referrals = await _repository.FindAsync(r => r.referrerId == referrerId, ct);
        return Result<IReadOnlyList<ReferralDto>>.Success(_mapper.Map<IReadOnlyList<ReferralDto>>(referrals));
    }

    public async Task<Result<ReferralDto>> GetByReferredIdAsync(string referredId, CancellationToken ct = default)
    {
        var referral = await _repository.FindOneAsync(r => r.referredId == referredId, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral for referred user {referredId} not found");
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var referral = await _repository.FindOneAsync(r => r.referralCode == code, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with code {code} not found");
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> CreateAsync(CreateReferralDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(r => r.referredId == dto.ReferredId, ct);
        if (exists)
            return Result<ReferralDto>.ValidationError($"User {dto.ReferredId} already has a referral");

        var referral = new Referral
        {
            id = Guid.NewGuid().ToString(),
            referrerId = dto.ReferrerId,
            referredId = dto.ReferredId,
            referralCode = dto.ReferralCode,
            status = ReferralStatus.PENDING,
            rewardClaimed = false,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> CompleteAsync(string id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        referral.status = ReferralStatus.COMPLETED;
        referral.completedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> ClaimRewardAsync(string id, ClaimReferralRewardDto dto, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        if (referral.status != ReferralStatus.COMPLETED)
            return Result<ReferralDto>.ValidationError("Cannot claim reward for uncompleted referral");

        if (referral.rewardClaimed)
            return Result<ReferralDto>.ValidationError("Reward already claimed");

        referral.rewardClaimed = true;
        referral.xpRewarded = dto.XpAmount;
        referral.tokenRewarded = dto.TokenAmount;

        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> ExpireAsync(string id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        referral.status = ReferralStatus.EXPIRED;

        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        referral.status = ReferralStatus.CANCELLED;

        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }
}
