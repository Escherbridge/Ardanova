namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IMembershipCredentialService
{
    Task<Result<MembershipCredentialDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> GetByProjectAndUserAsync(string projectId, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipCredentialDto>>> GetActiveByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> GrantAsync(GrantMembershipCredentialDto dto, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> RevokeAsync(string id, RevokeMembershipCredentialDto? dto = null, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> SuspendAsync(string id, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> ReactivateAsync(string id, CancellationToken ct = default);
    Task<Result<MembershipCredentialDto>> UpdateMintInfoAsync(string id, UpdateMembershipCredentialMintDto dto, CancellationToken ct = default);
}
