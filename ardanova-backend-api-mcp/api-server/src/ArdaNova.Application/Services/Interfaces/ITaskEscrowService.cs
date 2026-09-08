namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITaskEscrowService
{
    Task<Result<TaskEscrowDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> GetByTaskIdAsync(string taskId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskEscrowDto>>> GetByFunderIdAsync(string funderId, CancellationToken ct = default);

    /// <summary>Every escrow currently DISPUTED, across every funder — the admin adjudication queue.</summary>
    Task<Result<IReadOnlyList<TaskEscrowDto>>> GetDisputedAsync(CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> ReleaseAsync(string id, ReleaseEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> DisputeAsync(string id, DisputeEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> RefundAsync(string id, RefundEscrowDto dto, CancellationToken ct = default);

    /// <summary>
    /// An administrator adjudicates a disputed escrow: REFUND returns the held units to the
    /// contributor partition they were drawn from; REJECT dismisses the dispute and restores
    /// FUNDED, leaving the funder's own release action as the normal way to pay the worker.
    /// </summary>
    Task<Result<TaskEscrowDto>> ResolveAsync(string id, ResolveDisputeDto dto, CancellationToken ct = default);
}
