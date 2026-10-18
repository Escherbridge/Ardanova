namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

/// <summary>
/// A founder must be able to draft an unfinished ask in private, or nobody drafts honestly. These tests
/// drive the real predicates through the repository seam, so they fail if a read stops composing them.
/// </summary>
public class DraftProjectVisibilityBoundaryTests
{
    private const string Founder = "founder-1";
    private const string Teammate = "teammate-1";
    private const string Stranger = "stranger-1";
    private const string DraftProjectId = "project-draft";
    private const string DraftProjectSlug = "solar-barn-draft";
    private const string PublishedProjectId = "project-published";
    private const string PublishedProjectSlug = "solar-barn";

    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<Post>> _posts = new();
    private readonly Mock<IRepository<PostLike>> _likes = new();
    private readonly Mock<IRepository<PostComment>> _postComments = new();
    private readonly Mock<IRepository<PostBookmark>> _bookmarks = new();
    private readonly Mock<IRepository<PostShare>> _shares = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();

    private readonly ProjectService _projectService;
    private readonly PostService _postService;

    public DraftProjectVisibilityBoundaryTests()
    {
        var catalogue = Catalogue();

        _projects
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Project, bool>> predicate, CancellationToken _) =>
                catalogue.AsQueryable().FirstOrDefault(predicate));

        _projects
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Project, bool>> predicate, CancellationToken _) =>
                catalogue.AsQueryable().Where(predicate).ToList());

        _projects
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int pageSize, Expression<Func<Project, bool>>? predicate, CancellationToken _) =>
            {
                var filter = predicate ?? (Expression<Func<Project, bool>>)(project => true);
                var matches = catalogue.AsQueryable().Where(filter).ToList();
                return new PagedResult<Project>(matches, matches.Count, page, pageSize);
            });

        _posts
            .Setup(repository => repository.Query())
            .Returns(() => new TestAsyncEnumerable<Post>(Notes()));

        _postComments
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<PostComment, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PostComment, bool>> predicate, CancellationToken _) =>
                Replies().AsQueryable().Where(predicate).ToList());

        _mapper
            .Setup(mapper => mapper.Map<ProjectDto>(It.IsAny<object>()))
            .Returns((object source) => ToProjectDto((Project)source));
        _mapper
            .Setup(mapper => mapper.Map<List<ProjectDto>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<Project>)source).Select(ToProjectDto).ToList());
        _mapper
            .Setup(mapper => mapper.Map<PostDto>(It.IsAny<object>()))
            .Returns((object source) => ToPostDto((Post)source));
        _mapper
            .Setup(mapper => mapper.Map<PostCommentDto>(It.IsAny<object>()))
            .Returns((object source) => new PostCommentDto
            {
                Id = ((PostComment)source).id,
                PostId = ((PostComment)source).postId,
                AuthorId = ((PostComment)source).authorId,
                Content = ((PostComment)source).content
            });

        _projectService = new ProjectService(
            _projects.Object,
            _users.Object,
            new Mock<IRepository<ProjectTask>>().Object,
            new Mock<IRepository<ProjectMilestone>>().Object,
            _unitOfWork.Object,
            _mapper.Object);

        _postService = new PostService(
            _posts.Object,
            _likes.Object,
            _postComments.Object,
            _bookmarks.Object,
            _shares.Object,
            _unitOfWork.Object,
            _mapper.Object);
    }

    // ===== OI-13: the single-row project lookups the MCP seam calls =====

    [Fact]
    public async Task AnonymousSearch_LeavesDraftsOut()
    {
        var result = await _projectService.SearchVisibleAsync(
            ProjectViewer.Anonymous, "solar", null, null, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(project => project.Id)
            .Should().BeEquivalentTo(new[] { PublishedProjectId });
    }

    [Fact]
    public async Task Search_KeepsTheDraftForTheFounderWhoIsWritingIt()
    {
        var result = await _projectService.SearchVisibleAsync(
            new ProjectViewer(Founder, IsAdmin: false), "solar", null, null, null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(project => project.Id)
            .Should().BeEquivalentTo(new[] { DraftProjectId, PublishedProjectId });
    }

    [Fact]
    public async Task GetVisibleByIdAsync_ForAStranger_IsNotFoundRatherThanForbidden()
    {
        var result = await _projectService.GetVisibleByIdAsync(
            new ProjectViewer(Stranger, IsAdmin: false), DraftProjectId);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetVisibleByIdAsync_ForAnAnonymousCaller_IsNotFound()
    {
        var result = await _projectService.GetVisibleByIdAsync(ProjectViewer.Anonymous, DraftProjectId);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetVisibleBySlugAsync_ForAStranger_IsNotFoundRatherThanForbidden()
    {
        var result = await _projectService.GetVisibleBySlugAsync(
            new ProjectViewer(Stranger, IsAdmin: false), DraftProjectSlug);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetVisibleBySlugAsync_ForAProjectMember_ResolvesTheDraft()
    {
        var result = await _projectService.GetVisibleBySlugAsync(
            new ProjectViewer(Teammate, IsAdmin: false), DraftProjectSlug);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(DraftProjectId);
    }

    [Fact]
    public async Task GetVisibleBySlugAsync_ForAnyone_StillResolvesAPublishedProject()
    {
        var result = await _projectService.GetVisibleBySlugAsync(
            ProjectViewer.Anonymous, PublishedProjectSlug);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(PublishedProjectId);
    }

    // ===== OI-14: field notes on a draft =====

    [Fact]
    public async Task GetProjectFieldNotesAsync_ForAStranger_ReturnsNothingFromADraft()
    {
        var result = await _postService.GetProjectFieldNotesAsync(DraftProjectId, Stranger, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectFieldNotesAsync_ForAnAnonymousReader_ReturnsNothingFromADraft()
    {
        var result = await _postService.GetProjectFieldNotesAsync(DraftProjectId, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProjectFieldNotesAsync_ForAProjectMember_ReturnsTheDraftNotes()
    {
        var result = await _postService.GetProjectFieldNotesAsync(DraftProjectId, Teammate, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(note => note.Id).Should().BeEquivalentTo(new[] { "note-on-draft" });
    }

    [Fact]
    public async Task GetProjectFieldNotesAsync_ForAnyone_StillReturnsAPublishedProjectsNotes()
    {
        var result = await _postService.GetProjectFieldNotesAsync(PublishedProjectId, null, 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(note => note.Id).Should().BeEquivalentTo(new[] { "note-on-published" });
    }

    [Fact]
    public async Task GetForViewerAsync_ForAStranger_IsNotFoundOnADraftsNote()
    {
        var result = await _postService.GetForViewerAsync("note-on-draft", Stranger);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetForViewerAsync_ForAProjectMember_ResolvesADraftsNote()
    {
        var result = await _postService.GetForViewerAsync("note-on-draft", Teammate);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("note-on-draft");
    }

    [Fact]
    public async Task GetByAuthorForViewerAsync_ForAStranger_LeavesDraftNotesOut()
    {
        var result = await _postService.GetByAuthorForViewerAsync(Founder, Stranger);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(note => note.Id).Should().BeEquivalentTo(new[] { "note-on-published" });
    }

    [Fact]
    public async Task GetCommentsForViewerAsync_ForAStranger_IsNotFoundOnADraftsNote()
    {
        var result = await _postService.GetCommentsForViewerAsync("note-on-draft", Stranger);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetCommentsForViewerAsync_ForAProjectMember_ReturnsTheReplies()
    {
        var result = await _postService.GetCommentsForViewerAsync("note-on-draft", Teammate);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(reply => reply.Id).Should().BeEquivalentTo(new[] { "reply-on-draft" });
    }

    /// <summary>The viewer-less legacy reads cannot see a post's project, so they refuse project posts.</summary>
    [Fact]
    public async Task GetByIdAsync_RefusesAProjectScopedPost()
    {
        var note = Notes().Single(post => post.id == "note-on-published");
        _posts
            .Setup(repository => repository.GetByIdAsync("note-on-published", It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var result = await _postService.GetByIdAsync("note-on-published");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    private static ProjectDto ToProjectDto(Project project) => new()
    {
        Id = project.id,
        Title = project.title,
        Slug = project.slug,
        Description = project.description
    };

    private static PostDto ToPostDto(Post post) => new()
    {
        Id = post.id,
        AuthorId = post.authorId,
        ProjectId = post.projectId,
        Type = post.type.ToString(),
        Visibility = post.visibility.ToString(),
        Content = post.content
    };

    private static List<Project> Catalogue()
    {
        var draft = NewProject(DraftProjectId, DraftProjectSlug, "Solar barn (draft)", ProjectStatus.DRAFT);
        draft.ProjectMembers.Add(new ProjectMember
        {
            id = "member-1",
            projectId = DraftProjectId,
            userId = Teammate,
            role = ProjectRole.CONTRIBUTOR,
            joinedAt = DateTime.UtcNow
        });

        var published = NewProject(
            PublishedProjectId, PublishedProjectSlug, "Solar barn", ProjectStatus.PUBLISHED);

        return [draft, published];
    }

    private static Project NewProject(string id, string slug, string title, ProjectStatus status) => new()
    {
        id = id,
        slug = slug,
        title = title,
        description = "A solar barn for the co-op",
        problemStatement = "The co-op buys grid power it cannot afford",
        solution = "Roof the barn in panels",
        summary = "Solar for the co-op barn",
        categories = "TECHNOLOGY",
        status = status,
        createdById = Founder,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    /// <summary>Both notes are PUBLIC: post visibility alone would serve the draft's note to anyone.</summary>
    private static List<Post> Notes()
    {
        var catalogue = Catalogue();
        var draft = catalogue.Single(project => project.id == DraftProjectId);
        var published = catalogue.Single(project => project.id == PublishedProjectId);

        return
        [
            NewNote("note-on-draft", draft),
            NewNote("note-on-published", published)
        ];
    }

    private static Post NewNote(string id, Project project) => new()
    {
        id = id,
        authorId = Founder,
        projectId = project.id,
        type = PostType.POST,
        visibility = PostVisibility.PUBLIC,
        content = "Poured the footings today",
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow,
        Project = project
    };

    private static List<PostComment> Replies() =>
    [
        new()
        {
            id = "reply-on-draft",
            postId = "note-on-draft",
            authorId = Teammate,
            content = "Nice work",
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        }
    ];
}

/// <summary>Enough of an EF Core async provider to run the production predicates over in-memory rows.</summary>
internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
        => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executed = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })!
            .MakeGenericMethod(resultType)
            .Invoke(_inner, new object[] { expression });

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executed })!;
    }
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
