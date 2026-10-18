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

/// <summary>Guild profiles and their children. Reads are public; every mutation is actor-bound and authorised in the guild services (Application/Services/AGENTS.md §Guild authority).</summary>
[ApiController]
[Route("api/[controller]")]
public class GuildsController : ControllerBase
{
    private readonly IGuildService _guildService;
    private readonly IGuildMemberService _memberService;
    private readonly IGuildReviewService _reviewService;
    private readonly IGuildUpdateService _updateService;
    private readonly IGuildApplicationService _applicationService;
    private readonly IGuildInvitationService _invitationService;
    private readonly IGuildFollowService _followService;

    public GuildsController(
        IGuildService guildService,
        IGuildMemberService memberService,
        IGuildReviewService reviewService,
        IGuildUpdateService updateService,
        IGuildApplicationService applicationService,
        IGuildInvitationService invitationService,
        IGuildFollowService followService)
    {
        _guildService = guildService;
        _memberService = memberService;
        _reviewService = reviewService;
        _updateService = updateService;
        _applicationService = applicationService;
        _invitationService = invitationService;
        _followService = followService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => ToActionResult(await _guildService.GetAllAsync(ct));

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => ToActionResult(await _guildService.GetPagedAsync(page, pageSize, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
        => ToActionResult(await _guildService.GetByIdAsync(id, ct));

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        => ToActionResult(await _guildService.GetBySlugAsync(slug, ct));

    [HttpGet("owner/{ownerId}")]
    public async Task<IActionResult> GetByOwnerId(string ownerId, CancellationToken ct)
        => ToActionResult(await _guildService.GetByOwnerIdAsync(ownerId, ct));

    [HttpGet("verified")]
    public async Task<IActionResult> GetVerified(CancellationToken ct)
        => ToActionResult(await _guildService.GetVerifiedAsync(ct));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Create([FromBody] CreateGuildDto dto, CancellationToken ct)
    {
        var result = await _guildService.CreateAsync(Actor, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGuildDto dto, CancellationToken ct)
        => ToActionResult(await _guildService.UpdateAsync(id, Actor, dto, ct));

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _guildService.DeleteAsync(id, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>The verified badge is a platform-operator decision, never a guild's own.</summary>
    [HttpPost("{id}/verify")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> Verify(string id, CancellationToken ct)
        => ToActionResult(await _guildService.VerifyAsync(id, ct));

    // ===== GUILD MEMBERS =====
    [HttpGet("{guildId}/members")]
    public async Task<IActionResult> GetMembers(string guildId, CancellationToken ct)
        => ToActionResult(await _memberService.GetByGuildIdAsync(guildId, ct));

    [HttpGet("{guildId}/members/{memberId}")]
    public async Task<IActionResult> GetMemberById(string guildId, string memberId, CancellationToken ct)
        => ToActionResult(await _memberService.GetByIdAsync(guildId, memberId, ct));

    /// <summary>Platform-operator repair only; people otherwise join through an invitation or an application they made.</summary>
    [HttpPost("{guildId}/members")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AddMember(string guildId, [FromBody] CreateGuildMemberDto dto, CancellationToken ct)
    {
        var result = await _memberService.CreateAsync(guildId, Actor, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMemberById), new { guildId, memberId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{guildId}/members/{memberId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateMember(string guildId, string memberId, [FromBody] UpdateGuildMemberDto dto, CancellationToken ct)
        => ToActionResult(await _memberService.UpdateAsync(guildId, memberId, Actor, dto, ct));

    [HttpDelete("{guildId}/members/{memberId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RemoveMember(string guildId, string memberId, CancellationToken ct)
    {
        var result = await _memberService.DeleteAsync(guildId, memberId, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD REVIEWS =====
    [HttpGet("{guildId}/reviews")]
    public async Task<IActionResult> GetReviews(string guildId, CancellationToken ct)
        => ToActionResult(await _reviewService.GetByGuildIdAsync(guildId, ct));

    [HttpGet("{guildId}/reviews/{reviewId}")]
    public async Task<IActionResult> GetReviewById(string guildId, string reviewId, CancellationToken ct)
        => ToActionResult(await _reviewService.GetByIdAsync(guildId, reviewId, ct));

    [HttpPost("{guildId}/reviews")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateReview(string guildId, [FromBody] CreateGuildReviewDto dto, CancellationToken ct)
    {
        var result = await _reviewService.CreateAsync(guildId, Actor, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetReviewById), new { guildId, reviewId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{guildId}/reviews/{reviewId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UpdateReview(string guildId, string reviewId, [FromBody] UpdateGuildReviewDto dto, CancellationToken ct)
        => ToActionResult(await _reviewService.UpdateAsync(guildId, reviewId, Actor, dto, ct));

    [HttpDelete("{guildId}/reviews/{reviewId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteReview(string guildId, string reviewId, CancellationToken ct)
    {
        var result = await _reviewService.DeleteAsync(guildId, reviewId, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD UPDATES =====
    [HttpGet("{guildId}/updates")]
    public async Task<IActionResult> GetUpdates(string guildId, CancellationToken ct)
        => ToActionResult(await _updateService.GetByGuildIdAsync(guildId, ct));

    [HttpGet("{guildId}/updates/{updateId}")]
    public async Task<IActionResult> GetUpdateById(string guildId, string updateId, CancellationToken ct)
        => ToActionResult(await _updateService.GetByIdAsync(guildId, updateId, ct));

    [HttpPost("{guildId}/updates")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateUpdate(string guildId, [FromBody] CreateGuildUpdateDto dto, CancellationToken ct)
    {
        var result = await _updateService.CreateAsync(guildId, Actor, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUpdateById), new { guildId, updateId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{guildId}/updates/{updateId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteUpdate(string guildId, string updateId, CancellationToken ct)
    {
        var result = await _updateService.DeleteAsync(guildId, updateId, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD APPLICATIONS =====
    [HttpGet("{guildId}/applications")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetApplications(string guildId, CancellationToken ct)
        => ToActionResult(await _applicationService.GetByGuildIdAsync(guildId, Actor, ct));

    /// <summary>The signed actor's own applications to this guild, so a visitor can see what they already asked for.</summary>
    [HttpGet("{guildId}/applications/mine")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMyApplications(string guildId, CancellationToken ct)
        => ToActionResult(await _applicationService.GetMineAsync(guildId, Actor, ct));

    [HttpGet("{guildId}/applications/{applicationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetApplicationById(string guildId, string applicationId, CancellationToken ct)
        => ToActionResult(await _applicationService.GetByIdAsync(guildId, applicationId, Actor, ct));

    [HttpPost("{guildId}/applications")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> SubmitApplication(string guildId, [FromBody] CreateGuildApplicationDto dto, CancellationToken ct)
    {
        var result = await _applicationService.CreateAsync(guildId, Actor, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetApplicationById), new { guildId, applicationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{guildId}/applications/{applicationId}/accept")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AcceptApplication(string guildId, string applicationId, [FromBody] ReviewGuildApplicationDto? dto, CancellationToken ct)
        => ToActionResult(await _applicationService.AcceptAsync(guildId, applicationId, Actor, dto?.ReviewMessage, ct));

    [HttpPost("{guildId}/applications/{applicationId}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RejectApplication(string guildId, string applicationId, [FromBody] ReviewGuildApplicationDto? dto, CancellationToken ct)
        => ToActionResult(await _applicationService.RejectAsync(guildId, applicationId, Actor, dto?.ReviewMessage, ct));

    [HttpPost("{guildId}/applications/{applicationId}/withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> WithdrawApplication(string guildId, string applicationId, CancellationToken ct)
        => ToActionResult(await _applicationService.WithdrawAsync(guildId, applicationId, Actor, ct));

    [HttpDelete("{guildId}/applications/{applicationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteApplication(string guildId, string applicationId, CancellationToken ct)
    {
        var result = await _applicationService.DeleteAsync(guildId, applicationId, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD INVITATIONS =====
    /// <summary>The signed actor's own invitations that can still be answered.</summary>
    [HttpGet("invitations/mine")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMyInvitations(CancellationToken ct)
        => ToActionResult(await _invitationService.GetPendingForUserAsync(Actor.Id, ct));

    [HttpGet("{guildId}/invitations")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetInvitations(string guildId, CancellationToken ct)
        => ToActionResult(await _invitationService.GetByGuildIdAsync(guildId, Actor, ct));

    [HttpGet("{guildId}/invitations/{invitationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetInvitationById(string guildId, string invitationId, CancellationToken ct)
        => ToActionResult(await _invitationService.GetByIdAsync(guildId, invitationId, Actor, ct));

    [HttpPost("{guildId}/invitations")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateInvitation(string guildId, [FromBody] CreateGuildInvitationDto dto, CancellationToken ct)
    {
        var result = await _invitationService.CreateAsync(guildId, Actor, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetInvitationById), new { guildId, invitationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{guildId}/invitations/{invitationId}/accept")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AcceptInvitation(string guildId, string invitationId, CancellationToken ct)
        => ToActionResult(await _invitationService.AcceptAsync(guildId, invitationId, Actor, ct));

    [HttpPost("{guildId}/invitations/{invitationId}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RejectInvitation(string guildId, string invitationId, CancellationToken ct)
        => ToActionResult(await _invitationService.RejectAsync(guildId, invitationId, Actor, ct));

    [HttpDelete("{guildId}/invitations/{invitationId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> DeleteInvitation(string guildId, string invitationId, CancellationToken ct)
    {
        var result = await _invitationService.DeleteAsync(guildId, invitationId, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD FOLLOWS =====
    [HttpGet("{guildId}/followers")]
    public async Task<IActionResult> GetFollowers(string guildId, CancellationToken ct)
        => ToActionResult(await _followService.GetByGuildIdAsync(guildId, ct));

    [HttpPost("{guildId}/follow")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> FollowGuild(string guildId, [FromBody] CreateGuildFollowDto? dto, CancellationToken ct)
        => ToActionResult(await _followService.FollowAsync(guildId, Actor, dto ?? new CreateGuildFollowDto(), ct));

    [HttpDelete("{guildId}/follow")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> UnfollowGuild(string guildId, CancellationToken ct)
    {
        var result = await _followService.UnfollowAsync(guildId, Actor, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpGet("{guildId}/follow/check")]
    public async Task<IActionResult> IsFollowing(string guildId, [FromQuery] string userId, CancellationToken ct)
        => ToActionResult(await _followService.IsFollowingAsync(guildId, userId, ct));

    /// <summary>The acting identity, read only from the signed actor assertion.</summary>
    private GuildActor Actor => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        User.IsInRole(UserRole.ADMIN.ToString()));

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error ?? "Not permitted" }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
