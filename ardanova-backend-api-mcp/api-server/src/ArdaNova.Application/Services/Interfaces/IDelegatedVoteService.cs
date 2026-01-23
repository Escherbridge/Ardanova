namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IDelegatedVoteService
{
    Task<Result<DelegatedVoteDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegatorIdAsync(Guid delegatorId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByDelegateeIdAsync(Guid delegateeId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DelegatedVoteDto>>> GetActiveByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<decimal>> GetTotalDelegatedPowerAsync(Guid delegateeId, Guid projectId, CancellationToken ct = default);
    Task<Result<DelegatedVoteDto>> CreateAsync(CreateDelegatedVoteDto dto, CancellationToken ct = default);
    Task<Result<DelegatedVoteDto>> UpdateAsync(Guid id, UpdateDelegatedVoteDto dto, CancellationToken ct = default);
    Task<Result<DelegatedVoteDto>> RevokeAsync(Guid id, CancellationToken ct = default);
}
