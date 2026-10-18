using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ArdaNova.API.Filters;

/// <summary>Fail-closed authorization defaults applied to the controller model at startup. See Filters/AGENTS.md §Endpoint authorization.</summary>
public sealed class EndpointAuthorizationConvention : IApplicationModelConvention
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly string[] Administrator = [AuthorizationPolicies.ActorAdministrator];

    /// <summary>Full-PII user dumps need the admin key and a signed administrator, so a leaked key alone is not enough.</summary>
    private static readonly string[] AdminKeyAndAdministrator =
        [AuthorizationPolicies.AdminApiKey, AuthorizationPolicies.ActorAdministrator];

    /// <summary>Policies added to specific actions, on top of whatever the controller declares; every listed policy must pass.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> PolicyOverrides =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Users.GetAll"] = AdminKeyAndAdministrator,
            ["Users.GetByEmail"] = AdminKeyAndAdministrator,

            ["Achievements.Create"] = Administrator,
            ["Achievements.Update"] = Administrator,
            ["Achievements.Delete"] = Administrator,
            ["Achievements.UpdateProgress"] = Administrator,
            ["Achievements.AwardAchievement"] = Administrator,

            ["Leaderboards.Create"] = Administrator,
            ["Leaderboards.Delete"] = Administrator,
            ["Leaderboards.AddEntry"] = Administrator,
            ["Leaderboards.UpdateEntry"] = Administrator,
            ["Leaderboards.DeleteEntry"] = Administrator,

            ["XPEvents.AwardXP"] = Administrator,

            ["Referrals.Complete"] = Administrator,
            ["Referrals.Expire"] = Administrator,
            ["Referrals.Cancel"] = Administrator,

            ["Activities.Delete"] = Administrator,

            ["UserStreaks.Create"] = Administrator,
            ["UserStreaks.Delete"] = Administrator,
        };

    /// <summary>Mutating verbs that are reads in disguise and stay callable with the service key alone.</summary>
    public static readonly IReadOnlySet<string> ServiceKeyMutations =
        new HashSet<string>(StringComparer.Ordinal) { "ProjectTokens.GetMetadata" };

    /// <summary>Actions whose request names a user that must be the verified actor (admins excepted).</summary>
    public static readonly IReadOnlyDictionary<string, ActorSubjectBinding> SubjectBindings =
        new Dictionary<string, ActorSubjectBinding>(StringComparer.Ordinal)
        {
            ["Events.Create"] = new("dto", "OrganizerId"),
            ["Events.Register"] = new("dto", "UserId"),
            ["Events.Unregister"] = new("userId"),
            ["Activities.Create"] = new("dto", "UserId"),
            ["Referrals.Create"] = new("dto", "ReferrerId"),
            ["Referrals.GetByReferrerId"] = new("referrerId"),
            ["Referrals.GetByReferredId"] = new("referredId"),
            ["UserStreaks.RecordActivity"] = new("userId"),
            ["UserStreaks.ResetStreak"] = new("userId"),
            ["Products.Create"] = new("dto", "UserId"),
        };

    /// <summary>Actions that touch existing records and must pass each record's rule, in order (admins excepted).</summary>
    public static readonly IReadOnlyDictionary<string, ResourceGuard[]> ResourceGuards =
        new Dictionary<string, ResourceGuard[]>(StringComparer.Ordinal)
        {
            ["Events.Create"] =
            [
                new(ResourceRule.ProjectAffiliate, new("dto", "ProjectId"), Optional: true),
                new(ResourceRule.GuildAffiliate, new("dto", "GuildId"), Optional: true),
            ],
            ["Events.Update"] = [new(ResourceRule.EventOrganizer, new("id"))],
            ["Events.Delete"] = [new(ResourceRule.EventOrganizer, new("id"))],
            ["Events.GetById"] = [new(ResourceRule.EventViewer, new("id"))],
            ["Events.GetAttendees"] = [new(ResourceRule.EventViewer, new("id"))],
            ["Events.Register"] = [new(ResourceRule.EventViewer, new("id"))],
            ["Products.Create"] = [new(ResourceRule.ProjectCatalogManager, new("dto", "ProjectId"))],
            ["Products.Update"] = [new(ResourceRule.ProductManager, new("id"))],
            ["Products.Delete"] = [new(ResourceRule.ProductManager, new("id"))],
            ["Products.ToggleActive"] = [new(ResourceRule.ProductManager, new("id"))],
            ["Referrals.GetById"] = [new(ResourceRule.ReferralParticipant, new("id"))],
        };

    /// <summary>Override and binding keys that matched an action; the rest name an action that is not loaded.</summary>
    public HashSet<string> MatchedKeys { get; } = new(StringComparer.Ordinal);

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                var key = $"{controller.ControllerName}.{action.ActionName}";

                if (PolicyOverrides.TryGetValue(key, out var overridePolicies))
                {
                    foreach (var policy in overridePolicies)
                        Require(action, policy);
                    MatchedKeys.Add(key);
                }
                else if (IsMutation(action)
                    && !HasAuthorizationMetadata(controller, action)
                    && !ServiceKeyMutations.Contains(key))
                {
                    Require(action, AuthorizationPolicies.ActorAssertion);
                }

                if (SubjectBindings.TryGetValue(key, out var binding))
                {
                    binding.EnsureBindsTo(action, key);
                    action.Filters.Add(new ActorSubjectBindingFilter(binding));
                    MatchedKeys.Add(key);
                }

                if (ResourceGuards.TryGetValue(key, out var guards))
                {
                    foreach (var guard in guards)
                    {
                        guard.Record.EnsureBindsTo(action, key);
                        action.Filters.Add(new ResourceAuthorizationFilter(guard));
                    }
                    MatchedKeys.Add(key);
                }
            }
        }
    }

    internal static bool IsMutation(ActionModel action)
        => action.Attributes
            .OfType<IActionHttpMethodProvider>()
            .SelectMany(provider => provider.HttpMethods)
            .Any(MutatingMethods.Contains);

    private static bool HasAuthorizationMetadata(ControllerModel controller, ActionModel action)
        => controller.Attributes.Concat(action.Attributes)
            .Any(attribute => attribute is IAuthorizeData or IAllowAnonymous);

    private static void Require(ActionModel action, string policy)
    {
        foreach (var selector in action.Selectors)
            selector.EndpointMetadata.Add(new AuthorizeAttribute { Policy = policy });
    }
}

/// <summary>Where an action reads the user it acts for: a parameter, or a property of a body parameter.</summary>
public sealed record ActorSubjectBinding(string ParameterName, string? PropertyName = null)
{
    /// <summary>Throws at startup when a rename would silently leave the binding pointing at nothing.</summary>
    internal void EnsureBindsTo(ActionModel action, string key)
    {
        var parameter = action.Parameters.FirstOrDefault(p => p.ParameterName == ParameterName)
            ?? throw new InvalidOperationException($"Subject binding {key} names missing parameter '{ParameterName}'.");

        var subjectType = PropertyName is null
            ? parameter.ParameterType
            : parameter.ParameterType.GetProperty(PropertyName)?.PropertyType
                ?? throw new InvalidOperationException($"Subject binding {key} names missing property '{PropertyName}'.");

        if (subjectType != typeof(string))
            throw new InvalidOperationException($"Subject binding {key} must read a string user id.");
    }

    internal string? ReadSubject(IDictionary<string, object?> arguments)
    {
        if (!arguments.TryGetValue(ParameterName, out var value) || value is null)
            return null;
        return PropertyName is null
            ? value as string
            : value.GetType().GetProperty(PropertyName)?.GetValue(value) as string;
    }
}
