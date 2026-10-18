using System.Security.Claims;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArdaNova.API.Filters;

/// <summary>A user as anyone may see them: no contact, authority or trust fields. See Filters/AGENTS.md §User directory privacy.</summary>
public sealed record PublicUserView
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string? Image { get; init; }
    public string? Bio { get; init; }
    public string? Location { get; init; }
    public string? Website { get; init; }
    public string? LinkedIn { get; init; }
    public string? Twitter { get; init; }
    public UserType UserType { get; init; }
    public bool IsVerified { get; init; }
    public int TotalXP { get; init; }
    public int Level { get; init; }
    public UserTier Tier { get; init; }
    public VerificationLevel VerificationLevel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static PublicUserView From(UserDto user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Image = user.Image,
        Bio = user.Bio,
        Location = user.Location,
        Website = user.Website,
        LinkedIn = user.LinkedIn,
        Twitter = user.Twitter,
        UserType = user.UserType,
        IsVerified = user.IsVerified,
        TotalXP = user.TotalXP,
        Level = user.Level,
        Tier = user.Tier,
        VerificationLevel = user.VerificationLevel,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}

/// <summary>Projects every top-level UserDto response to <see cref="PublicUserView"/> unless the viewer is that user or an administrator.</summary>
public sealed class UserDirectoryPrivacyFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: { } value } result)
            return;

        var viewer = context.HttpContext.User;
        if (ViewerAuthority.IsAdministrator(viewer))
            return;

        var actorId = ViewerAuthority.ActorId(viewer);
        result.Value = value switch
        {
            UserDto user => Project(user, actorId),
            PagedResult<UserDto> page => page.Map(user => Project(user, actorId)),
            IEnumerable<UserDto> users => users.Select(user => Project(user, actorId)).ToList(),
            AttachmentWithUserDto attachment => RedactUploader(attachment, actorId),
            IEnumerable<AttachmentWithUserDto> attachments => attachments.Select(a => RedactUploader(a, actorId)).ToList(),
            ProjectMemberDto member => RedactMember(member, actorId),
            IEnumerable<ProjectMemberDto> members => members.Select(m => RedactMember(m, actorId)).ToList(),
            _ => value,
        };
        if (!ReferenceEquals(result.Value, value))
            result.DeclaredType = null;
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static object Project(UserDto user, string? actorId)
        => actorId is not null && string.Equals(user.Id, actorId, StringComparison.Ordinal)
            ? user
            : PublicUserView.From(user);

    /// <summary>A project member's email is for that member alone; the roster keeps name and image.</summary>
    private static ProjectMemberDto RedactMember(ProjectMemberDto member, string? actorId)
        => member.User is not { Email: not null } user
            || (actorId is not null && string.Equals(user.Id, actorId, StringComparison.Ordinal))
            ? member
            : member with { User = user with { Email = null } };

    /// <summary>Nested uploader keeps its UserDto type, so the private fields are blanked rather than projected away.</summary>
    private static AttachmentWithUserDto RedactUploader(AttachmentWithUserDto attachment, string? actorId)
        => attachment.UploadedBy is not { } uploader
            || (actorId is not null && string.Equals(uploader.Id, actorId, StringComparison.Ordinal))
            ? attachment
            : attachment with
            {
                UploadedBy = uploader with
                {
                    Email = null!,
                    EmailVerified = null,
                    Phone = null,
                    TrustScore = 0,
                },
            };
}
