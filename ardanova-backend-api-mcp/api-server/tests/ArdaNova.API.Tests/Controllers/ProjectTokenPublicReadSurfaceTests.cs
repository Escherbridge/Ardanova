namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using ArdaNova.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// OI-95, made a control instead of a count in prose.
///
/// The ledger row deliberately accepts a set of unauthenticated reads on
/// <see cref="ProjectTokensController"/>: they carry only the project-level facts a public
/// sponsorship page legitimately shows. The row is a DECISION, so what matters is that it names
/// the surface exactly — it originally said four and there were five, and an accepted-risk
/// record that undercounts is how the next reviewer concludes the surface was enumerated.
///
/// So the number is asserted against the controller by reflection rather than re-read by hand.
/// Adding an open route, or quietly removing an <c>[Authorize]</c> from an existing one, fails
/// here with the route named, and the fix is either the attribute or an updated OI-95.
/// </summary>
public sealed class ProjectTokenPublicReadSurfaceTests
{
    /// <summary>
    /// The reads OI-95 accepts, by action name. Exactly this set — no more, and no fewer.
    /// </summary>
    private static readonly string[] AcceptedOpenReads =
    {
        nameof(ProjectTokensController.GetConfig),              // GET  config/{id}
        nameof(ProjectTokensController.GetMetadata),            // POST config/metadata/batch
        nameof(ProjectTokensController.GetConfigByProject),     // GET  config/by-project/{projectId}
        nameof(ProjectTokensController.GetSupplyBreakdown),     // GET  config/{id}/supply
        nameof(ProjectTokensController.GetGateStatus),          // GET  {configId}/gate
    };

    [Fact]
    public void The_unauthenticated_surface_is_exactly_the_set_OI95_accepted()
    {
        var open = OpenActions();

        open.Should().BeEquivalentTo(
            AcceptedOpenReads,
            "OI-95 records this surface as a deliberate decision, so it has to name it exactly. "
            + "A route here that OI-95 does not list is either missing an [Authorize] or missing "
            + "from the ledger; a listed route that is no longer here means the row is stale.");
    }

    [Fact]
    public void Every_accepted_open_read_is_a_read_and_nothing_it_returns_is_a_mutation()
    {
        // POST config/metadata/batch is a POST only because it takes a list of ids in a body.
        // The accepted-harm argument in OI-95 is that these routes READ project-level facts; a
        // route that mutated would not be covered by it, whatever its verb.
        foreach (var action in OpenActions())
        {
            var method = typeof(ProjectTokensController).GetMethod(action)!;
            method.Name.Should().StartWith(
                "Get",
                "an unauthenticated route on this controller is accepted only as a read");
        }
    }

    /// <summary>
    /// Every public action carrying no <c>[Authorize]</c> of its own. The controller declares no
    /// class-level <c>[Authorize]</c>, which the first assertion pins, so an action's own
    /// attribute is the whole of its authorization.
    /// </summary>
    private static IEnumerable<string> OpenActions()
    {
        var controller = typeof(ProjectTokensController);

        controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Should().BeEmpty(
            "this test reads action-level attributes only; a class-level [Authorize] would make "
            + "every route below authorized and this enumeration meaningless");

        return controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
            .Where(method => !method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any())
            .Select(method => method.Name)
            .ToArray();
    }
}
