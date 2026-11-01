namespace ArdaNova.Application.Tests.Support;

using System.Linq;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Composition-root tests (namespace <c>ArdaNova.Application.Tests.Policies</c>) build a brand-new
/// <c>ServiceProvider</c> per <c>[Fact]</c> via <c>AddApplication</c>/<c>AddInfrastructure</c>. By the time
/// this assembly's real-Postgres flow tests (see <c>SponsorshipVoidFlowTests</c>,
/// <c>SettlementNoEffectReopenFlowTests</c>) have run in the same process, EF's "more than twenty
/// IServiceProvider instances" leak heuristic is already a false positive for whichever composition test
/// resolves the DbContext next, regardless of run order. <c>AddDbContext</c>'s own options registration uses
/// <c>TryAdd</c>, so a second <c>AddDbContext</c> call after <c>AddInfrastructure</c> would be silently
/// ignored; instead this wraps the already-registered <c>DbContextOptions&lt;ArdaNovaDbContext&gt;</c> factory
/// and layers the same <c>CoreEventId.ManyServiceProvidersCreatedWarning</c> suppression those flow tests use
/// on top of it, so the real <c>UseNpgsql</c>/enum-mapping configuration <c>AddInfrastructure</c> built stays
/// exactly what gets resolved.
/// </summary>
public static class CompositionRootWarnings
{
    public static IServiceCollection SuppressManyServiceProvidersCreatedWarning(this IServiceCollection services)
    {
        var registered = services.Single(d => d.ServiceType == typeof(DbContextOptions<ArdaNovaDbContext>));
        var originalFactory = registered.ImplementationFactory
            ?? throw new InvalidOperationException(
                "DbContextOptions<ArdaNovaDbContext> was not registered via a factory; AddInfrastructure's " +
                "AddDbContext call may have changed shape.");

        services.Replace(ServiceDescriptor.Describe(
            typeof(DbContextOptions<ArdaNovaDbContext>),
            provider =>
            {
                var original = (DbContextOptions<ArdaNovaDbContext>)originalFactory(provider);
                return new DbContextOptionsBuilder<ArdaNovaDbContext>(original)
                    .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .Options;
            },
            registered.Lifetime));

        return services;
    }
}
