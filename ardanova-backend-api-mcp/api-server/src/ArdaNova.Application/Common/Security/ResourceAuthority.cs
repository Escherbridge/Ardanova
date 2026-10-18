namespace ArdaNova.Application.Common.Security;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

/// <summary>Outcome of a per-record authorization lookup.</summary>
public enum ResourceAccess
{
    NotFound,
    Denied,
    Allowed,
}

/// <summary>Who a viewer is in relation to a set of events: the facts the event visibility policy reads.</summary>
public sealed record EventViewerRelations(
    IReadOnlySet<string> HostedEventIds,
    IReadOnlySet<string> AttendingEventIds,
    IReadOnlySet<string> InvitedEventIds,
    IReadOnlySet<string> ProjectIds,
    IReadOnlySet<string> GuildIds)
{
    public static readonly EventViewerRelations None = new(
        new HashSet<string>(), new HashSet<string>(), new HashSet<string>(),
        new HashSet<string>(), new HashSet<string>());
}

/// <summary>The minimum an event must expose for the visibility policy to judge it.</summary>
public readonly record struct EventVisibilityFacts(
    string Id,
    EventVisibility Visibility,
    string OrganizerId,
    string? ProjectId,
    string? GuildId);

/// <summary>Event read rules: who may see an event and who may see its meeting link. See Application/Common/AGENTS.md §Resource authority.</summary>
public static class EventVisibilityPolicy
{
    public static bool CanSee(EventVisibilityFacts facts, string? viewerId, EventViewerRelations relations)
    {
        if (facts.Visibility == EventVisibility.PUBLIC || IsHost(facts, viewerId, relations))
            return true;
        if (viewerId is null)
            return false;
        if (relations.AttendingEventIds.Contains(facts.Id) || relations.InvitedEventIds.Contains(facts.Id))
            return true;

        return facts.Visibility switch
        {
            EventVisibility.PROJECT_MEMBERS => facts.ProjectId is not null && relations.ProjectIds.Contains(facts.ProjectId),
            EventVisibility.GUILD_MEMBERS => facts.GuildId is not null && relations.GuildIds.Contains(facts.GuildId),
            _ => false,
        };
    }

    /// <summary>The join link is for the hosts and the people who said they are coming.</summary>
    public static bool CanSeeMeetingLink(EventVisibilityFacts facts, string? viewerId, EventViewerRelations relations)
        => IsHost(facts, viewerId, relations)
            || (viewerId is not null && relations.AttendingEventIds.Contains(facts.Id));

    private static bool IsHost(EventVisibilityFacts facts, string? viewerId, EventViewerRelations relations)
        => viewerId is not null
            && (string.Equals(facts.OrganizerId, viewerId, StringComparison.Ordinal)
                || relations.HostedEventIds.Contains(facts.Id));
}

/// <summary>Record-level authorization the API pipeline asks before a feature service runs.</summary>
public interface IResourceAuthority
{
    Task<ResourceAccess> CanManageEventAsync(string actorId, string eventId, CancellationToken ct = default);
    Task<ResourceAccess> CanViewEventAsync(string? viewerId, string eventId, CancellationToken ct = default);
    Task<ResourceAccess> CanManageProductAsync(string actorId, string productId, CancellationToken ct = default);
    Task<ResourceAccess> CanManageProjectCatalogAsync(string actorId, string projectId, CancellationToken ct = default);
    Task<ResourceAccess> CanReadReferralAsync(string actorId, string referralId, CancellationToken ct = default);
    Task<EventViewerRelations> GetEventViewerRelationsAsync(string viewerId, IReadOnlyCollection<string> eventIds, CancellationToken ct = default);
}

/// <summary>Repository-backed <see cref="IResourceAuthority"/>; every lookup is a projected, id-keyed query.</summary>
public sealed class ResourceAuthority(
    IRepository<Event> events,
    IRepository<EventAttendee> attendees,
    IRepository<EventCoHost> coHosts,
    IRepository<Product> products,
    IRepository<Project> projects,
    IRepository<ProjectMember> projectMembers,
    IRepository<Guild> guilds,
    IRepository<GuildMember> guildMembers,
    IRepository<Referral> referrals) : IResourceAuthority
{
    /// <summary>Same manager set as HierarchyAuthorizationService.CanManageProjectAsync.</summary>
    private static readonly ProjectRole[] ManagerRoles =
        [ProjectRole.FOUNDER, ProjectRole.LEADER, ProjectRole.CORE_CONTRIBUTOR];

    private static readonly AttendeeStatus[] AttendingStatuses =
        [AttendeeStatus.GOING, AttendeeStatus.MAYBE, AttendeeStatus.ATTENDED];

    public async Task<ResourceAccess> CanManageEventAsync(string actorId, string eventId, CancellationToken ct = default)
    {
        var organizers = await events.FindProjectedAsync(e => e.id == eventId, e => e.organizerId, ct);
        if (organizers.Count == 0)
            return ResourceAccess.NotFound;
        return organizers[0] == actorId ? ResourceAccess.Allowed : ResourceAccess.Denied;
    }

    public async Task<ResourceAccess> CanViewEventAsync(string? viewerId, string eventId, CancellationToken ct = default)
    {
        var found = await events.FindProjectedAsync(
            e => e.id == eventId,
            e => new { e.id, e.visibility, e.organizerId, e.projectId, e.guildId },
            ct);
        if (found.Count == 0)
            return ResourceAccess.NotFound;

        var row = found[0];
        var facts = new EventVisibilityFacts(row.id, row.visibility, row.organizerId, row.projectId, row.guildId);
        var relations = viewerId is null || facts.Visibility == EventVisibility.PUBLIC
            ? EventViewerRelations.None
            : await GetEventViewerRelationsAsync(viewerId, [eventId], ct);

        // An event the viewer may not see does not exist for them.
        return EventVisibilityPolicy.CanSee(facts, viewerId, relations) ? ResourceAccess.Allowed : ResourceAccess.NotFound;
    }

    public async Task<ResourceAccess> CanManageProductAsync(string actorId, string productId, CancellationToken ct = default)
    {
        var projectIds = await products.FindProjectedAsync(p => p.id == productId, p => p.projectId, ct);
        if (projectIds.Count == 0)
            return ResourceAccess.NotFound;
        return await IsProjectManagerAsync(actorId, projectIds[0], ct) ? ResourceAccess.Allowed : ResourceAccess.Denied;
    }

    public async Task<ResourceAccess> CanManageProjectCatalogAsync(string actorId, string projectId, CancellationToken ct = default)
    {
        if (!await projects.ExistsAsync(p => p.id == projectId, ct))
            return ResourceAccess.NotFound;
        return await IsProjectManagerAsync(actorId, projectId, ct) ? ResourceAccess.Allowed : ResourceAccess.Denied;
    }

    public async Task<ResourceAccess> CanReadReferralAsync(string actorId, string referralId, CancellationToken ct = default)
    {
        var parties = await referrals.FindProjectedAsync(
            r => r.id == referralId,
            r => new { r.referrerId, r.referredId },
            ct);
        if (parties.Count == 0)
            return ResourceAccess.NotFound;
        return parties[0].referrerId == actorId || parties[0].referredId == actorId
            ? ResourceAccess.Allowed
            : ResourceAccess.Denied;
    }

    public async Task<EventViewerRelations> GetEventViewerRelationsAsync(
        string viewerId,
        IReadOnlyCollection<string> eventIds,
        CancellationToken ct = default)
    {
        var ids = eventIds.Distinct().ToList();
        var hosted = await coHosts.FindProjectedAsync(h => h.userId == viewerId && ids.Contains(h.eventId), h => h.eventId, ct);
        var rsvps = await attendees.FindProjectedAsync(
            a => a.userId == viewerId && ids.Contains(a.eventId),
            a => new { a.eventId, a.status },
            ct);
        var memberOf = await projectMembers.FindProjectedAsync(m => m.userId == viewerId, m => m.projectId, ct);
        var created = await projects.FindProjectedAsync(p => p.createdById == viewerId, p => p.id, ct);
        var guildMemberOf = await guildMembers.FindProjectedAsync(m => m.userId == viewerId, m => m.guildId, ct);
        var owned = await guilds.FindProjectedAsync(g => g.ownerId == viewerId, g => g.id, ct);

        return new EventViewerRelations(
            hosted.ToHashSet(StringComparer.Ordinal),
            rsvps.Where(r => AttendingStatuses.Contains(r.status)).Select(r => r.eventId).ToHashSet(StringComparer.Ordinal),
            rsvps.Where(r => r.status == AttendeeStatus.INVITED).Select(r => r.eventId).ToHashSet(StringComparer.Ordinal),
            memberOf.Concat(created).ToHashSet(StringComparer.Ordinal),
            guildMemberOf.Concat(owned).ToHashSet(StringComparer.Ordinal));
    }

    private async Task<bool> IsProjectManagerAsync(string actorId, string projectId, CancellationToken ct)
        => await projects.ExistsAsync(p => p.id == projectId && p.createdById == actorId, ct)
            || await projectMembers.ExistsAsync(
                m => m.projectId == projectId && m.userId == actorId && ManagerRoles.Contains(m.role),
                ct);
}
