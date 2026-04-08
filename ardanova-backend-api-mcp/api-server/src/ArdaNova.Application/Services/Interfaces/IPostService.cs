namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IPostService
{
    Task<Result<PagedResult<PostDto>>> GetFeedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<PostDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PostDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<PostDto>> CreateAsync(CreatePostDto dto, CancellationToken ct = default);
    Task<Result<PostDto>> UpdateAsync(string id, string authorId, UpdatePostDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, string authorId, CancellationToken ct = default);
    Task<Result<PostDto>> LikeAsync(string postId, string userId, CancellationToken ct = default);
    Task<Result<PostDto>> UnlikeAsync(string postId, string userId, CancellationToken ct = default);
    Task<Result<PostDto>> ShareAsync(string postId, string userId, CreatePostShareDto? dto, CancellationToken ct = default);
    Task<Result<bool>> BookmarkAsync(string postId, string userId, CancellationToken ct = default);
    Task<Result<bool>> UnbookmarkAsync(string postId, string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PostCommentDto>>> GetCommentsAsync(string postId, CancellationToken ct = default);
    Task<Result<PostCommentDto>> AddCommentAsync(string postId, CreatePostCommentDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteCommentAsync(string postId, string commentId, string authorId, CancellationToken ct = default);
}
