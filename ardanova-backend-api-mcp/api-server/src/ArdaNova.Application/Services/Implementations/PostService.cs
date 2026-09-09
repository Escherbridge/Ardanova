using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ArdaNova.Application.Services.Interfaces
{
    /// <summary>Viewer-aware post reads, used by the project field-notes stream.</summary>
    /// <remarks>
    /// Declared here rather than beside <see cref="IPostService"/> because this change set does not own
    /// that file; <see cref="PostService"/> implements both and <c>PostsController</c> resolves this
    /// surface from the registered <see cref="IPostService"/>. Fold these methods into
    /// <see cref="IPostService"/> when that file is next opened.
    /// </remarks>
    public interface IPostFieldNotesService
    {
        Task<Result<PagedResult<PostDto>>> GetProjectFieldNotesAsync(
            string projectId,
            string? viewerId,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<Result<PostDto>> GetForViewerAsync(string postId, string? viewerId, CancellationToken ct = default);

        Task<Result<IReadOnlyList<PostDto>>> GetByAuthorForViewerAsync(
            string authorId,
            string? viewerId,
            CancellationToken ct = default);

        Task<Result<IReadOnlyList<PostCommentDto>>> GetCommentsForViewerAsync(
            string postId,
            string? viewerId,
            CancellationToken ct = default);
    }
}

namespace ArdaNova.Application.Services.Implementations
{
    public class PostService : IPostService, IPostFieldNotesService
    {
        private const int MaximumPageSize = 100;

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

        /// <summary>Legacy viewer-less feed: the anonymous PUBLIC subset only, never a members-only post.</summary>
        public async Task<Result<PagedResult<PostDto>>> GetFeedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var (safePage, safePageSize) = ClampPaging(page, pageSize);
            var query = _posts.Query()
                .Where(PostVisibilityPolicy.VisibleTo(null))
                .Where(OnAVisibleProject(null))
                .OrderByDescending(p => p.createdAt);
            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(ct);
            return Result<PagedResult<PostDto>>.Success(
                new PagedResult<PostDto>(MapPosts(items), total, safePage, safePageSize));
        }

        /// <summary>Field notes for one project, filtered to what this viewer may read.</summary>
        public async Task<Result<PagedResult<PostDto>>> GetProjectFieldNotesAsync(
            string projectId,
            string? viewerId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return Result<PagedResult<PostDto>>.ValidationError("A project is required");

            var (safePage, safePageSize) = ClampPaging(page, pageSize);
            var query = _posts.Query()
                .Where(p => p.projectId == projectId)
                .Where(PostVisibilityPolicy.VisibleTo(viewerId))
                .Where(OnAVisibleProject(viewerId))
                .OrderByDescending(p => p.isPinned)
                .ThenByDescending(p => p.createdAt);
            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(ct);
            return Result<PagedResult<PostDto>>.Success(
                new PagedResult<PostDto>(MapPosts(items), total, safePage, safePageSize));
        }

        /// <summary>Legacy viewer-less read: PUBLIC posts that belong to no project.</summary>
        /// <remarks>
        /// This loads one row without its project, so it cannot apply <see cref="OnAVisibleProject"/> and
        /// would serve a DRAFT project's notes if it were ever routed. It refuses project-scoped posts
        /// instead; <see cref="GetForViewerAsync"/> is the seam that can answer them.
        /// </remarks>
        public async Task<Result<PostDto>> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var post = await _posts.GetByIdAsync(id, ct);
            if (post is null || !PostVisibilityPolicy.IsPubliclyVisible(post) || post.projectId is not null)
                return Result<PostDto>.NotFound($"Post {id} not found");

            return Result<PostDto>.Success(await RecordViewAsync(post, ct));
        }

        public async Task<Result<PostDto>> GetForViewerAsync(string postId, string? viewerId, CancellationToken ct = default)
        {
            var post = await LoadVisibleAsync(postId, viewerId, ct);
            if (post is null)
                return Result<PostDto>.NotFound($"Post {postId} not found");

            return Result<PostDto>.Success(await RecordViewAsync(post, ct));
        }

        /// <summary>Legacy viewer-less read: PUBLIC posts by one author only.</summary>
        public async Task<Result<IReadOnlyList<PostDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        {
            Expression<Func<Post, bool>> publicByAuthor =
                p => p.authorId == userId && p.visibility == PostVisibility.PUBLIC;
            var list = await _posts.FindAsync(publicByAuthor.And(OnAVisibleProject(null)), ct);
            var ordered = list.OrderByDescending(p => p.createdAt).ToList();
            return Result<IReadOnlyList<PostDto>>.Success(MapPosts(ordered));
        }

        public async Task<Result<IReadOnlyList<PostDto>>> GetByAuthorForViewerAsync(
            string authorId,
            string? viewerId,
            CancellationToken ct = default)
        {
            var items = await _posts.Query()
                .Where(p => p.authorId == authorId)
                .Where(PostVisibilityPolicy.VisibleTo(viewerId))
                .Where(OnAVisibleProject(viewerId))
                .OrderByDescending(p => p.createdAt)
                .ToListAsync(ct);
            return Result<IReadOnlyList<PostDto>>.Success(MapPosts(items));
        }

        /// <summary>Creates a post for dto.AuthorId, which the controller binds to the signed actor.</summary>
        public async Task<Result<PostDto>> CreateAsync(CreatePostDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.AuthorId))
                return Result<PostDto>.Unauthorized("An author is required");
            if (string.IsNullOrWhiteSpace(dto.Content))
                return Result<PostDto>.ValidationError("A field note needs something to say");
            if (!PostVisibilityPolicy.HasCoherentAudience(dto.Visibility, dto.ProjectId, dto.GuildId))
                return Result<PostDto>.ValidationError(
                    "A members-only note must belong to the project or group whose members may read it");

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

        /// <summary>Updates a post; authorId is the signed actor, never a request value.</summary>
        public async Task<Result<PostDto>> UpdateAsync(string id, string authorId, UpdatePostDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(authorId))
                return Result<PostDto>.Unauthorized("A signed-in author is required");

            var post = await LoadVisibleAsync(id, authorId, ct);
            if (post is null)
                return Result<PostDto>.NotFound($"Post {id} not found");
            if (!string.Equals(post.authorId, authorId, StringComparison.Ordinal))
                return Result<PostDto>.Forbidden("Only the author can update this post");

            var nextVisibility = dto.Visibility ?? post.visibility;
            if (!PostVisibilityPolicy.HasCoherentAudience(nextVisibility, post.projectId, post.guildId))
                return Result<PostDto>.ValidationError(
                    "A members-only note must belong to the project or group whose members may read it");

            if (dto.Content is not null && string.IsNullOrWhiteSpace(dto.Content))
                return Result<PostDto>.ValidationError("A field note needs something to say");

            if (dto.Title is not null) post.title = dto.Title;
            if (dto.Content is not null) post.content = dto.Content;
            post.visibility = nextVisibility;
            if (dto.Metadata is not null) post.metadata = dto.Metadata;
            if (dto.IsPinned is not null) post.isPinned = dto.IsPinned.Value;
            post.updatedAt = DateTime.UtcNow;

            await _posts.UpdateAsync(post, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
        }

        public async Task<Result<bool>> DeleteAsync(string id, string authorId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(authorId))
                return Result<bool>.Unauthorized("A signed-in author is required");

            var post = await LoadVisibleAsync(id, authorId, ct);
            if (post is null)
                return Result<bool>.NotFound($"Post {id} not found");
            if (!string.Equals(post.authorId, authorId, StringComparison.Ordinal))
                return Result<bool>.Forbidden("Only the author can delete this post");

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
            var post = await LoadVisibleAsync(postId, userId, ct);
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
            var post = await LoadVisibleAsync(postId, userId, ct);
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
            var post = await LoadVisibleAsync(postId, userId, ct);
            if (post is null)
                return Result<PostDto>.NotFound($"Post {postId} not found");

            // Passing a members-only note on would move it outside the audience its author chose.
            if (post.visibility != PostVisibility.PUBLIC)
                return Result<PostDto>.Forbidden("Only a note shared with everyone can be passed on");

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
            var post = await LoadVisibleAsync(postId, userId, ct);
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

        /// <summary>Legacy viewer-less read: comments on PUBLIC posts that belong to no project.</summary>
        /// <remarks>Refuses project-scoped posts for the reason given on <see cref="GetByIdAsync"/>.</remarks>
        public async Task<Result<IReadOnlyList<PostCommentDto>>> GetCommentsAsync(string postId, CancellationToken ct = default)
        {
            var post = await _posts.GetByIdAsync(postId, ct);
            if (post is null || !PostVisibilityPolicy.IsPubliclyVisible(post) || post.projectId is not null)
                return Result<IReadOnlyList<PostCommentDto>>.NotFound($"Post {postId} not found");

            return Result<IReadOnlyList<PostCommentDto>>.Success(await LoadCommentsAsync(postId, ct));
        }

        public async Task<Result<IReadOnlyList<PostCommentDto>>> GetCommentsForViewerAsync(
            string postId,
            string? viewerId,
            CancellationToken ct = default)
        {
            var post = await LoadVisibleAsync(postId, viewerId, ct);
            if (post is null)
                return Result<IReadOnlyList<PostCommentDto>>.NotFound($"Post {postId} not found");

            return Result<IReadOnlyList<PostCommentDto>>.Success(await LoadCommentsAsync(postId, ct));
        }

        /// <summary>Adds a comment for dto.AuthorId, which the controller binds to the signed actor.</summary>
        public async Task<Result<PostCommentDto>> AddCommentAsync(string postId, CreatePostCommentDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.AuthorId))
                return Result<PostCommentDto>.Unauthorized("A signed-in author is required");
            if (string.IsNullOrWhiteSpace(dto.Content))
                return Result<PostCommentDto>.ValidationError("A reply needs something to say");

            var post = await LoadVisibleAsync(postId, dto.AuthorId, ct);
            if (post is null)
                return Result<PostCommentDto>.NotFound($"Post {postId} not found");

            if (dto.ParentId is not null)
            {
                var parent = await _comments.GetByIdAsync(dto.ParentId, ct);
                if (parent is null || !string.Equals(parent.postId, postId, StringComparison.Ordinal))
                    return Result<PostCommentDto>.ValidationError("The reply target is not on this post");
            }

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
            if (string.IsNullOrWhiteSpace(authorId))
                return Result<bool>.Unauthorized("A signed-in author is required");

            var post = await LoadVisibleAsync(postId, authorId, ct);
            if (post is null)
                return Result<bool>.NotFound($"Post {postId} not found");

            var comment = await _comments.GetByIdAsync(commentId, ct);
            if (comment is null || !string.Equals(comment.postId, postId, StringComparison.Ordinal))
                return Result<bool>.NotFound("Comment not found");

            if (!string.Equals(comment.authorId, authorId, StringComparison.Ordinal)
                && !string.Equals(post.authorId, authorId, StringComparison.Ordinal))
                return Result<bool>.Forbidden("Not allowed to delete this comment");

            await _comments.DeleteAsync(comment, ct);
            post.commentsCount = Math.Max(0, post.commentsCount - 1);
            post.updatedAt = DateTime.UtcNow;
            await _posts.UpdateAsync(post, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        /// <summary>The one read gate: a post resolves only when both the post and its project admit this viewer.</summary>
        private Task<Post?> LoadVisibleAsync(string postId, string? viewerId, CancellationToken ct)
            => _posts.Query()
                .Where(p => p.id == postId)
                .Where(PostVisibilityPolicy.VisibleTo(viewerId))
                .Where(OnAVisibleProject(viewerId))
                .FirstOrDefaultAsync(ct);

        /// <summary>Lifts the project visibility rule onto a post: a draft's notes are as private as the draft.</summary>
        /// <remarks>
        /// Composed from <see cref="ProjectQueryFilters.VisibleTo"/> - the one definition of what a draft is -
        /// rather than restated here, so the note stream and the project page cannot drift apart. A post with
        /// no project passes; every other post inherits its project's answer. Admin reach is deliberately not
        /// modelled: a note read resolves an admin as a plain viewer, which fails closed.
        /// </remarks>
        private static Expression<Func<Post, bool>> OnAVisibleProject(string? viewerId)
        {
            var post = Expression.Parameter(typeof(Post), "post");
            var project = Expression.Property(post, nameof(Post.Project));
            var rule = ProjectQueryFilters.VisibleTo(new ProjectViewer(viewerId, IsAdmin: false));
            var body = Expression.OrElse(
                Expression.Equal(project, Expression.Constant(null, typeof(Project))),
                ProjectParameterRebinder.Rebind(rule.Body, rule.Parameters[0], project));

            return Expression.Lambda<Func<Post, bool>>(body, post);
        }

        /// <summary>Rewrites a project-rooted predicate to read through a post's project navigation.</summary>
        private sealed class ProjectParameterRebinder : ExpressionVisitor
        {
            private readonly ParameterExpression _replaced;
            private readonly Expression _replacement;

            private ProjectParameterRebinder(ParameterExpression replaced, Expression replacement)
            {
                _replaced = replaced;
                _replacement = replacement;
            }

            public static Expression Rebind(
                Expression body,
                ParameterExpression replaced,
                Expression replacement)
                => new ProjectParameterRebinder(replaced, replacement).Visit(body)!;

            protected override Expression VisitParameter(ParameterExpression node)
                => ReferenceEquals(node, _replaced) ? _replacement : base.VisitParameter(node);
        }

        private async Task<IReadOnlyList<PostCommentDto>> LoadCommentsAsync(string postId, CancellationToken ct)
        {
            var list = await _comments.FindAsync(c => c.postId == postId, ct);
            return list
                .OrderBy(c => c.createdAt)
                .Select(c => _mapper.Map<PostCommentDto>(c))
                .ToList();
        }

        private async Task<PostDto> RecordViewAsync(Post post, CancellationToken ct)
        {
            post.viewsCount += 1;
            post.updatedAt = DateTime.UtcNow;
            await _posts.UpdateAsync(post, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return _mapper.Map<PostDto>(post);
        }

        private IReadOnlyList<PostDto> MapPosts(IEnumerable<Post> posts)
            => posts.Select(p => _mapper.Map<PostDto>(p)).ToList();

        private static (int Page, int PageSize) ClampPaging(int page, int pageSize)
            => (Math.Max(1, page), Math.Clamp(pageSize, 1, MaximumPageSize));
    }
}
