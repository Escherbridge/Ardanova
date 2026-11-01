using System.Data.Common;
using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Feed;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

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

        /// <summary>
        /// PostType values a person may choose when authoring a post. Every other value -
        /// MILESTONE, TASK_COMPLETED, and every feed-envelope kind added for SR-03 phase 0
        /// (PROJECT_LAUNCHED onward in database-architecture.dbml) - is written only by a system
        /// process (the feed projector, task/milestone completion), never by a person through
        /// this endpoint. See database-architecture.dbml, Enum PostType, for the full list and the
        /// additive-only ordering it is appended in.
        /// </summary>
        private static readonly HashSet<PostType> UserAuthorablePostTypes = new()
        {
            PostType.POST,
            PostType.PROJECT_UPDATE,
            PostType.GUILD_ACTIVITY,
            PostType.PROPOSAL,
            PostType.SHOP_ITEM,
            // A person's ask for help on one task; its own rules in ResolveAskAsync (Implementations/AGENTS.md §Help wanted).
            PostType.HELP_WANTED
        };

        /// <summary>The 409 code when a task already has its one HELP_WANTED; <c>conflictId</c> carries that ask's id.</summary>
        public const string HelpWantedExistsCode = "HELP_WANTED_EXISTS";

        /// <summary>Why a HELP_WANTED was refused: the task already has one.</summary>
        public const string AskAlreadyOpen = "This task already has an ask.";

        /// <summary>Why a HELP_WANTED was refused: it names no task.</summary>
        public const string AskNeedsATask = "An ask for help needs a task.";

        /// <summary>Why a post was refused: only a HELP_WANTED names a task (the database CHECK backs this).</summary>
        public const string TaskOnlyOnAnAsk = "Only an ask for help names a task.";

        /// <summary>Why a HELP_WANTED was refused: the post's project and the task's differ.</summary>
        public const string AskOnAnotherProject = "That task belongs to another project.";

        /// <summary>Why a HELP_WANTED was refused: the author is not on the task's project.</summary>
        public const string AskNeedsMembership = "Only the project's members can ask for help on its tasks.";

        /// <summary>Why a HELP_WANTED was refused: the task is COMPLETED.</summary>
        public const string AskOnADoneTask = "This task is done.";

        /// <summary>A post a person wrote; feed envelopes (sourceType set) are system rows. See Implementations/AGENTS.md §Posts.</summary>
        private static readonly Expression<Func<Post, bool>> WrittenByAPerson = p => p.sourceType == null;

        private const string UniqueViolationSqlState = "23505";

        /// <summary>Why a person cannot edit or delete a feed envelope that names them as its author.</summary>
        public const string MirroredActivityRefusal =
            "This card mirrors activity elsewhere on ArdaNova; change it where it happened";

        private readonly IRepository<Post> _posts;
        private readonly IRepository<PostLike> _likes;
        private readonly IRepository<PostComment> _comments;
        private readonly IRepository<PostBookmark> _bookmarks;
        private readonly IRepository<PostShare> _shares;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IRepository<ProjectTask> _tasks;
        private readonly IRepository<Project> _projects;
        private readonly IHierarchyAuthorizationService _authorization;
        private readonly INotificationService _notifications;

        /// <param name="tasks">Resolves a HELP_WANTED's task.</param>
        /// <param name="projects">Limits that task to projects its author may see, so a hidden draft reads as missing.</param>
        /// <param name="authorization">Membership for an ask; project management for removing one.</param>
        /// <param name="notifications">Stages the reply notification inside the reply's own save.</param>
        public PostService(
            IRepository<Post> posts,
            IRepository<PostLike> likes,
            IRepository<PostComment> comments,
            IRepository<PostBookmark> bookmarks,
            IRepository<PostShare> shares,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRepository<ProjectTask> tasks,
            IRepository<Project> projects,
            IHierarchyAuthorizationService authorization,
            INotificationService notifications)
        {
            _posts = posts;
            _likes = likes;
            _comments = comments;
            _bookmarks = bookmarks;
            _shares = shares;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tasks = tasks;
            _projects = projects;
            _authorization = authorization;
            _notifications = notifications;
        }

        /// <summary>Legacy viewer-less feed: the anonymous PUBLIC subset only, never a members-only post.</summary>
        public async Task<Result<PagedResult<PostDto>>> GetFeedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var (safePage, safePageSize) = ClampPaging(page, pageSize);
            var query = _posts.Query()
                .Where(WrittenByAPerson)
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
                .Where(WrittenByAPerson)
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
                p => p.authorId == userId && p.visibility == PostVisibility.PUBLIC && p.sourceType == null;
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
                .Where(WrittenByAPerson)
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
            if (!UserAuthorablePostTypes.Contains(dto.Type))
                return Result<PostDto>.ValidationError(
                    $"{dto.Type} is written by the system, not chosen when a person creates a post");
            if (dto.Type != PostType.HELP_WANTED && dto.TaskId is not null)
                return Result<PostDto>.ValidationError(TaskOnlyOnAnAsk);

            var projectId = dto.ProjectId;
            if (dto.Type == PostType.HELP_WANTED)
            {
                var ask = await ResolveAskAsync(dto, ct);
                if (ask.Refusal is not null)
                    return ask.Refusal;
                projectId = ask.ProjectId;
            }

            if (!PostVisibilityPolicy.HasCoherentAudience(dto.Visibility, projectId, dto.GuildId))
                return Result<PostDto>.ValidationError(
                    "A members-only note must belong to the project or group whose members may read it");

            var now = DateTime.UtcNow;
            var post = new Post
            {
                id = Guid.NewGuid().ToString(),
                authorId = dto.AuthorId,
                projectId = projectId,
                taskId = dto.Type == PostType.HELP_WANTED ? dto.TaskId : null,
                guildId = dto.GuildId,
                type = dto.Type,
                visibility = dto.Visibility,
                title = dto.Title,
                content = dto.Content,
                metadata = dto.Metadata,
                hashtags = Hashtags.Extract(dto.Title, dto.Content),
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
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException failure) when (post.taskId is not null && IsUniqueViolation(failure))
            {
                // One ask per task ("Post"."taskId" unique): the answer names the ask that already holds it.
                _unitOfWork.ClearTrackedChanges();
                var existingId = await _posts.Query()
                    .Where(existing => existing.taskId == post.taskId)
                    .Select(existing => existing.id)
                    .FirstOrDefaultAsync(ct);
                if (existingId is null)
                    throw;
                return Result<PostDto>.Conflict(AskAlreadyOpen)
                    .WithCode(HelpWantedExistsCode)
                    .WithReference(existingId);
            }

            return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
        }

        /// <summary>The project a HELP_WANTED lives on (its task's), or why it is refused. See Implementations/AGENTS.md §Help wanted.</summary>
        private async Task<(string? ProjectId, Result<PostDto>? Refusal)> ResolveAskAsync(CreatePostDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.TaskId))
                return (null, Result<PostDto>.ValidationError(AskNeedsATask));

            // 404 before 403: a task on a draft the author cannot see answers exactly like a missing one.
            var visibleProjectIds = _projects.Query()
                .Where(ProjectQueryFilters.VisibleTo(new ProjectViewer(dto.AuthorId, IsAdmin: false)))
                .Select(project => project.id);
            var task = await _tasks.Query()
                .Where(candidate => candidate.id == dto.TaskId && visibleProjectIds.Contains(candidate.projectId))
                .Select(candidate => new { candidate.projectId, candidate.status })
                .FirstOrDefaultAsync(ct);
            if (task is null)
                return (null, Result<PostDto>.NotFound($"Task {dto.TaskId} not found"));
            if (dto.ProjectId is not null && !string.Equals(dto.ProjectId, task.projectId, StringComparison.Ordinal))
                return (null, Result<PostDto>.ValidationError(AskOnAnotherProject));
            if (!await _authorization.IsProjectMemberAsync(dto.AuthorId, task.projectId, ct))
                return (null, Result<PostDto>.Forbidden(AskNeedsMembership));
            if (task.status == TaskStatus.COMPLETED)
                return (null, Result<PostDto>.ValidationError(AskOnADoneTask));

            return (task.projectId, null);
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
            if (post.sourceType is not null)
                return Result<PostDto>.Forbidden(MirroredActivityRefusal);

            var nextVisibility = dto.Visibility ?? post.visibility;
            if (!PostVisibilityPolicy.HasCoherentAudience(nextVisibility, post.projectId, post.guildId))
                return Result<PostDto>.ValidationError(
                    "A members-only note must belong to the project or group whose members may read it");

            if (dto.Content is not null && string.IsNullOrWhiteSpace(dto.Content))
                return Result<PostDto>.ValidationError("A field note needs something to say");

            if (dto.Title is not null) post.title = dto.Title;
            if (dto.Content is not null) post.content = dto.Content;
            post.hashtags = Hashtags.Extract(post.title, post.content);
            post.visibility = nextVisibility;
            if (dto.Metadata is not null) post.metadata = dto.Metadata;
            if (dto.IsPinned is not null) post.isPinned = dto.IsPinned.Value;
            post.updatedAt = DateTime.UtcNow;

            await _posts.UpdateAsync(post, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<PostDto>.Success(_mapper.Map<PostDto>(post));
        }

        /// <summary>Deletes a post for its author; a HELP_WANTED may also be removed by a manager of its project.</summary>
        public async Task<Result<bool>> DeleteAsync(string id, string actorId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                return Result<bool>.Unauthorized("A signed-in author is required");

            var post = await LoadVisibleAsync(id, actorId, ct);
            if (post is null)
                return Result<bool>.NotFound($"Post {id} not found");
            if (!string.Equals(post.authorId, actorId, StringComparison.Ordinal)
                && !await MayRemoveAskAsync(post, actorId, ct))
                return Result<bool>.Forbidden("Only the author can delete this post");
            if (post.sourceType is not null)
                return Result<bool>.Forbidden(MirroredActivityRefusal);

            var likes = await _likes.FindAsync(l => l.postId == id, ct);
            foreach (var l in likes) await _likes.DeleteAsync(l, ct);

            var comments = await _comments.FindAsync(c => c.postId == id, ct);
            foreach (var c in comments) await _comments.DeleteAsync(c, ct);

            var bookmarks = await _bookmarks.FindAsync(b => b.postId == id, ct);
            foreach (var b in bookmarks) await _bookmarks.DeleteAsync(b, ct);

            var shareRows = await _shares.FindAsync(s => s.postId == id, ct);
            foreach (var s in shareRows) await _shares.DeleteAsync(s, ct);

            await _notifications.StageForgetPostAsync(id, ct);
            await _posts.DeleteAsync(post, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        /// <summary>A manager may remove an ask on their project, so one whose author left cannot hold its task forever.</summary>
        private async Task<bool> MayRemoveAskAsync(Post post, string actorId, CancellationToken ct)
            => post.type == PostType.HELP_WANTED
               && post.projectId is not null
               && await _authorization.CanManageProjectAsync(actorId, post.projectId, ct);

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
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException failure) when (IsUniqueViolation(failure))
            {
                // A double tap: the other request stored this person's like first, and its counter bump with it.
                _unitOfWork.ClearTrackedChanges();
                var current = await LoadVisibleAsync(postId, userId, ct);
                return current is null
                    ? Result<PostDto>.NotFound($"Post {postId} not found")
                    : Result<PostDto>.Success(_mapper.Map<PostDto>(current));
            }

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
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // A double tap: the other request deleted this like first (and decremented with it); this save
                // rolled back whole, so the counter is not decremented twice.
                _unitOfWork.ClearTrackedChanges();
                var current = await LoadVisibleAsync(postId, userId, ct);
                return current is null
                    ? Result<PostDto>.NotFound($"Post {postId} not found")
                    : Result<PostDto>.Success(_mapper.Map<PostDto>(current));
            }

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
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException failure) when (IsUniqueViolation(failure))
            {
                // A double tap: the other request saved this bookmark first, which is the state asked for.
                _unitOfWork.ClearTrackedChanges();
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> UnbookmarkAsync(string postId, string userId, CancellationToken ct = default)
        {
            var existing = await _bookmarks.FindOneAsync(b => b.postId == postId && b.userId == userId, ct);
            if (existing is null)
                return Result<bool>.Success(true);

            await _bookmarks.DeleteAsync(existing, ct);
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // A double tap: the other request removed this bookmark first, which is the state asked for.
                _unitOfWork.ClearTrackedChanges();
            }

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

            // A discussion card points at a thread that already exists; a reply here would start a second one.
            if (post.type == PostType.DISCUSSION && post.sourceType is not null)
                return Result<PostCommentDto>.ValidationError(
                    "This conversation lives in its original thread; reply there so everyone sees one discussion");

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
            // One save: the reply and its author's notification land together or not at all.
            await _notifications.StageReplyAsync(postId, dto.AuthorId, dto.Content, ct);
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
        /// <remarks>A retracted feed card fails <see cref="PostVisibilityPolicy.VisibleTo"/>, so detail, like,
        /// reply, share and bookmark all answer it with the same not-found as a missing post.</remarks>
        private Task<Post?> LoadVisibleAsync(string postId, string? viewerId, CancellationToken ct)
            => _posts.Query()
                .Where(p => p.id == postId)
                .ReadableBy(viewerId)
                .FirstOrDefaultAsync(ct);

        /// <summary>The project half of the read gate; shared with the feed through <see cref="PostReadFilters"/>.</summary>
        private static Expression<Func<Post, bool>> OnAVisibleProject(string? viewerId)
            => PostReadFilters.OnAVisibleProject(viewerId);

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

        /// <summary>A unique index refused the row: a second like or bookmark from one person, or a second ask on one task.</summary>
        private static bool IsUniqueViolation(DbUpdateException failure)
            => failure.InnerException is DbException { SqlState: UniqueViolationSqlState };

        private IReadOnlyList<PostDto> MapPosts(IEnumerable<Post> posts)
            => posts.Select(p => _mapper.Map<PostDto>(p)).ToList();

        private static (int Page, int PageSize) ClampPaging(int page, int pageSize)
            => (Math.Max(1, page), Math.Clamp(pageSize, 1, MaximumPageSize));
    }
}
