namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class LeaderboardService : ILeaderboardService
{
    private readonly IRepository<Leaderboard> _leaderboardRepository;
    private readonly IRepository<LeaderboardEntry> _entryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LeaderboardService(
        IRepository<Leaderboard> leaderboardRepository,
        IRepository<LeaderboardEntry> entryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _leaderboardRepository = leaderboardRepository;
        _entryRepository = entryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<LeaderboardDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var leaderboard = await _leaderboardRepository.GetByIdAsync(id, ct);
        if (leaderboard is null)
            return Result<LeaderboardDto>.NotFound($"Leaderboard with id {id} not found");
        return Result<LeaderboardDto>.Success(_mapper.Map<LeaderboardDto>(leaderboard));
    }

    public async Task<Result<IReadOnlyList<LeaderboardDto>>> GetByPeriodAsync(LeaderboardPeriod period, CancellationToken ct = default)
    {
        var leaderboards = await _leaderboardRepository.FindAsync(lb => lb.period == period, ct);
        return Result<IReadOnlyList<LeaderboardDto>>.Success(
            _mapper.Map<IReadOnlyList<LeaderboardDto>>(leaderboards));
    }

    public async Task<Result<IReadOnlyList<LeaderboardDto>>> GetByCategoryAsync(LeaderboardCategory category, CancellationToken ct = default)
    {
        var leaderboards = await _leaderboardRepository.FindAsync(lb => lb.category == category, ct);
        return Result<IReadOnlyList<LeaderboardDto>>.Success(
            _mapper.Map<IReadOnlyList<LeaderboardDto>>(leaderboards));
    }

    public async Task<Result<LeaderboardDto>> CreateAsync(CreateLeaderboardDto dto, CancellationToken ct = default)
    {
        var leaderboard = new Leaderboard
        {
            id = Guid.NewGuid().ToString(),
            period = dto.Period,
            category = dto.Category,
            startDate = dto.StartDate,
            endDate = dto.EndDate,
            createdAt = DateTime.UtcNow
        };

        await _leaderboardRepository.AddAsync(leaderboard, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LeaderboardDto>.Success(_mapper.Map<LeaderboardDto>(leaderboard));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var leaderboard = await _leaderboardRepository.GetByIdAsync(id, ct);
        if (leaderboard is null)
            return Result<bool>.NotFound($"Leaderboard with id {id} not found");

        await _leaderboardRepository.DeleteAsync(leaderboard, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<LeaderboardEntryDto>>> GetEntriesAsync(string leaderboardId, CancellationToken ct = default)
    {
        var entries = await _entryRepository.FindAsync(e => e.leaderboardId == leaderboardId, ct);
        return Result<IReadOnlyList<LeaderboardEntryDto>>.Success(
            _mapper.Map<IReadOnlyList<LeaderboardEntryDto>>(entries));
    }

    public async Task<Result<LeaderboardEntryDto>> AddEntryAsync(CreateLeaderboardEntryDto dto, CancellationToken ct = default)
    {
        // Get existing entries to calculate rank (higher score = better rank = lower number)
        var existingEntries = await _entryRepository.FindAsync(
            e => e.leaderboardId == dto.LeaderboardId, ct);

        var rank = 1;
        foreach (var existing in existingEntries)
        {
            if (existing.score >= dto.Score)
                rank++;
        }

        var entry = new LeaderboardEntry
        {
            id = Guid.NewGuid().ToString(),
            leaderboardId = dto.LeaderboardId,
            userId = dto.UserId,
            score = dto.Score,
            rank = rank,
            metadata = dto.Metadata
        };

        await _entryRepository.AddAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LeaderboardEntryDto>.Success(_mapper.Map<LeaderboardEntryDto>(entry));
    }

    public async Task<Result<LeaderboardEntryDto>> UpdateEntryAsync(string entryId, UpdateLeaderboardEntryDto dto, CancellationToken ct = default)
    {
        var entry = await _entryRepository.GetByIdAsync(entryId, ct);
        if (entry is null)
            return Result<LeaderboardEntryDto>.NotFound($"LeaderboardEntry with id {entryId} not found");

        if (dto.Score.HasValue)
            entry.score = dto.Score.Value;
        if (dto.Rank.HasValue)
            entry.rank = dto.Rank.Value;
        if (dto.Metadata is not null)
            entry.metadata = dto.Metadata;

        await _entryRepository.UpdateAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<LeaderboardEntryDto>.Success(_mapper.Map<LeaderboardEntryDto>(entry));
    }

    public async Task<Result<bool>> DeleteEntryAsync(string entryId, CancellationToken ct = default)
    {
        var entry = await _entryRepository.GetByIdAsync(entryId, ct);
        if (entry is null)
            return Result<bool>.NotFound($"LeaderboardEntry with id {entryId} not found");

        await _entryRepository.DeleteAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<LeaderboardEntryDto>>> GetUserRankingsAsync(string userId, CancellationToken ct = default)
    {
        var entries = await _entryRepository.FindAsync(e => e.userId == userId, ct);
        return Result<IReadOnlyList<LeaderboardEntryDto>>.Success(
            _mapper.Map<IReadOnlyList<LeaderboardEntryDto>>(entries));
    }
}
