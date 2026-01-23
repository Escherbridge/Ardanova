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

    public async Task<Result<UserStreakDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var streak = await _repository.GetByIdAsync(id, ct);
        if (streak is null)
            return Result<UserStreakDto>.NotFound($"UserStreak with id {id} not found");
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var streak = await _repository.FindOneAsync(s => s.UserId == userId, ct);
        if (streak is null)
            return Result<UserStreakDto>.NotFound($"UserStreak for user {userId} not found");
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> CreateAsync(CreateUserStreakDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(s => s.UserId == dto.UserId, ct);
        if (exists)
            return Result<UserStreakDto>.ValidationError($"Streak already exists for user {dto.UserId}");

        var streak = UserStreak.Create(dto.UserId, dto.StreakType);
        await _repository.AddAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> RecordActivityAsync(Guid userId, CancellationToken ct = default)
    {
        var streak = await _repository.FindOneAsync(s => s.UserId == userId, ct);
        if (streak is null)
        {
            // Auto-create streak if doesn't exist
            streak = UserStreak.Create(userId);
            await _repository.AddAsync(streak, ct);
        }

        streak.RecordActivity();
        await _repository.UpdateAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<UserStreakDto>> ResetStreakAsync(Guid userId, CancellationToken ct = default)
    {
        var streak = await _repository.FindOneAsync(s => s.UserId == userId, ct);
        if (streak is null)
            return Result<UserStreakDto>.NotFound($"UserStreak for user {userId} not found");

        streak.ResetStreak();
        await _repository.UpdateAsync(streak, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserStreakDto>.Success(_mapper.Map<UserStreakDto>(streak));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
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

    public async Task<Result<ReferralDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<IReadOnlyList<ReferralDto>>> GetByReferrerIdAsync(Guid referrerId, CancellationToken ct = default)
    {
        var referrals = await _repository.FindAsync(r => r.ReferrerId == referrerId, ct);
        return Result<IReadOnlyList<ReferralDto>>.Success(_mapper.Map<IReadOnlyList<ReferralDto>>(referrals));
    }

    public async Task<Result<ReferralDto>> GetByReferredIdAsync(Guid referredId, CancellationToken ct = default)
    {
        var referral = await _repository.FindOneAsync(r => r.ReferredId == referredId, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral for referred user {referredId} not found");
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var referral = await _repository.FindOneAsync(r => r.ReferralCode == code, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with code {code} not found");
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> CreateAsync(CreateReferralDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(r => r.ReferredId == dto.ReferredId, ct);
        if (exists)
            return Result<ReferralDto>.ValidationError($"User {dto.ReferredId} already has a referral");

        var referral = Referral.Create(dto.ReferrerId, dto.ReferredId, dto.ReferralCode);
        await _repository.AddAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        referral.Complete();
        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> ClaimRewardAsync(Guid id, ClaimReferralRewardDto dto, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        try
        {
            referral.ClaimReward(dto.XpAmount, dto.TokenAmount);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ReferralDto>.ValidationError(ex.Message);
        }

        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> ExpireAsync(Guid id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        referral.Expire();
        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }

    public async Task<Result<ReferralDto>> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var referral = await _repository.GetByIdAsync(id, ct);
        if (referral is null)
            return Result<ReferralDto>.NotFound($"Referral with id {id} not found");

        referral.Cancel();
        await _repository.UpdateAsync(referral, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }
}
