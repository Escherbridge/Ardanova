namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The credential lifecycle operations that reach outside ArdaNova: re-submitting a
/// verification-pending credential for issuance, and reading the proof recorded against one.
/// </summary>
/// <remarks>
/// <para>Issuing and withdrawing live on <see cref="MembershipCredentialsController"/>, which
/// routes both through this controller's service. What is left here is the pair of operations
/// that only make sense once a credential exists.</para>
/// <para>Authorization is the same <see cref="CredentialScopeAuthorizer"/> the other seam uses,
/// and the collaborators are optional constructor parameters so that a controller built without
/// them refuses rather than proceeding unauthorized.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class CredentialUtilityController : ControllerBase
{
    private readonly ICredentialUtilityService _credentialUtilityService;
    private readonly IMembershipCredentialService? _credentials;
    private readonly CredentialScopeAuthorizer? _authorizer;

    public CredentialUtilityController(
        ICredentialUtilityService credentialUtilityService,
        IMembershipCredentialService? credentials = null,
        IProjectService? projects = null,
        IHierarchyAuthorizationService? hierarchy = null,
        IGuildService? guilds = null,
        IGuildMemberService? guildMembers = null)
    {
        _credentialUtilityService = credentialUtilityService;
        _credentials = credentials;
        _authorizer = CredentialScopeAuthorizer.TryCreate(projects, hierarchy, guilds, guildMembers);
    }

    /// <summary>Re-submits a verification-pending project credential for issuance.</summary>
    /// <remarks>
    /// Only reachable for a credential that has no proof yet, so a successful retry can add a
    /// proof but can never replace one — which is what stops a retry being a way to rewrite an
    /// issuance that already happened.
    /// </remarks>
    /// <response code="200">Issued; the credential now carries a proof reference.</response>
    /// <response code="400">Already verified, or the issuance did not complete.</response>
    /// <response code="403">Caller does not manage the project.</response>
    [HttpPost("project/{projectId}/user/{userId}/reissue")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> ReissueForProject(string projectId, string userId, CancellationToken ct)
        => ReissueAsync(projectId, null, userId, ct);

    /// <summary>Re-submits a verification-pending guild credential for issuance.</summary>
    [HttpPost("guild/{guildId}/user/{userId}/reissue")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> ReissueForGuild(string guildId, string userId, CancellationToken ct)
        => ReissueAsync(null, guildId, userId, ct);

    // ========================================================================
    // Body-scoped legacy surface. Superseded by the scope-scoped routes here
    // and on MembershipCredentialsController; each of these took its scope,
    // its recipient or its authority from the request body.
    // ========================================================================

    [HttpPost("grant-and-mint")]
    public IActionResult GrantAndMint([FromBody] GrantMembershipCredentialDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/revoke-and-burn")]
    public IActionResult RevokeAndBurn(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPatch("{id}/upgrade-tier")]
    public IActionResult UpgradeTier(string id, [FromBody] UpdateCredentialTierDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("check-auto-grant")]
    public IActionResult CheckAutoGrant([FromBody] CheckAutoGrantRequestDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/retry-mint")]
    public IActionResult RetryMint(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    /// <summary>The proof recorded against one credential, for its holder or the scope's managers.</summary>
    /// <remarks>
    /// This response carries issuance identifiers. It is a disclosure surface behind the member's
    /// own "View proof", not a page anybody browses, so it is scoped to the holder and to the
    /// people who administer the scope that issued it.
    /// </remarks>
    [HttpGet("{id}/chain-data")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetChainData(string id, CancellationToken ct)
    {
        if (_authorizer is null || _credentials is null)
            return ProofLookupUnavailable();

        var credential = await _credentials.GetByIdAsync(id, ct);
        if (credential.IsFailure)
            return ToActionResult(credential);

        // The holder always sees their own proof; anyone else must administer the issuing scope.
        if (!string.Equals(credential.Value!.UserId, ActorId, StringComparison.Ordinal))
        {
            var authorized = await _authorizer.AuthorizeScopeAdminAsync(
                ActorId, IsAdmin, credential.Value!.ProjectId, credential.Value!.GuildId, ct);
            if (authorized.IsFailure)
                return ToActionResult(authorized);
        }

        var result = await _credentialUtilityService.GetCredentialWithChainDataAsync(id, ct);
        return ToActionResult(result);
    }

    private async Task<IActionResult> ReissueAsync(
        string? projectId,
        string? guildId,
        string holderUserId,
        CancellationToken ct)
    {
        if (_authorizer is null || _credentials is null)
            return MutationUnavailable();

        var authorized = await _authorizer.AuthorizeScopeAdminAsync(ActorId, IsAdmin, projectId, guildId, ct);
        if (authorized.IsFailure)
            return ToActionResult(authorized);

        // Found by scope, not by a caller-supplied id, so an authorized manager of one project
        // cannot re-submit a credential that belongs to another.
        var credential = !string.IsNullOrWhiteSpace(projectId)
            ? await _credentials.GetByProjectAndUserAsync(projectId, holderUserId, ct)
            : await _credentials.GetByGuildAndUserAsync(guildId!, holderUserId, ct);
        if (credential.IsFailure)
            return ToActionResult(credential);

        var result = await _credentialUtilityService.RetryMintAsync(credential.Value!.Id, ct);
        return ToActionResult(result);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private IActionResult MutationUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Credential lifecycle mutations are unavailable",
            detail: "Actor-bound scope, server-derived grant authority, and an auditable idempotent "
                  + "credential transition are required before this mutation surface can be enabled. "
                  + "The scope-scoped reissue routes and the membership credential routes carry all "
                  + "three and supersede these.");

    private IActionResult ProofLookupUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Credential proof lookup is unavailable",
            detail: "The scope authorization collaborators are not available, so this disclosure "
                  + "cannot be limited to the credential holder and the scope's managers.");

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
