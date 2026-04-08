namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

public class PostService : IPostService
{
    private readonly IRepository<Post> _posts;
    private readonly IRepository<PostLike> _likes;
    private readonly IRepository<PostComment> _comments;
    private readonly IRepository<PostBookmark> _bookmarks;
    private readonly IRepository<PostShare> _shares;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PostService(
        IRepository<Post> posts,
        IRepository<PostLike> likes,
        IRepository<PostComment> comments,
        IRepository<PostBookmark> bookmarks,
        IRepository<PostShare> shares,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _posts = posts;
        _likes = likes;
        _comments = comments;
        _bookmarks = bookmarks;
        _shares = shares;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<PostDto>>> GetFeedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _posts.Query().OrderByDescending(p => p.createdAt);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var dtos = items.Select(p => _mapper.Map<PostDto>(p)).ToList();
        return Result<PagedResult<PostDto>>.Success(new PagedResult<PostDto>(dtos, total, page, pageSize));
    }

    public async Task<Result<PostDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(id, ct);
        if (post is null)
            return Result<PostDto>.NotFound($"Post {id} not found");

        post.viewsCount += 1;
        post.updatedAt = DateTime.UtcNow;
        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
    }

    public async Task<Result<IReadOnlyList<PostDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var list = await _posts.FindAsync(p => p.authorId == userId, ct);
        var ordered = list.OrderByDescending(p => p.createdAt).ToList();
        return Result<IReadOnlyList<PostDto>>.Success(ordered.Select(p => _mapper.Map<PostDto>(p)).ToList());
    }

    public async Task<Result<PostDto>> CreateAsync(CreatePostDto dto, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var post = new Post
        {
            id = Guid.NewGuid().ToString(),
            authorId = dto.AuthorId,
            projectId = dto.ProjectId,
            guildId = dto.GuildId,
            type = dto.Type,
            visibility = dto.Visibility,
            title = dto.Title,
            content = dto.Content,
            metadata = dto.Metadata,
            likesCount = 0,
            commentsCount = 0,
            sharesCount = 0,
            viewsCount = 0,
            isPinned = false,
            isTrending = false,
            trendingScore = 0,
            createdAt = now,
            updatedAt = now
        };

        await _posts.AddAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
    }

    public async Task<Result<PostDto>> UpdateAsync(string id, string authorId, UpdatePostDto dto, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(id, ct);
        if (post is null)
            return Result<PostDto>.NotFound($"Post {id} not found");
        if (post.authorId != authorId)
            return Result<PostDto>.ValidationError("Only the author can update this post");

        if (dto.Title is not null) post.title = dto.Title;
        if (dto.Content is not null) post.content = dto.Content;
        if (dto.Visibility is not null) post.visibility = dto.Visibility.Value;
        if (dto.Metadata is not null) post.metadata = dto.Metadata;
        if (dto.IsPinned is not null) post.isPinned = dto.IsPinned.Value;
        post.updatedAt = DateTime.UtcNow;

        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
    }

    public async Task<Result<bool>> DeleteAsync(string id, string authorId, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(id, ct);
        if (post is null)
            return Result<bool>.NotFound($"Post {id} not found");
        if (post.authorId != authorId)
            return Result<bool>.ValidationError("Only the author can delete this post");

        var likes = await _likes.FindAsync(l => l.postId == id, ct);
        foreach (var l in likes) await _likes.DeleteAsync(l, ct);

        var comments = await _comments.FindAsync(c => c.postId == id, ct);
        foreach (var c in comments) await _comments.DeleteAsync(c, ct);

        var bookmarks = await _bookmarks.FindAsync(b => b.postId == id, ct);
        foreach (var b in bookmarks) await _bookmarks.DeleteAsync(b, ct);

        var shareRows = await _shares.FindAsync(s => s.postId == id, ct);
        foreach (var s in shareRows) await _shares.DeleteAsync(s, ct);

        await _posts.DeleteAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<PostDto>> LikeAsync(string postId, string userId, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<PostDto>.NotFound($"Post {postId} not found");

        var existing = await _likes.FindOneAsync(l => l.postId == postId && l.userId == userId, ct);
        if (existing is not null)
            return Result<PostDto>.Success(_mapper.Map<PostDto>(post));

        await _likes.AddAsync(new PostLike
        {
            id = Guid.NewGuid().ToString(),
            postId = postId,
            userId = userId,
            createdAt = DateTime.UtcNow
        }, ct);
        post.likesCount += 1;
        post.updatedAt = DateTime.UtcNow;
        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
    }

    public async Task<Result<PostDto>> UnlikeAsync(string postId, string userId, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<PostDto>.NotFound($"Post {postId} not found");

        var existing = await _likes.FindOneAsync(l => l.postId == postId && l.userId == userId, ct);
        if (existing is null)
            return Result<PostDto>.Success(_mapper.Map<PostDto>(post));

        await _likes.DeleteAsync(existing, ct);
        post.likesCount = Math.Max(0, post.likesCount - 1);
        post.updatedAt = DateTime.UtcNow;
        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
    }

    public async Task<Result<PostDto>> ShareAsync(string postId, string userId, CreatePostShareDto? dto, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<PostDto>.NotFound($"Post {postId} not found");

        await _shares.AddAsync(new PostShare
        {
            id = Guid.NewGuid().ToString(),
            postId = postId,
            userId = userId,
            sharedToProjectId = dto?.SharedToProjectId,
            sharedToGuildId = dto?.SharedToGuildId,
            comment = dto?.Comment,
            createdAt = DateTime.UtcNow
        }, ct);
        post.sharesCount += 1;
        post.updatedAt = DateTime.UtcNow;
        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
    }

    public async Task<Result<bool>> BookmarkAsync(string postId, string userId, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<bool>.NotFound($"Post {postId} not found");

        var existing = await _bookmarks.FindOneAsync(b => b.postId == postId && b.userId == userId, ct);
        if (existing is not null)
            return Result<bool>.Success(true);

        await _bookmarks.AddAsync(new PostBookmark
        {
            id = Guid.NewGuid().ToString(),
            postId = postId,
            userId = userId,
            createdAt = DateTime.UtcNow
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UnbookmarkAsync(string postId, string userId, CancellationToken ct = default)
    {
        var existing = await _bookmarks.FindOneAsync(b => b.postId == postId && b.userId == userId, ct);
        if (existing is null)
            return Result<bool>.Success(true);

        await _bookmarks.DeleteAsync(existing, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<PostCommentDto>>> GetCommentsAsync(string postId, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<IReadOnlyList<PostCommentDto>>.NotFound($"Post {postId} not found");

        var list = await _comments.FindAsync(c => c.postId == postId, ct);
        var ordered = list.OrderBy(c => c.createdAt).ToList();
        return Result<IReadOnlyList<PostCommentDto>>.Success(ordered.Select(c => _mapper.Map<PostCommentDto>(c)).ToList());
    }

    public async Task<Result<PostCommentDto>> AddCommentAsync(string postId, CreatePostCommentDto dto, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<PostCommentDto>.NotFound($"Post {postId} not found");

        var now = DateTime.UtcNow;
        var comment = new PostComment
        {
            id = Guid.NewGuid().ToString(),
            postId = postId,
            authorId = dto.AuthorId,
            parentId = dto.ParentId,
            content = dto.Content,
            likesCount = 0,
            createdAt = now,
            updatedAt = now
        };

        await _comments.AddAsync(comment, ct);
        post.commentsCount += 1;
        post.updatedAt = now;
        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PostCommentDto>.Success(_mapper.Map<PostCommentDto>(comment));
    }

    public async Task<Result<bool>> DeleteCommentAsync(string postId, string commentId, string authorId, CancellationToken ct = default)
    {
        var comment = await _comments.GetByIdAsync(commentId, ct);
        if (comment is null || comment.postId != postId)
            return Result<bool>.NotFound("Comment not found");

        var post = await _posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Result<bool>.NotFound($"Post {postId} not found");

        if (comment.authorId != authorId && post.authorId != authorId)
            return Result<bool>.ValidationError("Not allowed to delete this comment");

        await _comments.DeleteAsync(comment, ct);
        post.commentsCount = Math.Max(0, post.commentsCount - 1);
        post.updatedAt = DateTime.UtcNow;
        await _posts.UpdateAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
