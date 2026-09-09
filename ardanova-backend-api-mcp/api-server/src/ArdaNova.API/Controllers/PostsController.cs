namespace ArdaNova.API.Controllers;

using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Field notes: project-scoped posts. Writes need a signed actor; reads are filtered to that reader.</summary>
[ApiController]
[Route("api/posts")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IPostFieldNotesService _fieldNotes;
    private readonly IProjectVisibility _projectVisibility;
    private readonly IHierarchyAuthorizationService _hierarchyAuthorization;
    private readonly IGuildService _guildService;
    private readonly IGuildMemberService _guildMemberService;

    public PostsController(
        IPostService postService,
        IPostFieldNotesService fieldNotes,
        IProjectVisibility projectVisibility,
        IHierarchyAuthorizationService hierarchyAuthorization,
        IGuildService guildService,
        IGuildMemberService guildMemberService)
    {
        _postService = postService;
        // The viewer-aware read surface lives on PostService until IPostService can declare
        // it; DI binds both interfaces to the same scoped instance.
        _fieldNotes = fieldNotes;
        _projectVisibility = projectVisibility;
        _hierarchyAuthorization = hierarchyAuthorization;
        _guildService = guildService;
        _guildMemberService = guildMemberService;
    }

    /// <summary>A project field-notes stream. There is no global feed.</summary>
    /// <remarks>
    /// Readable without an actor; the policy then admits only PUBLIC notes. Post visibility alone is not
    /// enough: a note on a DRAFT project defaults to PUBLIC, so the project's own visibility is consulted
    /// first and a viewer who may not see the draft is told it is absent, never that it is forbidden.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("/api/projects/{projectId}/field-notes")]
    public async Task<IActionResult> GetProjectFieldNotes(
        string projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (await ProjectNotVisibleAsync(projectId, ct) is { } hidden)
            return hidden;

        return ToActionResult(
            await _fieldNotes.GetProjectFieldNotesAsync(projectId, ViewerId, page, pageSize, ct));
    }

    /// <summary>NotFound when this viewer may not see the project, because its existence is private too.</summary>
    private async Task<IActionResult?> ProjectNotVisibleAsync(string projectId, CancellationToken ct)
    {
        var project = await _projectVisibility.GetVisibleByIdAsync(Viewer, projectId, ct);
        return project.IsSuccess
            ? null
            : NotFound(new { error = $"Project with id {projectId} not found" });
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        return ToActionResult(await _fieldNotes.GetForViewerAsync(id, ViewerId, ct));
    }

    [AllowAnonymous]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        return ToActionResult(await _fieldNotes.GetByAuthorForViewerAsync(userId, ViewerId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        if (!await ActorMayPostToAsync(request.ProjectId, request.GuildId, ct))
            return Forbid();

        var result = await _postService.CreateAsync(new CreatePostDto
        {
            AuthorId = ActorId,
            ProjectId = request.ProjectId,
            GuildId = request.GuildId,
            Type = request.Type ?? PostType.POST,
            Visibility = request.Visibility ?? PostVisibility.PUBLIC,
            Title = request.Title,
            Content = request.Content,
            Metadata = request.Metadata
        }, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePostDto dto, CancellationToken ct)
    {
        return ToActionResult(await _postService.UpdateAsync(id, ActorId, dto, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _postService.DeleteAsync(id, ActorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/like")]
    public async Task<IActionResult> Like(string id, CancellationToken ct)
    {
        return ToActionResult(await _postService.LikeAsync(id, ActorId, ct));
    }

    [HttpDelete("{id}/like")]
    public async Task<IActionResult> Unlike(string id, CancellationToken ct)
    {
        return ToActionResult(await _postService.UnlikeAsync(id, ActorId, ct));
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> Share(string id, [FromBody] CreatePostShareDto? dto, CancellationToken ct)
    {
        return ToActionResult(await _postService.ShareAsync(id, ActorId, dto, ct));
    }

    [HttpPost("{id}/bookmark")]
    public async Task<IActionResult> Bookmark(string id, CancellationToken ct)
    {
        return ToActionResult(await _postService.BookmarkAsync(id, ActorId, ct));
    }

    [HttpDelete("{id}/bookmark")]
    public async Task<IActionResult> Unbookmark(string id, CancellationToken ct)
    {
        return ToActionResult(await _postService.UnbookmarkAsync(id, ActorId, ct));
    }

    [AllowAnonymous]
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(string id, CancellationToken ct)
    {
        return ToActionResult(await _fieldNotes.GetCommentsForViewerAsync(id, ViewerId, ct));
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(string id, [FromBody] AddPostCommentRequest request, CancellationToken ct)
    {
        var result = await _postService.AddCommentAsync(id, new CreatePostCommentDto
        {
            AuthorId = ActorId,
            ParentId = request.ParentId,
            Content = request.Content
        }, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetComments), new { id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{id}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string id, string commentId, CancellationToken ct)
    {
        var result = await _postService.DeleteCommentAsync(id, commentId, ActorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>Only a member of the project, or of the group, may add to its record.</summary>
    private async Task<bool> ActorMayPostToAsync(string? projectId, string? guildId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(projectId)
            && !await _hierarchyAuthorization.IsProjectMemberAsync(ActorId, projectId, ct))
            return false;

        if (!string.IsNullOrWhiteSpace(guildId) && !await ActorBelongsToGuildAsync(guildId, ct))
            return false;

        return true;
    }

    private async Task<bool> ActorBelongsToGuildAsync(string guildId, CancellationToken ct)
    {
        var guild = await _guildService.GetByIdAsync(guildId, ct);
        if (!guild.IsSuccess)
            return false;
        if (string.Equals(guild.Value!.OwnerId, ActorId, StringComparison.Ordinal))
            return true;

        var memberships = await _guildMemberService.GetByUserIdAsync(ActorId, ct);
        return memberships.IsSuccess && memberships.Value!.Any(membership =>
            string.Equals(membership.GuildId, guildId, StringComparison.Ordinal));
    }

    /// <summary>The signed actor, or null when nobody is signed in. Reads degrade to PUBLIC.</summary>
    private string? ViewerId =>
        ActorAssertionMiddleware.TryGetActorId(User, out var actorId) ? actorId : null;

    /// <summary>The same viewer the project routes read for, so both seams agree on what a draft is.</summary>
    private ProjectViewer Viewer => ViewerId is { } actorId
        ? new ProjectViewer(actorId, User.IsInRole(UserRole.ADMIN.ToString()))
        : ProjectViewer.Anonymous;

    /// <summary>The signed actor on an actor-gated route; the policy guarantees one is present.</summary>
    private string ActorId => ViewerId!;

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
            _ => BadRequest(new { error = result.Error })
        };
    }

    /// <summary>Create payload with no author field: the author is the signed actor.</summary>
    public sealed record CreatePostRequest(
        string Content,
        string? ProjectId,
        string? GuildId,
        PostType? Type,
        PostVisibility? Visibility,
        string? Title,
        string? Metadata);

    /// <summary>Reply payload with no author field: the author is the signed actor.</summary>
    public sealed record AddPostCommentRequest(string Content, string? ParentId);
}
