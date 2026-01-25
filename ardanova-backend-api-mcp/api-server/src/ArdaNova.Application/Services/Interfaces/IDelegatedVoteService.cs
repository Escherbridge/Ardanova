namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IDelegatedVoteService
{
    Task<Result<DelegatedVoteDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegatorIdAsync(string delegatorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegateeIdAsync(string delegateeId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<decimal>> GetTotalDelegatedPowerAsync(string delegateeId, string projectId, CancellationToken ct = default);
    Task<Result<DelegatedVoteDto>> CreateAsync(CreateDelegatedVoteDto dto, CancellationToken ct = default);
    Task<Result<DelegatedVoteDto>> UpdateAsync(string id, UpdateDelegatedVoteDto dto, CancellationToken ct = default);
    Task<Result<DelegatedVoteDto>> RevokeAsync(string id, CancellationToken ct = default);
}
