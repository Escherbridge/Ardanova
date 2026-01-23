namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITaskEscrowService
{
    Task<Result<TaskEscrowDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskEscrowDto>>> GetByFunderIdAsync(Guid funderId, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> ReleaseAsync(Guid id, ReleaseEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> DisputeAsync(Guid id, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> RefundAsync(Guid id, RefundEscrowDto dto, CancellationToken ct = default);
}
