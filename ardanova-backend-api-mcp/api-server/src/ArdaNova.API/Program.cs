using System.Text.Json.Serialization;
using ArdaNova.API.Middleware;
using ArdaNova.API.Readiness;
using ArdaNova.API.EventBus.Extensions;
using ArdaNova.API.WebSocket.Extensions;
using ArdaNova.Application;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

// Configure Npgsql to handle DateTime without requiring explicit UTC Kind
// This allows DateTime.UtcNow and other DateTime values to work with PostgreSQL timestamptz
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    if (!ApiKeyMiddleware.IsStrongApiKey(ApiKeyMiddleware.GetApiKey(builder.Configuration)))
        throw new InvalidOperationException("API_KEY must be a generated 32+ byte secret in production.");

    if (!ApiKeyMiddleware.IsStrongApiKey(ApiKeyMiddleware.GetAdminApiKey(builder.Configuration)))
        throw new InvalidOperationException("ADMIN_API_KEY must be a generated 32+ byte secret in production.");

    if (ActorAssertionMiddleware.GetSigningKey(builder.Configuration) is null)
        throw new InvalidOperationException("ACTOR_ASSERTION_HMAC_KEY must be a generated 32+ byte secret in production.");
}

var kycProvider = builder.Configuration["KYC_PROVIDER"] ?? Environment.GetEnvironmentVariable("KYC_PROVIDER");
if (string.Equals(kycProvider, "veriff", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "KYC_PROVIDER=veriff is not supported: Veriff integration is not implemented. Use KYC_PROVIDER=manual or omit the setting.");
}

// Add services to the container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

// Add Storage services (S3, Local, etc.)
builder.Services.AddStorageServices(builder.Configuration);

// Add EventBus and WebSocket services
builder.Services.AddEventBus();
builder.Services.AddArdaNovaWebSocket();

// Add CORS for SignalR (allows Next.js server to connect)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalR", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "http://localhost:3000",
            "https://localhost:3000"
        };

        // Add configured origin if present
        var nextJsUrl = Environment.GetEnvironmentVariable("NEXTJS_URL");
        if (!string.IsNullOrEmpty(nextJsUrl))
        {
            allowedOrigins.Add(nextJsUrl);
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddArdaNovaAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ArdaNova API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Description = "API Key authentication using the X-Api-Key header",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "ApiKey" },
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS for SignalR
app.UseCors("SignalR");

app.UseApiKeyAuthentication();

app.UseActorAssertions();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapArdaNovaHubs();

// Health check endpoint (no authentication required)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "ardanova-api"
})).AllowAnonymous();

app.MapGet("/ready", async (
    ArdaNovaDbContext dbContext,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IFundingSettlementReadiness fundingSettlementReadiness,
    IAzoaCustodialAccountGateway custodialAccountGateway,
    ISettlementOutboxRuntimeCapability outboxRuntime,
    AlgorandProviderCapabilities algorandCapabilities,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.Headers.CacheControl = "no-store";
    var apiKey = ApiKeyMiddleware.GetApiKey(configuration);
    var adminApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY")
        ?? configuration["AdminApiKey:Key"];
    var actorAssertionKey = Environment.GetEnvironmentVariable("ACTOR_ASSERTION_HMAC_KEY")
        ?? configuration["ActorAssertion:HmacKey"];
    var azoaMode = configuration["Azoa:Mode"] ?? "Simulated";
    var algorandProvider = algorandCapabilities.Provider;
    var fundingCheckoutEnabled = bool.TryParse(
        configuration["Azoa:EnableFundingCheckout"],
        out var parsedFundingCheckout) && parsedFundingCheckout;
    var custodialAccountsEnabled = bool.TryParse(
        configuration["Azoa:EnableCustodialAccounts"],
        out var parsedCustodialAccounts) && parsedCustodialAccounts;
    var settlementOutboxEnabled = bool.TryParse(
        configuration["Azoa:EnableSettlementOutboxWorker"],
        out var parsedSettlementOutbox) && parsedSettlementOutbox;
    var valueTransportEnabled = fundingCheckoutEnabled
        || settlementOutboxEnabled
        || azoaMode.Equals("Live", StringComparison.OrdinalIgnoreCase)
        || algorandProvider.Equals("Azoa", StringComparison.OrdinalIgnoreCase);
    var azoaEnabled = valueTransportEnabled || custodialAccountsEnabled;
    var azoaSettings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
        ?? new AzoaSettings();
    var custodyApiKey = AzoaCredentialSelection.ResolveCustodyKey(
        azoaSettings,
        environment.IsProduction());
    var valueApiKey = AzoaCredentialSelection.ResolveValueKey(
        azoaSettings,
        environment.IsProduction());
    var questApiKey = AzoaCredentialSelection.ResolveQuestKey(
        azoaSettings,
        environment.IsProduction());
    var questTransportEnabled = !string.IsNullOrWhiteSpace(questApiKey);
    azoaEnabled = azoaEnabled || questTransportEnabled;
    var hasSeparatedAzoaCredentials = AzoaCredentialSelection.AreCredentialsSeparated(
        custodyApiKey,
        valueApiKey,
        questApiKey,
        environment.IsProduction());
    var azoaBaseUrl = configuration["Azoa:BaseUrl"];
    var hasValidAzoaBaseUrl = Uri.TryCreate(azoaBaseUrl, UriKind.Absolute, out var parsedAzoaBaseUrl)
        && (parsedAzoaBaseUrl.Scheme == Uri.UriSchemeHttp
            || parsedAzoaBaseUrl.Scheme == Uri.UriSchemeHttps)
        && (!environment.IsProduction() || parsedAzoaBaseUrl.Scheme == Uri.UriSchemeHttps)
        && string.IsNullOrEmpty(parsedAzoaBaseUrl.UserInfo);
    var hasValidAzoaTimeout = int.TryParse(
        configuration["Azoa:TimeoutSeconds"],
        out var azoaTimeoutSeconds)
        && azoaTimeoutSeconds is > 0 and <= 120;
    var azoaTransportReady = !azoaEnabled
        || (hasValidAzoaBaseUrl
            && (!custodialAccountsEnabled
                || (!environment.IsProduction()
                    ? !string.IsNullOrWhiteSpace(custodyApiKey)
                    : ApiKeyMiddleware.IsStrongSecret(custodyApiKey)))
            && (!valueTransportEnabled
                || (!environment.IsProduction()
                    ? !string.IsNullOrWhiteSpace(valueApiKey)
                    : ApiKeyMiddleware.IsStrongSecret(valueApiKey)))
            && (!questTransportEnabled
                || (!environment.IsProduction()
                    || ApiKeyMiddleware.IsStrongSecret(questApiKey)))
            && hasSeparatedAzoaCredentials
            && hasValidAzoaTimeout
            && (!custodialAccountsEnabled
                || Guid.TryParse(configuration["Azoa:TenantId"], out _))
            && (!fundingCheckoutEnabled
                || !string.IsNullOrWhiteSpace(configuration["Azoa:SelectedSettlementNodeId"])));
    var fundingSettlementReady = !fundingCheckoutEnabled || fundingSettlementReadiness.IsReady;
    var custodialAccountsReady = !custodialAccountsEnabled;
    string? custodialAccountsUnavailableReason = null;
    if (custodialAccountsEnabled && azoaTransportReady)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(2500));
            var capabilities = await custodialAccountGateway.GetCapabilitiesAsync(timeout.Token);
            custodialAccountsReady = capabilities.IsSuccess && capabilities.Value?.Ready == true;
            custodialAccountsUnavailableReason = capabilities.Value?.UnavailableReason;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            custodialAccountsReady = false;
            custodialAccountsUnavailableReason = "AZOA custody capability probe failed.";
        }
    }
    var azoaReady = azoaTransportReady && fundingSettlementReady && custodialAccountsReady;

    var configurationReady = ApiKeyMiddleware.IsStrongApiKey(apiKey)
        && ApiKeyMiddleware.IsStrongSecret(actorAssertionKey)
        && (!environment.IsProduction()
            || ApiKeyMiddleware.IsStrongApiKey(adminApiKey))
        && azoaReady;

    var databaseReady = false;
    var releaseSchemaReady = false;
    try
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(2500));
        databaseReady = await dbContext.Database.CanConnectAsync(timeout.Token);
        if (databaseReady)
            releaseSchemaReady = await ReleaseSchemaReadiness.CheckAsync(dbContext, timeout.Token);
    }
    catch (Exception) when (!cancellationToken.IsCancellationRequested)
    {
        databaseReady = false;
        releaseSchemaReady = false;
    }

    var ready = configurationReady && databaseReady && releaseSchemaReady;
    return Results.Json(new
    {
        status = ready ? "ready" : "not_ready",
        timestamp = DateTime.UtcNow,
        service = "ardanova-api",
        checks = new
        {
            configuration = new { ready = configurationReady },
            database = new { ready = databaseReady },
            releaseSchema = new { ready = releaseSchemaReady },
            azoa = new
            {
                enabled = azoaEnabled,
                ready = azoaReady,
                fundingSettlementReady,
                questTransportEnabled,
                custodialAccountsEnabled,
                custodialAccountsReady,
                custodialAccountsUnavailableReason,
                outboxWorkerRegistered = outboxRuntime.IsHostedDispatcherRegistered,
            },
            algorand = new
            {
                provider = algorandProvider,
                noChain = algorandCapabilities.IsNoChain,
                credentialLifecycleSupported = algorandCapabilities.SupportsAddressBasedCredentialLifecycle,
            }
        }
    }, statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.Run();
