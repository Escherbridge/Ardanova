namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;

public interface ITaskBidService
{
    Task<Result<TaskBidDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskBidDto>>> GetByTaskIdAsync(string taskId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskBidDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<PagedResult<TaskBidDto>>> SearchAsync(string? taskId, string? guildId, TaskBidStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<Result<TaskBidDto>> CreateAsync(CreateTaskBidDto dto, CancellationToken ct = default);
    Task<Result<TaskBidDto>> UpdateAsync(string id, UpdateTaskBidDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<TaskBidDto>> AcceptAsync(string id, CancellationToken ct = default);
    Task<Result<TaskBidDto>> RejectAsync(string id, CancellationToken ct = default);
    Task<Result<TaskBidDto>> WithdrawAsync(string id, CancellationToken ct = default);
    Task<Result<TaskBidDto>> CompleteAsync(string id, CancellationToken ct = default);
    Task<Result<TaskBidDto>> ReviewAsync(string id, CancellationToken ct = default);
}
