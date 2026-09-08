using System;
using System.Linq;
using System.Linq.Expressions;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>The single rule for who may read a post, written as an EF-translatable predicate.</summary>
/// <remarks>
/// Every read seam composes <see cref="VisibleTo"/> into its own query, so the list, single-row and
/// pre-write checks cannot drift apart. Project membership here mirrors
/// <c>HierarchyAuthorizationService.IsProjectMemberAsync</c> (project creator, or any ProjectMember row);
/// change the two together.
/// </remarks>
public static class PostVisibilityPolicy
{
    /// <summary>The subset a caller with no viewer identity may read.</summary>
    private static readonly Expression<Func<Post, bool>> PublicOnly =
        post => post.visibility == PostVisibility.PUBLIC;

    private static readonly Func<Post, bool> PublicOnlyCheck = PublicOnly.Compile();

    /// <summary>Posts <paramref name="viewerId"/> may read; a null or empty viewer gets the PUBLIC subset only.</summary>
    public static Expression<Func<Post, bool>> VisibleTo(string? viewerId)
    {
        if (string.IsNullOrEmpty(viewerId))
            return PublicOnly;

        var viewer = viewerId;
        return post =>
            post.visibility == PostVisibility.PUBLIC
            || post.authorId == viewer
            || (post.visibility == PostVisibility.PROJECT_MEMBERS
                && post.Project != null
                && (post.Project.createdById == viewer
                    || post.Project.ProjectMembers.Any(member => member.userId == viewer)))
            || (post.visibility == PostVisibility.GUILD_MEMBERS
                && post.Guild != null
                && (post.Guild.ownerId == viewer
                    || post.Guild.GuildMembers.Any(member => member.userId == viewer)));
    }

    /// <summary>In-memory form of the anonymous subset, compiled from the same expression.</summary>
    public static bool IsPubliclyVisible(Post post) => PublicOnlyCheck(post);

    /// <summary>Whether a post declares an audience its project or guild scope can actually express.</summary>
    public static bool HasCoherentAudience(PostVisibility visibility, string? projectId, string? guildId)
        => visibility switch
        {
            PostVisibility.PROJECT_MEMBERS => !string.IsNullOrWhiteSpace(projectId),
            PostVisibility.GUILD_MEMBERS => !string.IsNullOrWhiteSpace(guildId),
            _ => true
        };
}
