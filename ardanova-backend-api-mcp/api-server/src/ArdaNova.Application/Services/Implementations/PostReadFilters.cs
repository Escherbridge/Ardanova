namespace ArdaNova.Application.Services.Implementations;

using System.Linq.Expressions;
using ArdaNova.Domain.Models.Entities;

/// <summary>The post read gate shared by the field-notes seam and the feed, so the two cannot drift apart.</summary>
public static class PostReadFilters
{
    /// <summary>Both rules every viewer-facing post read composes: the post's own audience, then its project's.</summary>
    public static IQueryable<Post> ReadableBy(this IQueryable<Post> posts, string? viewerId)
        => posts
            .Where(PostVisibilityPolicy.VisibleTo(viewerId))
            .Where(OnAVisibleProject(viewerId));

    /// <summary>Lifts the project visibility rule onto a post: a draft's notes are as private as the draft.</summary>
    /// <remarks>
    /// Composed from <see cref="ProjectQueryFilters.VisibleTo"/> - the one definition of what a draft is -
    /// rather than restated here, so the note stream, the feed and the project page cannot drift apart. A post
    /// with no project passes; every other post inherits its project's answer. Admin reach is deliberately not
    /// modelled: a post read resolves an admin as a plain viewer, which fails closed.
    /// </remarks>
    public static Expression<Func<Post, bool>> OnAVisibleProject(string? viewerId)
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

        public static Expression Rebind(Expression body, ParameterExpression replaced, Expression replacement)
            => new ProjectParameterRebinder(replaced, replacement).Visit(body)!;

        protected override Expression VisitParameter(ParameterExpression node)
            => ReferenceEquals(node, _replaced) ? _replacement : base.VisitParameter(node);
    }
}
