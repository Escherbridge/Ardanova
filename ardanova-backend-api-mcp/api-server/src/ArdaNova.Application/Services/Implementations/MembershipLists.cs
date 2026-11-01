namespace ArdaNova.Application.Services.Implementations;

/// <summary>The bound on GET /api/projects/mine and GET /api/guilds/mine; see AGENTS.md §Memberships.</summary>
public static class MembershipLists
{
    /// <summary>At most this many rows, the most recently active first chosen, then sorted by name for the picker.</summary>
    public const int Cap = 200;
}
