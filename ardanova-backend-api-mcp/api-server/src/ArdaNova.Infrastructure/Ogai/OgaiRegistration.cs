namespace ArdaNova.Infrastructure.Ogai;

using System.Net.Http.Headers;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Selects the Nova AI upstream: live partner client, local preview, or disabled in Production. See AGENTS.md.</summary>
public static class OgaiRegistration
{
    /// <summary>Config key for the Nova session-ticket secret; unset falls back to the actor-assertion key (AGENTS.md §Session tickets).</summary>
    public const string SessionKeyPath = "Nova:SessionKey";

    public static IServiceCollection AddOgai(this IServiceCollection services, IConfiguration configuration, bool isProduction)
    {
        var ticketKey = NovaSessionTickets.DeriveKey(
            FirstSet(Environment.GetEnvironmentVariable("NOVA_SESSION_KEY"), configuration[SessionKeyPath],
                Environment.GetEnvironmentVariable("ACTOR_ASSERTION_HMAC_KEY"), configuration["ActorAssertion:HmacKey"]));
        services.AddSingleton(sp => new NovaSessionTickets(ticketKey, sp.GetService<TimeProvider>() ?? TimeProvider.System));

        var settings = configuration.GetSection(OgaiSettings.SectionName).Get<OgaiSettings>() ?? new OgaiSettings();
        if (!settings.IsConfigured)
        {
            if (isProduction)
                services.AddSingleton<IOgaiClient, DisabledOgaiClient>();
            else
                services.AddSingleton<IOgaiClient, LocalNovaPreviewClient>();
            return services;
        }

        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri)
            || (isProduction && baseUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Ogai:BaseUrl must be an absolute URL (https in Production).");

        services.AddHttpClient<IOgaiClient, OgaiClient>(client =>
            {
                client.BaseAddress = new Uri(baseUri.ToString().TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 120));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.PartnerApiKey);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .RedactLoggedHeaders(_ => true);
        return services;
    }

    private static string? FirstSet(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
