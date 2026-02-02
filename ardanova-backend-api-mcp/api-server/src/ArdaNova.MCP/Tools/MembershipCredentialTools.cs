namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class MembershipCredentialTools
{
    private readonly IMembershipCredentialService _membershipCredentialService;

    public MembershipCredentialTools(IMembershipCredentialService membershipCredentialService)
    {
        _membershipCredentialService = membershipCredentialService;
    }

    [McpServerTool(Name = "membership_credential_get_by_id")]
    [Description("Retrieves a membership credential by its unique identifier")]
    public async Task<MembershipCredentialDto?> GetCredentialById(
        [Description("The membership credential ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_get_by_project")]
    [Description("Retrieves all membership credentials for a project")]
    public async Task<IReadOnlyList<MembershipCredentialDto>?> GetCredentialsByProject(
        [Description("The project ID")] string projectId,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.GetByProjectIdAsync(projectId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_get_active_by_project")]
    [Description("Retrieves all active membership credentials for a project (members with voting rights)")]
    public async Task<IReadOnlyList<MembershipCredentialDto>?> GetActiveCredentialsByProject(
        [Description("The project ID")] string projectId,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.GetActiveByProjectIdAsync(projectId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_get_by_user")]
    [Description("Retrieves all membership credentials for a user across all projects")]
    public async Task<IReadOnlyList<MembershipCredentialDto>?> GetCredentialsByUser(
        [Description("The user ID")] string userId,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_check")]
    [Description("Checks if a user has an active membership credential for a project")]
    public async Task<MembershipCredentialDto?> CheckCredential(
        [Description("The project ID")] string projectId,
        [Description("The user ID")] string userId,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.GetByProjectAndUserAsync(projectId, userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_grant")]
    [Description("Grants a membership credential to a user for a project. GrantedVia must be one of: FOUNDER, DAO_VOTE, CONTRIBUTION_THRESHOLD, APPLICATION_APPROVED, GAME_SDK_THRESHOLD")]
    public async Task<MembershipCredentialDto?> GrantCredential(
        [Description("The project ID")] string projectId,
        [Description("The user ID to grant membership to")] string userId,
        [Description("How membership was earned: FOUNDER, DAO_VOTE, CONTRIBUTION_THRESHOLD, APPLICATION_APPROVED, GAME_SDK_THRESHOLD")] string grantedVia,
        [Description("Optional proposal ID if granted via DAO vote")] string? grantedByProposalId = null,
        CancellationToken ct = default)
    {
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            UserId = userId,
            GrantedVia = grantedVia,
            GrantedByProposalId = grantedByProposalId
        };
        var result = await _membershipCredentialService.GrantAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_revoke")]
    [Description("Revokes a membership credential, removing the user's governance voting rights")]
    public async Task<MembershipCredentialDto?> RevokeCredential(
        [Description("The membership credential ID")] string id,
        [Description("Optional blockchain transaction hash for the revocation")] string? revokeTxHash = null,
        CancellationToken ct = default)
    {
        var dto = revokeTxHash != null ? new RevokeMembershipCredentialDto { RevokeTxHash = revokeTxHash } : null;
        var result = await _membershipCredentialService.RevokeAsync(id, dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_suspend")]
    [Description("Temporarily suspends a membership credential")]
    public async Task<MembershipCredentialDto?> SuspendCredential(
        [Description("The membership credential ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.SuspendAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "membership_credential_reactivate")]
    [Description("Reactivates a suspended membership credential")]
    public async Task<MembershipCredentialDto?> ReactivateCredential(
        [Description("The membership credential ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _membershipCredentialService.ReactivateAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
