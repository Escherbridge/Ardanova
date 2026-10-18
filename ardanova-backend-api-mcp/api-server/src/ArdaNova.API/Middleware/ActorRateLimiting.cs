using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArdaNova.API.Middleware;

/// <summary>Per-actor fixed-window request budget; service-key-only calls share the BFF and are not limited. See AGENTS.md §Rate limiting.</summary>
public static class ActorRateLimiting
{
    public const string PermitsPerMinuteKey = "RateLimiting:ActorRequestsPerMinute";
    public const int DefaultPermitsPerMinute = 600;
    public const string TooManyRequestsMessage = "Too many requests. Try again shortly.";

    public static IServiceCollection AddArdaNovaActorRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration.GetValue<int?>(PermitsPerMinuteKey);
        var permits = configured is > 0 ? configured.Value : DefaultPermitsPerMinute;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                ActorAssertionMiddleware.TryGetActorId(context.User, out var actorId)
                    ? RateLimitPartition.GetFixedWindowLimiter(actorId, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permits,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    })
                    : RateLimitPartition.GetNoLimiter(string.Empty));
            options.OnRejected = (rejection, _) =>
            {
                if (rejection.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    rejection.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                return new ValueTask(ApiErrorEnvelope.WriteAsync(
                    rejection.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    TooManyRequestsMessage));
            };
        });
        return services;
    }

    /// <summary>Must run after <c>UseActorAssertions</c>, which is what names the partition.</summary>
    public static IApplicationBuilder UseArdaNovaActorRateLimiting(this IApplicationBuilder app)
        => app.UseRateLimiter();
}
