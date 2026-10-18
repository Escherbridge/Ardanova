namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Domain.Models.Enums;

/// <summary>
/// Who may seat, re-role, or remove a project member, and who may delete the project.
/// Rank-ordered roles; see Services/Implementations/AGENTS.md §Project member authority.
/// </summary>
public static class ProjectMemberRolePolicy
{
    public const string FounderSeatOnly =
        "Members join by invitation or an accepted application; only the project's creator can seat themselves as founder.";
    public const string FounderIsNotGrantable = "The founder role cannot be given to anyone.";
    public const string CannotChangeOwnRole = "You cannot change your own role.";
    public const string FounderSeatIsFixed = "The founder's membership cannot be changed or removed.";
    public const string OutranksActor = "You can only manage members, and give roles, below your own.";
    public const string FounderOnlyDelete = "Only the project's founder can delete the project.";

    /// <summary>FOUNDER outranks LEADER outranks CORE_CONTRIBUTOR outranks CONTRIBUTOR outranks OBSERVER.</summary>
    public static int Rank(ProjectRole role) => role switch
    {
        ProjectRole.FOUNDER => 4,
        ProjectRole.LEADER => 3,
        ProjectRole.CORE_CONTRIBUTOR => 2,
        ProjectRole.CONTRIBUTOR => 1,
        _ => 0,
    };

    /// <summary>The actor's effective role: the creator always ranks as founder, otherwise their membership row.</summary>
    public static ProjectRole? EffectiveRole(string actorId, string projectCreatorId, ProjectRole? membershipRole)
        => string.Equals(actorId, projectCreatorId, StringComparison.Ordinal) ? ProjectRole.FOUNDER : membershipRole;

    /// <summary>Direct seating is the creator's own founder seat; everyone else consents via invitation or application.</summary>
    public static string? RefuseAdd(string actorId, bool actorIsAdmin, string projectCreatorId, string userId, ProjectRole role)
    {
        if (actorIsAdmin)
            return role == ProjectRole.FOUNDER && !string.Equals(userId, projectCreatorId, StringComparison.Ordinal)
                ? FounderIsNotGrantable
                : null;

        var seatsOwnFounderRow = string.Equals(actorId, userId, StringComparison.Ordinal)
            && string.Equals(actorId, projectCreatorId, StringComparison.Ordinal)
            && role == ProjectRole.FOUNDER;
        return seatsOwnFounderRow ? null : FounderSeatOnly;
    }

    /// <summary>A role change: never to FOUNDER, never one's own, never the founder's row, and only below the actor's rank.</summary>
    public static string? RefuseRoleChange(
        string actorId,
        bool actorIsAdmin,
        ProjectRole? actorRole,
        string targetUserId,
        ProjectRole targetRole,
        ProjectRole requestedRole)
    {
        if (requestedRole == ProjectRole.FOUNDER)
            return FounderIsNotGrantable;
        if (targetRole == ProjectRole.FOUNDER && !actorIsAdmin)
            return FounderSeatIsFixed;
        if (actorIsAdmin)
            return null;
        if (string.Equals(actorId, targetUserId, StringComparison.Ordinal))
            return CannotChangeOwnRole;

        var actorRank = actorRole is { } role ? Rank(role) : -1;
        return Rank(targetRole) < actorRank && Rank(requestedRole) < actorRank ? null : OutranksActor;
    }

    /// <summary>Removal: anyone but the founder may leave; otherwise only a higher-ranked actor removes a member.</summary>
    public static string? RefuseRemoval(string actorId, bool actorIsAdmin, ProjectRole? actorRole, string targetUserId, ProjectRole targetRole)
    {
        if (actorIsAdmin)
            return null;
        if (targetRole == ProjectRole.FOUNDER)
            return FounderSeatIsFixed;
        if (string.Equals(actorId, targetUserId, StringComparison.Ordinal))
            return null;

        var actorRank = actorRole is { } role ? Rank(role) : -1;
        return Rank(targetRole) < actorRank ? null : OutranksActor;
    }

    /// <summary>Deleting the project is the founder's (or an admin's) call, not every manager's.</summary>
    public static string? RefuseProjectDelete(bool actorIsAdmin, ProjectRole? actorRole)
        => actorIsAdmin || actorRole == ProjectRole.FOUNDER ? null : FounderOnlyDelete;
}
