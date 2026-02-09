namespace ArdaNova.Application.Services.Implementations;

using AutoMapper;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Application.Common.Interfaces;

public class UserFollowService : IUserFollowService
{
    private readonly IRepository<UserFollow> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserFollowService(IRepository<UserFollow> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<UserFollowDto>>> GetFollowersAsync(string userId, CancellationToken ct = default)
    {
        var follows = await _repository.FindAsync(f => f.followingId == userId, ct);
        return Result<IReadOnlyList<UserFollowDto>>.Success(_mapper.Map<IReadOnlyList<UserFollowDto>>(follows));
    }

    public async Task<Result<IReadOnlyList<UserFollowDto>>> GetFollowingAsync(string userId, CancellationToken ct = default)
    {
        var follows = await _repository.FindAsync(f => f.followerId == userId, ct);
        return Result<IReadOnlyList<UserFollowDto>>.Success(_mapper.Map<IReadOnlyList<UserFollowDto>>(follows));
    }

    public async Task<Result<UserFollowDto>> FollowAsync(CreateUserFollowDto dto, CancellationToken ct = default)
    {
        if (dto.FollowerId == dto.FollowingId)
            return Result<UserFollowDto>.ValidationError("Cannot follow yourself");

        var existing = await _repository.FindOneAsync(
            f => f.followerId == dto.FollowerId && f.followingId == dto.FollowingId, ct);
        if (existing is not null)
            return Result<UserFollowDto>.ValidationError("Already following this user");

        var follow = new UserFollow
        {
            id = Guid.NewGuid().ToString(),
            followerId = dto.FollowerId,
            followingId = dto.FollowingId,
            createdAt = DateTime.UtcNow
        };

        await _repository.AddAsync(follow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserFollowDto>.Success(_mapper.Map<UserFollowDto>(follow));
    }

    public async Task<Result<bool>> UnfollowAsync(string followerId, string followingId, CancellationToken ct = default)
    {
        var follow = await _repository.FindOneAsync(
            f => f.followerId == followerId && f.followingId == followingId, ct);
        if (follow is null)
            return Result<bool>.NotFound("Follow relationship not found");

        await _repository.DeleteAsync(follow, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> IsFollowingAsync(string followerId, string followingId, CancellationToken ct = default)
    {
        var follow = await _repository.FindOneAsync(
            f => f.followerId == followerId && f.followingId == followingId, ct);
        return Result<bool>.Success(follow is not null);
    }

    public async Task<Result<UserFollowCountsDto>> GetFollowCountsAsync(string userId, CancellationToken ct = default)
    {
        var followers = await _repository.FindAsync(f => f.followingId == userId, ct);
        var following = await _repository.FindAsync(f => f.followerId == userId, ct);
        return Result<UserFollowCountsDto>.Success(new UserFollowCountsDto
        {
            FollowersCount = followers.Count,
            FollowingCount = following.Count
        });
    }
}
