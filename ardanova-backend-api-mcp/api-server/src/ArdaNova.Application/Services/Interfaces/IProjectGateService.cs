namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IProjectGateService
{
    /// <summary>Check if funding threshold is met and advance FUNDING → ACTIVE.</summary>
    Task<Result<GateTransitionResultDto>> EvaluateGate1Async(string projectTokenConfigId, CancellationToken ct = default);

    /// <summary>Admin/community marks project as succeeded: ACTIVE → SUCCEEDED.</summary>
    Task<Result<GateTransitionResultDto>> ClearGate2Async(string projectTokenConfigId, string verifiedByUserId, CancellationToken ct = default);

    /// <summary>Mark project as failed: ACTIVE → FAILED. Burns founder tokens and triggers investor trust protection.</summary>
    Task<Result<GateTransitionResultDto>> FailProjectAsync(string projectTokenConfigId, string reason, CancellationToken ct = default);

    Task<Result<ProjectGateStatusDto>> GetGateStatusAsync(string projectTokenConfigId, CancellationToken ct = default);
}
