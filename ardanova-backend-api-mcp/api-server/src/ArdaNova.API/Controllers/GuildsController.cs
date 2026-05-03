namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
    {
        var result = await _guildService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _guildService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _guildService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _guildService.GetBySlugAsync(slug, ct);
        return ToActionResult(result);
    }

    [HttpGet("owner/{ownerId}")]
    public async Task<IActionResult> GetByOwnerId(string ownerId, CancellationToken ct)
    {
        var result = await _guildService.GetByOwnerIdAsync(ownerId, ct);
        return ToActionResult(result);
    }

    [HttpGet("verified")]
    public async Task<IActionResult> GetVerified(CancellationToken ct)
    {
        var result = await _guildService.GetVerifiedAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuildDto dto, CancellationToken ct)
    {
        var result = await _guildService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGuildDto dto, CancellationToken ct)
    {
        var result = await _guildService.UpdateAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string requesterId, CancellationToken ct)
    {
        var result = await _guildService.DeleteAsync(id, requesterId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> Verify(string id, CancellationToken ct)
    {
        var result = await _guildService.VerifyAsync(id, ct);
        return ToActionResult(result);
    }

    // ===== GUILD MEMBERS =====
    [HttpGet("{guildId}/members")]
    public async Task<IActionResult> GetMembers(string guildId, CancellationToken ct)
    {
        var result = await _memberService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{guildId}/members/{memberId}")]
    public async Task<IActionResult> GetMemberById(string guildId, string memberId, CancellationToken ct)
    {
        var result = await _memberService.GetByIdAsync(memberId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/members")]
    public async Task<IActionResult> AddMember(string guildId, [FromBody] CreateGuildMemberDto dto, CancellationToken ct)
    {
        var dtoWithGuild = dto with { GuildId = guildId };
        var result = await _memberService.CreateAsync(dtoWithGuild, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMemberById), new { guildId, memberId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{guildId}/members/{memberId}")]
    public async Task<IActionResult> UpdateMember(string guildId, string memberId, [FromBody] UpdateGuildMemberDto dto, CancellationToken ct)
    {
        var result = await _memberService.UpdateAsync(memberId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{guildId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(string guildId, string memberId, CancellationToken ct)
    {
        var result = await _memberService.DeleteAsync(memberId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD REVIEWS =====
    [HttpGet("{guildId}/reviews")]
    public async Task<IActionResult> GetReviews(string guildId, CancellationToken ct)
    {
        var result = await _reviewService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{guildId}/reviews/{reviewId}")]
    public async Task<IActionResult> GetReviewById(string guildId, string reviewId, CancellationToken ct)
    {
        var result = await _reviewService.GetByIdAsync(reviewId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/reviews")]
    public async Task<IActionResult> CreateReview(string guildId, [FromBody] CreateGuildReviewDto dto, CancellationToken ct)
    {
        var dtoWithGuild = dto with { GuildId = guildId };
        var result = await _reviewService.CreateAsync(dtoWithGuild, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetReviewById), new { guildId, reviewId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{guildId}/reviews/{reviewId}")]
    public async Task<IActionResult> UpdateReview(string guildId, string reviewId, [FromBody] UpdateGuildReviewDto dto, CancellationToken ct)
    {
        var result = await _reviewService.UpdateAsync(reviewId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{guildId}/reviews/{reviewId}")]
    public async Task<IActionResult> DeleteReview(string guildId, string reviewId, CancellationToken ct)
    {
        var result = await _reviewService.DeleteAsync(reviewId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD UPDATES =====
    [HttpGet("{guildId}/updates")]
    public async Task<IActionResult> GetUpdates(string guildId, CancellationToken ct)
    {
        var result = await _updateService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{guildId}/updates/{updateId}")]
    public async Task<IActionResult> GetUpdateById(string guildId, string updateId, CancellationToken ct)
    {
        var result = await _updateService.GetByIdAsync(updateId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/updates")]
    public async Task<IActionResult> CreateUpdate(string guildId, [FromBody] CreateGuildUpdateDto dto, CancellationToken ct)
    {
        var dtoWithGuild = dto with { GuildId = guildId };
        var result = await _updateService.CreateAsync(dtoWithGuild, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUpdateById), new { guildId, updateId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{guildId}/updates/{updateId}")]
    public async Task<IActionResult> DeleteUpdate(string guildId, string updateId, CancellationToken ct)
    {
        var result = await _updateService.DeleteAsync(updateId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD APPLICATIONS =====
    [HttpGet("{guildId}/applications")]
    public async Task<IActionResult> GetApplications(string guildId, CancellationToken ct)
    {
        var result = await _applicationService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{guildId}/applications/{applicationId}")]
    public async Task<IActionResult> GetApplicationById(string guildId, string applicationId, CancellationToken ct)
    {
        var result = await _applicationService.GetByIdAsync(applicationId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/applications")]
    public async Task<IActionResult> SubmitApplication(string guildId, [FromBody] CreateGuildApplicationDto dto, CancellationToken ct)
    {
        var dtoWithGuild = dto with { GuildId = guildId };
        var result = await _applicationService.CreateAsync(dtoWithGuild, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetApplicationById), new { guildId, applicationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{guildId}/applications/{applicationId}/accept")]
    public async Task<IActionResult> AcceptApplication(string guildId, string applicationId, [FromBody] ReviewGuildApplicationDto? dto, CancellationToken ct)
    {
        var result = await _applicationService.AcceptAsync(applicationId, dto?.ReviewMessage, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/applications/{applicationId}/reject")]
    public async Task<IActionResult> RejectApplication(string guildId, string applicationId, [FromBody] ReviewGuildApplicationDto? dto, CancellationToken ct)
    {
        var result = await _applicationService.RejectAsync(applicationId, dto?.ReviewMessage, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{guildId}/applications/{applicationId}")]
    public async Task<IActionResult> DeleteApplication(string guildId, string applicationId, CancellationToken ct)
    {
        var result = await _applicationService.DeleteAsync(applicationId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD INVITATIONS =====
    [HttpGet("{guildId}/invitations")]
    public async Task<IActionResult> GetInvitations(string guildId, CancellationToken ct)
    {
        var result = await _invitationService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{guildId}/invitations/{invitationId}")]
    public async Task<IActionResult> GetInvitationById(string guildId, string invitationId, CancellationToken ct)
    {
        var result = await _invitationService.GetByIdAsync(invitationId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/invitations")]
    public async Task<IActionResult> CreateInvitation(string guildId, [FromBody] CreateGuildInvitationDto dto, CancellationToken ct)
    {
        var dtoWithGuild = dto with { GuildId = guildId };
        var result = await _invitationService.CreateAsync(dtoWithGuild, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetInvitationById), new { guildId, invitationId = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{guildId}/invitations/{invitationId}/accept")]
    public async Task<IActionResult> AcceptInvitation(string guildId, string invitationId, CancellationToken ct)
    {
        var result = await _invitationService.AcceptAsync(invitationId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/invitations/{invitationId}/reject")]
    public async Task<IActionResult> RejectInvitation(string guildId, string invitationId, CancellationToken ct)
    {
        var result = await _invitationService.RejectAsync(invitationId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{guildId}/invitations/{invitationId}")]
    public async Task<IActionResult> DeleteInvitation(string guildId, string invitationId, CancellationToken ct)
    {
        var result = await _invitationService.DeleteAsync(invitationId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    // ===== GUILD FOLLOWS =====
    [HttpGet("{guildId}/followers")]
    public async Task<IActionResult> GetFollowers(string guildId, CancellationToken ct)
    {
        var result = await _followService.GetByGuildIdAsync(guildId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{guildId}/follow")]
    public async Task<IActionResult> FollowGuild(string guildId, [FromBody] CreateGuildFollowDto dto, CancellationToken ct)
    {
        var dtoWithGuild = dto with { GuildId = guildId };
        var result = await _followService.FollowAsync(dtoWithGuild, ct);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    [HttpDelete("{guildId}/follow")]
    public async Task<IActionResult> UnfollowGuild(string guildId, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _followService.UnfollowAsync(guildId, userId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpGet("{guildId}/follow/check")]
    public async Task<IActionResult> IsFollowing(string guildId, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _followService.IsFollowingAsync(guildId, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            ResultType.BadRequest => BadRequest(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
