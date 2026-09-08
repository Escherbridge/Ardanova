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
/// Membership credentials: who holds one, and the scope-scoped routes that issue and withdraw them.
/// </summary>
/// <remarks>
/// <para><b>The scope lives in the route, never in a body.</b> The original
/// <c>POST /api/membershipcredentials</c> took the project, the recipient, the grant reason and a
/// proposal id from the request body, which is four different ways for a caller to nominate an
/// authority they do not have. It stays refused. The working routes below name the scope and the
/// recipient in the path and carry no body at all, so there is nothing a caller can say that
/// redirects the grant.</para>
/// <para><b>Grant authority is derived, never asserted.</b>
/// <see cref="CredentialScopeAuthorizer.AuthorizeGrantAsync"/> reads ArdaNova's own records to
/// decide both questions — may this actor administer the scope, and does the recipient's standing
/// in it support a credential — and returns the <see cref="MembershipGrantType"/> that follows.
/// The same authorizer answers for <c>CredentialUtilityController</c>, so the two mutation seams
/// cannot reach different answers.</para>
/// <para><b>Fail closed by construction.</b> The authorization collaborators are optional
/// constructor parameters. If any is missing the controller cannot ask the question, so every
/// mutation returns 501 rather than proceeding unauthorized.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class MembershipCredentialsController : ControllerBase
{
    private readonly IMembershipCredentialService _membershipCredentialService;
    private readonly ICredentialUtilityService? _credentialUtility;
    private readonly CredentialScopeAuthorizer? _authorizer;

    public MembershipCredentialsController(
        IMembershipCredentialService membershipCredentialService,
        ICredentialUtilityService? credentialUtility = null,
        IProjectService? projects = null,
        IHierarchyAuthorizationService? hierarchy = null,
        IGuildService? guilds = null,
        IGuildMemberService? guildMembers = null)
    {
        _membershipCredentialService = membershipCredentialService;
        _credentialUtility = credentialUtility;
        _authorizer = CredentialScopeAuthorizer.TryCreate(projects, hierarchy, guilds, guildMembers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}/active")]
    public async Task<IActionResult> GetActiveByProjectId(string projectId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetActiveByProjectIdAsync(projectId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("project/{projectId}/user/{userId}")]
    public async Task<IActionResult> GetByProjectAndUser(string projectId, string userId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByProjectAndUserAsync(projectId, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}")]
    public async Task<IActionResult> GetByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}/active")]
    public async Task<IActionResult> GetActiveByGuildId(string guildId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetActiveByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("guild/{guildId}/user/{userId}")]
    public async Task<IActionResult> GetByGuildAndUser(string guildId, string userId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.GetByGuildAndUserAsync(guildId, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("eligibility")]
    public async Task<IActionResult> CheckEligibility([FromQuery] string userId, [FromQuery] string? projectId, [FromQuery] string? guildId, CancellationToken ct)
    {
        var result = await _membershipCredentialService.CheckEligibilityAsync(userId, projectId, guildId, ct);
        return ToActionResult(result);
    }

    // ========================================================================
    // Scope-scoped lifecycle. Scope and recipient come from the route; nothing
    // is read from a request body on any of these.
    // ========================================================================

    /// <summary>Issues a membership credential to a member of <paramref name="projectId"/>.</summary>
    /// <response code="200">Issued. A credential without a proof reference is verification-pending, not failed.</response>
    /// <response code="403">Caller does not manage the project, or the recipient is not a member of it.</response>
    /// <response code="501">Authorization collaborators are unavailable; nothing was changed.</response>
    [HttpPost("project/{projectId}/user/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> GrantForProject(string projectId, string userId, CancellationToken ct)
        => GrantAsync(projectId, null, userId, ct);

    /// <summary>Issues a membership credential to a member of <paramref name="guildId"/>.</summary>
    [HttpPost("guild/{guildId}/user/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> GrantForGuild(string guildId, string userId, CancellationToken ct)
        => GrantAsync(null, guildId, userId, ct);

    /// <summary>Withdraws a project membership credential and returns the issued proof.</summary>
    [HttpDelete("project/{projectId}/user/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> WithdrawForProject(string projectId, string userId, CancellationToken ct)
        => WithdrawAsync(projectId, null, userId, ct);

    /// <summary>Withdraws a guild membership credential and returns the issued proof.</summary>
    [HttpDelete("guild/{guildId}/user/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> WithdrawForGuild(string guildId, string userId, CancellationToken ct)
        => WithdrawAsync(null, guildId, userId, ct);

    /// <summary>Pauses a project membership credential without withdrawing it.</summary>
    [HttpPost("project/{projectId}/user/{userId}/pause")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> PauseForProject(string projectId, string userId, CancellationToken ct)
        => TransitionAsync(projectId, null, userId, (service, id) => service.SuspendAsync(id, ct), ct);

    /// <summary>Restores a paused project membership credential.</summary>
    [HttpPost("project/{projectId}/user/{userId}/resume")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> ResumeForProject(string projectId, string userId, CancellationToken ct)
        => TransitionAsync(projectId, null, userId, (service, id) => service.ReactivateAsync(id, ct), ct);

    /// <summary>Pauses a guild membership credential without withdrawing it.</summary>
    [HttpPost("guild/{guildId}/user/{userId}/pause")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> PauseForGuild(string guildId, string userId, CancellationToken ct)
        => TransitionAsync(null, guildId, userId, (service, id) => service.SuspendAsync(id, ct), ct);

    /// <summary>Restores a paused guild membership credential.</summary>
    [HttpPost("guild/{guildId}/user/{userId}/resume")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public Task<IActionResult> ResumeForGuild(string guildId, string userId, CancellationToken ct)
        => TransitionAsync(null, guildId, userId, (service, id) => service.ReactivateAsync(id, ct), ct);

    // ========================================================================
    // Body-scoped legacy surface. Superseded, not revived: every one of these
    // took its scope, its recipient or its authority from the request body.
    // ========================================================================

    [HttpPost]
    public IActionResult Grant([FromBody] GrantMembershipCredentialDto dto, CancellationToken ct)
    {
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/revoke")]
    public IActionResult Revoke(string id, [FromBody] RevokeMembershipCredentialDto? dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/suspend")]
    public IActionResult Suspend(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPost("{id}/reactivate")]
    public IActionResult Reactivate(string id, CancellationToken ct)
    {
        _ = id;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPatch("{id}/mint")]
    public IActionResult UpdateMintInfo(string id, [FromBody] UpdateMembershipCredentialMintDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    [HttpPatch("{id}/tier")]
    public IActionResult UpdateTier(string id, [FromBody] UpdateCredentialTierDto dto, CancellationToken ct)
    {
        _ = id;
        _ = dto;
        _ = ct;
        return MutationUnavailable();
    }

    // ========================================================================
    // Shared bodies for the scope-scoped routes
    // ========================================================================

    private async Task<IActionResult> GrantAsync(
        string? projectId,
        string? guildId,
        string recipientUserId,
        CancellationToken ct)
    {
        if (_authorizer is null || _credentialUtility is null)
            return MutationUnavailable();

        var authority = await _authorizer.AuthorizeGrantAsync(
            ActorId, IsAdmin, projectId, guildId, recipientUserId, ct);
        if (authority.IsFailure)
            return ToActionResult(authority);

        // Every field is server-side: the scope and recipient came from the route, the grant
        // authority from ArdaNova's records, and grantedByProposalId stays null because no
        // proposal outcome can be quoted at a member until governance can produce one.
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            GuildId = guildId,
            UserId = recipientUserId,
            GrantedVia = authority.Value.ToString(),
            GrantedByProposalId = null
        };

        var result = await _credentialUtility.GrantAndMintAsync(dto, ct);
        return ToActionResult(result);
    }

    private async Task<IActionResult> WithdrawAsync(
        string? projectId,
        string? guildId,
        string holderUserId,
        CancellationToken ct)
    {
        if (_authorizer is null || _credentialUtility is null)
            return MutationUnavailable();

        var credential = await AuthorizeAndLoadAsync(projectId, guildId, holderUserId, ct);
        if (credential.IsFailure)
            return ToActionResult(credential);

        var result = await _credentialUtility.RevokeAndBurnAsync(credential.Value!.Id, ct);
        return ToActionResult(result);
    }

    private async Task<IActionResult> TransitionAsync(
        string? projectId,
        string? guildId,
        string holderUserId,
        Func<IMembershipCredentialService, string, Task<Result<MembershipCredentialDto>>> transition,
        CancellationToken ct)
    {
        if (_authorizer is null)
            return MutationUnavailable();

        var credential = await AuthorizeAndLoadAsync(projectId, guildId, holderUserId, ct);
        if (credential.IsFailure)
            return ToActionResult(credential);

        var result = await transition(_membershipCredentialService, credential.Value!.Id);
        return ToActionResult(result);
    }

    /// <summary>Authorizes the actor over the scope, then loads the credential BY that scope.</summary>
    /// <remarks>
    /// The credential is found from the route scope rather than from an id the caller supplies,
    /// so an authorized project manager cannot reach a credential belonging to another project.
    /// </remarks>
    private async Task<Result<MembershipCredentialDto>> AuthorizeAndLoadAsync(
        string? projectId,
        string? guildId,
        string holderUserId,
        CancellationToken ct)
    {
        var authorized = await _authorizer!.AuthorizeScopeAdminAsync(ActorId, IsAdmin, projectId, guildId, ct);
        if (authorized.IsFailure)
            return Result<MembershipCredentialDto>.Forbidden(authorized.Error!);

        return !string.IsNullOrWhiteSpace(projectId)
            ? await _membershipCredentialService.GetByProjectAndUserAsync(projectId, holderUserId, ct)
            : await _membershipCredentialService.GetByGuildAndUserAsync(guildId!, holderUserId, ct);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool IsAdmin => User.IsInRole(UserRole.ADMIN.ToString());

    private IActionResult MutationUnavailable()
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Membership credential mutations are unavailable",
            detail: "Actor-bound scope, server-derived grant authority, and an auditable idempotent "
                  + "credential transition are required before this mutation surface can be enabled. "
                  + "The scope-scoped routes under project/{projectId}/user/{userId} and "
                  + "guild/{guildId}/user/{userId} carry all three and supersede this one.");

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            // An explicit body, not Forbid(): the refusal reason is the whole point of a
            // credential refusal ("not a member of this project", "verification required").
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

/// <summary>
/// The single authority check behind both credential mutation seams: may this actor administer
/// the scope, and what grant authority do ArdaNova's own records support for this recipient?
/// </summary>
/// <remarks>
/// <para>Declared once and shared by <see cref="MembershipCredentialsController"/> and
/// <c>CredentialUtilityController</c> deliberately. Two copies of an authorization rule is how
/// one of them ends up a version behind the other.</para>
/// <para>Grant authority is DERIVED here rather than accepted from the caller. A project owner
/// may not staple a credential onto someone who is not in their project, and nobody may claim
/// <c>DAO_VOTE</c> — that authority requires a proposal outcome ArdaNova cannot yet produce, so
/// it is never derived and therefore never granted.</para>
/// </remarks>
internal sealed class CredentialScopeAuthorizer
{
    private readonly IProjectService _projects;
    private readonly IHierarchyAuthorizationService _hierarchy;
    private readonly IGuildService _guilds;
    private readonly IGuildMemberService _guildMembers;

    private CredentialScopeAuthorizer(
        IProjectService projects,
        IHierarchyAuthorizationService hierarchy,
        IGuildService guilds,
        IGuildMemberService guildMembers)
    {
        _projects = projects;
        _hierarchy = hierarchy;
        _guilds = guilds;
        _guildMembers = guildMembers;
    }

    /// <summary>Null when any collaborator is missing, so the caller must fail closed.</summary>
    internal static CredentialScopeAuthorizer? TryCreate(
        IProjectService? projects,
        IHierarchyAuthorizationService? hierarchy,
        IGuildService? guilds,
        IGuildMemberService? guildMembers)
        => projects is null || hierarchy is null || guilds is null || guildMembers is null
            ? null
            : new CredentialScopeAuthorizer(projects, hierarchy, guilds, guildMembers);

    /// <summary>May <paramref name="actorId"/> administer the named scope?</summary>
    internal async Task<Result<bool>> AuthorizeScopeAdminAsync(
        string actorId,
        bool actorIsAdmin,
        string? projectId,
        string? guildId,
        CancellationToken ct)
    {
        var scope = ValidateScope(projectId, guildId);
        if (scope is not null)
            return Result<bool>.ValidationError(scope);

        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var project = await _projects.GetByIdAsync(projectId, ct);
            if (project.IsFailure)
                return Result<bool>.NotFound($"Project {projectId} was not found");

            if (!actorIsAdmin && !await _hierarchy.CanManageProjectAsync(actorId, projectId, ct))
                return Result<bool>.Forbidden("Only someone who manages this project can change its membership credentials.");

            return Result<bool>.Success(true);
        }

        var guild = await _guilds.GetByIdAsync(guildId!, ct);
        if (guild.IsFailure)
            return Result<bool>.NotFound($"Guild {guildId} was not found");

        if (!actorIsAdmin && !string.Equals(guild.Value!.OwnerId, actorId, StringComparison.Ordinal))
            return Result<bool>.Forbidden("Only the guild owner can change this guild's membership credentials.");

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Authorizes the actor over the scope AND derives the grant authority the recipient's own
    /// standing supports. The returned value is the only grant reason the caller may record.
    /// </summary>
    internal async Task<Result<MembershipGrantType>> AuthorizeGrantAsync(
        string actorId,
        bool actorIsAdmin,
        string? projectId,
        string? guildId,
        string recipientUserId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
            return Result<MembershipGrantType>.ValidationError("A recipient is required.");

        var authorized = await AuthorizeScopeAdminAsync(actorId, actorIsAdmin, projectId, guildId, ct);
        if (authorized.IsFailure)
            return authorized.Type switch
            {
                ResultType.NotFound => Result<MembershipGrantType>.NotFound(authorized.Error!),
                ResultType.ValidationError => Result<MembershipGrantType>.ValidationError(authorized.Error!),
                _ => Result<MembershipGrantType>.Forbidden(authorized.Error!)
            };

        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var project = await _projects.GetByIdAsync(projectId, ct);
            if (project.IsFailure)
                return Result<MembershipGrantType>.NotFound($"Project {projectId} was not found");

            if (string.Equals(project.Value!.CreatedById, recipientUserId, StringComparison.Ordinal))
                return Result<MembershipGrantType>.Success(MembershipGrantType.FOUNDER);

            if (await _hierarchy.IsProjectMemberAsync(recipientUserId, projectId, ct))
                return Result<MembershipGrantType>.Success(MembershipGrantType.APPLICATION_APPROVED);

            return Result<MembershipGrantType>.Forbidden(
                "A membership credential can only be issued to someone who is already on this project. "
              + "Add them to the project first.");
        }

        var guild = await _guilds.GetByIdAsync(guildId!, ct);
        if (guild.IsFailure)
            return Result<MembershipGrantType>.NotFound($"Guild {guildId} was not found");

        if (string.Equals(guild.Value!.OwnerId, recipientUserId, StringComparison.Ordinal))
            return Result<MembershipGrantType>.Success(MembershipGrantType.FOUNDER);

        var members = await _guildMembers.GetByGuildIdAsync(guildId!, ct);
        if (members.IsSuccess && members.Value!.Any(member =>
                string.Equals(member.UserId, recipientUserId, StringComparison.Ordinal)))
        {
            return Result<MembershipGrantType>.Success(MembershipGrantType.APPLICATION_APPROVED);
        }

        return Result<MembershipGrantType>.Forbidden(
            "A membership credential can only be issued to someone who is already in this guild. "
          + "Add them to the guild first.");
    }

    /// <summary>Exactly one scope, always. Returns the complaint, or null when the scope is sound.</summary>
    private static string? ValidateScope(string? projectId, string? guildId)
    {
        var hasProject = !string.IsNullOrWhiteSpace(projectId);
        var hasGuild = !string.IsNullOrWhiteSpace(guildId);
        return hasProject == hasGuild
            ? "A membership credential belongs to exactly one project or one guild."
            : null;
    }
}
