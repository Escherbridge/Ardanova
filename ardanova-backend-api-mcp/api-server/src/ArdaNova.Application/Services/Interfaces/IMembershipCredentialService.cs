namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IMembershipCredentialService
{
    // Project queries
    Task<Result<MembershipCredentialDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> GetByProjectAndUserAsync(string projectId, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default);

    // Guild queries
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByGuildIdAsync(string guildId, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> GetByGuildAndUserAsync(string guildId, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetActiveByGuildIdAsync(string guildId, CancellationToken ct = default);

    // Lifecycle
    Task<Result<MembershipCredentialDto>> GrantAsync(GrantMembershipCredentialDto dto, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> RevokeAsync(string id, RevokeMembershipCredentialDto? dto = null, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> SuspendAsync(string id, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> ReactivateAsync(string id, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> UpdateMintInfoAsync(string id, UpdateMembershipCredentialMintDto dto, CancellationToken ct = default);

    // Tier management
    Task<Result<MembershipCredentialDto>> UpdateTierAsync(string id, UpdateCredentialTierDto dto, CancellationToken ct = default);

    // Eligibility
    Task<Result<CredentialEligibilityDto>> CheckEligibilityAsync(string userId, string? projectId, string? guildId, CancellationToken ct = default);
}
