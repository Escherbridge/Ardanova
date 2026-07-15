namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>Creates or replays the local, effect-free commerce agreement for an accepted opportunity bid.</summary>
public interface ITaskCommerceService
{
    /// <summary>Authorizes the project owner or opportunity poster and atomically creates the accepted bid's task agreement.</summary>
    Task<Result<TaskCommerceAcceptanceDto>> AcceptBidAsync(
        string bidId,
        string actorId,
        CancellationToken ct = default);

    /// <summary>Reads a task agreement only for its contributor or the owning project creator.</summary>
    Task<Result<TaskCommerceViewDto>> GetByTaskIdAsync(
        string taskId,
        string actorId,
        CancellationToken ct = default);
}
