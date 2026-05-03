namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IProjectTokenService
{
    Task<Result<ProjectTokenConfigDto>> CreateConfigAsync(CreateProjectTokenConfigDto dto, CancellationToken ct = default);
    Task<Result<ProjectTokenConfigDto>> GetConfigByIdAsync(string id, CancellationToken ct = default);
    Task<Result<ProjectTokenConfigDto>> GetConfigByProjectIdAsync(string projectId, CancellationToken ct = default);

    // Holder-class-aware allocation
    Task<Result<TokenAllocationDto>> AllocateToTaskAsync(string projectTokenConfigId, CreateTokenAllocationDto dto, CancellationToken ct = default);
    Task<Result<TokenAllocationDto>> AllocateToInvestorAsync(string projectTokenConfigId, CreateInvestorAllocationDto dto, CancellationToken ct = default);
    Task<Result<TokenAllocationDto>> AllocateToFounderAsync(string projectTokenConfigId, CreateFounderAllocationDto dto, CancellationToken ct = default);

    Task<Result<TokenAllocationDto>> DistributeAsync(string allocationId, string recipientUserId, CancellationToken ct = default);
    Task<Result<TokenAllocationDto>> RevokeAllocationAsync(string allocationId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TokenAllocationDto>>> GetAllocationsByProjectAsync(string projectTokenConfigId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TokenAllocationDto>>> GetAllocationsByPbiAsync(string pbiId, CancellationToken ct = default);
    Task<Result<ProjectTokenConfigDto>> GetSupplyBreakdownAsync(string projectTokenConfigId, CancellationToken ct = default);

    // Failure handling
    Task<Result<bool>> BurnFounderTokensAsync(string projectTokenConfigId, CancellationToken ct = default);
    Task<Result<bool>> ProcessInvestorTrustProtectionAsync(string projectTokenConfigId, CancellationToken ct = default);

    // Investor queries
    Task<Result<IReadOnlyList<ProjectInvestmentDto>>> GetInvestorsByProjectAsync(string projectTokenConfigId, CancellationToken ct = default);
}
