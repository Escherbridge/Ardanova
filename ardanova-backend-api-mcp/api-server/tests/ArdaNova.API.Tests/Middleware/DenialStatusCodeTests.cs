namespace ArdaNova.API.Tests.Middleware;

using System.Security.Claims;
using System.Text.Json;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

/// <summary>Drives the real authorization pipeline and ForbidResult against the registered denial scheme.</summary>
public class DenialStatusCodeTests
{
    private static readonly ClaimsPrincipal ServiceKeyCaller = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, "api-client"), new Claim(ClaimTypes.Role, "Service")], "ApiKey"));

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public static TheoryData<string, ClaimsPrincipal, int> PolicyDenials => new()
    {
        { "service key without an actor assertion", ServiceKeyCaller, StatusCodes.Status403Forbidden },
        { "no authenticated caller at all", Anonymous, StatusCodes.Status401Unauthorized },
    };

    [Theory]
    [MemberData(nameof(PolicyDenials))]
    public async Task ActorAssertionEndpoint_DeniesWithAuthStatus_NeverBadRequest(
        string _case, ClaimsPrincipal caller, int expectedStatus)
    {
        var services = BuildServices();
        var context = NewContext(services, caller);
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(AuthorizationPolicies.ActorAssertion)),
            "actor-bound"));
        var reachedEndpoint = false;
        var middleware = new Microsoft.AspNetCore.Authorization.AuthorizationMiddleware(
            _ => { reachedEndpoint = true; return Task.CompletedTask; },
            services.GetRequiredService<IAuthorizationPolicyProvider>(),
            services);

        await middleware.Invoke(context);

        reachedEndpoint.Should().BeFalse();
        context.Response.StatusCode.Should().Be(expectedStatus);
        (await ReadError(context)).Should().NotContain("authenticationScheme");
    }

    [Fact]
    public async Task ControllerForbid_WritesForbidden()
    {
        var services = BuildServices();
        var context = NewContext(services, ServiceKeyCaller);

        await new ForbidResult().ExecuteResultAsync(
            new ActionContext(context, new RouteData(), new ActionDescriptor()));

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource());
        services.AddArdaNovaAuthorization();
        services.AddArdaNovaDenialStatusCodes();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext NewContext(IServiceProvider services, ClaimsPrincipal caller)
    {
        var context = new DefaultHttpContext { RequestServices = services, User = caller };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadError(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.GetProperty("error").GetString() ?? "";
    }
}
