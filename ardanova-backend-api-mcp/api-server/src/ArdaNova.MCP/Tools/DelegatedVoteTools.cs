namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class DelegatedVoteTools
{
    private readonly IDelegatedVoteService _delegatedVoteService;

    public DelegatedVoteTools(IDelegatedVoteService delegatedVoteService)
    {
        _delegatedVoteService = delegatedVoteService;
    }

    [McpServerTool(Name = "delegation_get_by_id")]
    [Description("Retrieves a vote delegation by its unique identifier")]
    public async Task<DelegatedVoteDto?> GetDelegationById(
        [Description("The delegation ID")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _delegatedVoteService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "delegation_get_by_delegator")]
    [Description("Retrieves all vote delegations from a user")]
    public async Task<IReadOnlyList<DelegatedVoteDto>?> GetDelegationsByDelegator(
        [Description("The delegator user ID")] Guid delegatorId,
        CancellationToken ct = default)
    {
        var result = await _delegatedVoteService.GetByDelegatorIdAsync(delegatorId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "delegation_get_by_delegatee")]
    [Description("Retrieves all vote delegations to a user")]
    public async Task<IReadOnlyList<DelegatedVoteDto>?> GetDelegationsByDelegatee(
        [Description("The delegatee user ID")] Guid delegateeId,
        CancellationToken ct = default)
    {
        var result = await _delegatedVoteService.GetByDelegateeIdAsync(delegateeId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "delegation_get_active_by_project")]
    [Description("Retrieves all active vote delegations for a project")]
    public async Task<IReadOnlyList<DelegatedVoteDto>?> GetActiveDelegationsByProject(
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var result = await _delegatedVoteService.GetActiveByProjectIdAsync(projectId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "delegation_get_total_power")]
    [Description("Gets the total delegated voting power for a user in a project")]
    public async Task<decimal> GetTotalDelegatedPower(
        [Description("The delegatee user ID")] Guid delegateeId,
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var result = await _delegatedVoteService.GetTotalDelegatedPowerAsync(delegateeId, projectId, ct);
        return result.IsSuccess ? result.Value : 0;
    }

    [McpServerTool(Name = "delegation_create")]
    [Description("Creates a new vote delegation")]
    public async Task<DelegatedVoteDto?> CreateDelegation(
        [Description("The project ID")] Guid projectId,
        [Description("The delegator user ID")] Guid delegatorId,
        [Description("The delegatee user ID")] Guid delegateeId,
        [Description("The token ID")] Guid tokenId,
        [Description("The amount to delegate")] decimal amount,
        [Description("Optional expiration date")] DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        var dto = new CreateDelegatedVoteDto
        {
            ProjectId = projectId,
            DelegatorId = delegatorId,
            DelegateeId = delegateeId,
            TokenId = tokenId,
            Amount = amount,
            ExpiresAt = expiresAt
        };
        var result = await _delegatedVoteService.CreateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "delegation_revoke")]
    [Description("Revokes a vote delegation")]
    public async Task<DelegatedVoteDto?> RevokeDelegation(
        [Description("The delegation ID")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _delegatedVoteService.RevokeAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
