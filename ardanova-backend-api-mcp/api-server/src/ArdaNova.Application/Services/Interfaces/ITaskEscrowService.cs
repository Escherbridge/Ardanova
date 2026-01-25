namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITaskEscrowService
{
    Task<Result<TaskEscrowDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> GetByTaskIdAsync(string taskId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskEscrowDto>>> GetByFunderIdAsync(string funderId, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> CreateAsync(CreateTaskEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> ReleaseAsync(string id, ReleaseEscrowDto dto, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> DisputeAsync(string id, CancellationToken ct = default);
    Task<Result<TaskEscrowDto>> RefundAsync(string id, RefundEscrowDto dto, CancellationToken ct = default);
}
