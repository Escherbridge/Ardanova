namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _postService.GetFeedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _postService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _postService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto, CancellationToken ct)
    {
        var result = await _postService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromQuery] string authorId, [FromBody] UpdatePostDto dto, CancellationToken ct)
    {
        var result = await _postService.UpdateAsync(id, authorId, dto, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string authorId, CancellationToken ct)
    {
        var result = await _postService.DeleteAsync(id, authorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{id}/like")]
    public async Task<IActionResult> Like(string id, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _postService.LikeAsync(id, userId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}/like")]
    public async Task<IActionResult> Unlike(string id, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _postService.UnlikeAsync(id, userId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> Share(string id, [FromQuery] string userId, [FromBody] CreatePostShareDto? dto, CancellationToken ct)
    {
        var result = await _postService.ShareAsync(id, userId, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/bookmark")]
    public async Task<IActionResult> Bookmark(string id, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _postService.BookmarkAsync(id, userId, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}/bookmark")]
    public async Task<IActionResult> Unbookmark(string id, [FromQuery] string userId, CancellationToken ct)
    {
        var result = await _postService.UnbookmarkAsync(id, userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(string id, CancellationToken ct)
    {
        var result = await _postService.GetCommentsAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(string id, [FromBody] CreatePostCommentDto dto, CancellationToken ct)
    {
        var result = await _postService.AddCommentAsync(id, dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetComments), new { id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{id}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string id, string commentId, [FromQuery] string authorId, CancellationToken ct)
    {
        var result = await _postService.DeleteCommentAsync(id, commentId, authorId, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
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
            _ => BadRequest(new { error = result.Error })
        };
    }
}
