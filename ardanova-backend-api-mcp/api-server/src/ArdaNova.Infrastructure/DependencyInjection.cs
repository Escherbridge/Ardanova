namespace ArdaNova.Infrastructure;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Security;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Algorand;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Outbox;
using ArdaNova.Infrastructure.Payments;
using ArdaNova.Infrastructure.Repositories;
using ArdaNova.Infrastructure.Security;
using ArdaNova.Infrastructure.UnitOfWork;
using ArdaNova.Infrastructure.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Npgsql.NameTranslation;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        // Database credentials are deployment configuration; see the root environment template.
        var connectionString = GetConnectionString(configuration);

        // Build Npgsql data source with enum mappings for PostgreSQL native enums
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        // Dynamically map all .NET enums from ArdaNova.Domain.Models.Enums to PostgreSQL native enums
        // Prisma uses PascalCase type names and SCREAMING_SNAKE_CASE values
        // NpgsqlNullNameTranslator preserves the original names without transformation
        var translator = new NpgsqlNullNameTranslator();
        var enumTypes = typeof(TaskPriority).Assembly
            .GetTypes()
            .Where(t => t.IsEnum && t.Namespace == "ArdaNova.Domain.Models.Enums");

        foreach (var enumType in enumTypes)
        {
            dataSourceBuilder.MapEnum(enumType, enumType.Name, translator);
        }

        var dataSource = dataSourceBuilder.Build();

        // DbContext - Note: Prisma manages schema/migrations, EF Core only reads/writes
        services.AddDbContext<ArdaNovaDbContext>(options =>
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                // External data sources require enum registration at both the
                // ADO.NET and EF provider layers (Npgsql EF 9+ contract).
                foreach (var enumType in enumTypes)
                {
                    npgsqlOptions.MapEnum(
                        enumType,
                        enumType.Name,
                        "public",
                        translator);
                }
            }));

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IStripeWebhookInbox, StripeWebhookInbox>();
        services.AddScoped<IActorAssertionReplayLedger, ActorAssertionReplayLedger>();
        services.AddScoped<IActorAssertionReplayCleanupStore, ActorAssertionReplayCleanupStore>();
        services.AddScoped<ActorAssertionReplayCleanupCycle>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ActorAssertionReplayCleanupService>();
        services.AddScoped<IWalletVerificationChallengeStore, WalletVerificationChallengeStore>();
        services.AddScoped<IWalletProofVerifier, AlgorandWalletProofVerifier>();
        services.AddScoped<IEconomicOutboxLeaseStore, EconomicOutboxLeaseStore>();

        // Entity-specific Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // AZOA shared-node integration (contract §2/§3/§11).
        services.Configure<AzoaSettings>(
            configuration.GetSection(AzoaSettings.SectionName));
        var azoaSettings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var custodyApiKey = AzoaCredentialSelection.ResolveCustodyKey(azoaSettings, isProduction);
        var valueApiKey = AzoaCredentialSelection.ResolveValueKey(azoaSettings, isProduction);
        var questApiKey = AzoaCredentialSelection.ResolveQuestKey(azoaSettings, isProduction);
        ValidateAzoaMode(azoaSettings.Mode);
        ValidateAzoaTransportSettings(azoaSettings, isProduction);
        ValidateCredentialSeparation(custodyApiKey, valueApiKey, questApiKey, isProduction);
        ValidateCustodialAccounts(azoaSettings, custodyApiKey, isProduction);
        ValidateValueFeatures(azoaSettings, valueApiKey, isProduction);
        ValidateQuestCredential(azoaSettings, questApiKey, isProduction);
        services.AddHttpClient<IAzoaNodeClient, AzoaNodeClient>(client =>
        {
            ConfigureAzoaHttpClient(client, azoaSettings, valueApiKey);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });
        services.AddHttpClient<IAzoaCustodialNodeClient, AzoaCustodialNodeClient>(client =>
        {
            ConfigureAzoaHttpClient(client, azoaSettings, custodyApiKey);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });
        services.AddHttpClient<IAzoaQuestNodeClient, AzoaQuestNodeClient>(client =>
        {
            ConfigureAzoaHttpClient(client, azoaSettings, questApiKey);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });
        services.AddHttpClient<IAzoaPublicNodeClient, AzoaPublicNodeClient>(client =>
        {
            ConfigureAzoaHttpClient(client, azoaSettings, apiKey: string.Empty);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });
        // Application-layer ports onto the node (dependency-inversion seam, like IAlgorandService).
        services.AddScoped<IAzoaAvatarNode, AzoaAvatarNodeAdapter>();
        if (azoaSettings.EnableCustodialAccounts)
            services.AddScoped<IAzoaCustodialAccountGateway, AzoaCustodialAccountGateway>();
        else
            services.AddScoped<IAzoaCustodialAccountGateway, DisabledAzoaCustodialAccountGateway>();
        services.AddScoped<IAzoaQuestNode, AzoaQuestNodeAdapter>();
        services.AddScoped<IAzoaAllocationNode, AzoaAllocationNodeAdapter>();
        services.AddScoped<IAzoaSettlementGateway, DisabledAzoaSettlementGateway>();
        services.AddScoped<IFundingSettlementReadiness, AzoaFundingSettlementReadiness>();

        var outboxOptions = CreateOutboxOptions(azoaSettings);
        services.AddSingleton(outboxOptions);
        services.AddSingleton<ISettlementOutboxRuntimeCapability>(
            new SettlementOutboxRuntimeCapability(outboxOptions.Enabled));
        services.AddScoped<EconomicOutboxDispatchWorker>();
        if (outboxOptions.Enabled)
            services.AddHostedService<EconomicOutboxHostedService>();

        // Algorand blockchain service.
        // The IAlgorandService implementation is selected by the Algorand:Provider
        // flag (Legacy | Azoa | Simulated). Default Legacy preserves current
        // behavior; the Azoa-backed adapter is registered by the provider-adapter track.
        // The interface is the seam — every consumer (CredentialUtilityService,
        // tokenomics, …) binds to IAlgorandService and is unaffected by the choice.
        services.Configure<AlgorandSettings>(
            configuration.GetSection(AlgorandSettings.SectionName));

        var algorandSettings = configuration.GetSection(AlgorandSettings.SectionName).Get<AlgorandSettings>()
            ?? new AlgorandSettings();
        var algorandProvider = AlgorandProviderSelection.Parse(
            configuration[$"{AlgorandSettings.SectionName}:Provider"]);
        ValidateAlgorandProvider(
            algorandProvider,
            algorandSettings,
            azoaSettings,
            valueApiKey,
            isProduction);

        var providerCapabilities = algorandProvider switch
        {
            AlgorandProvider.Legacy => new AlgorandProviderCapabilities("Legacy", false, true),
            AlgorandProvider.Azoa => new AlgorandProviderCapabilities("Azoa", false, false),
            AlgorandProvider.Simulated => new AlgorandProviderCapabilities("Simulated", true, true),
            _ => throw new InvalidOperationException("Unsupported Algorand provider selection."),
        };
        services.AddSingleton(providerCapabilities);

        if (algorandProvider == AlgorandProvider.Azoa)
        {
            // Azoa-backed adapter: routes value moves to the shared/managed AZOA
            // node via IAzoaNodeClient (registered in the AZOA block above). No
            // HttpClient of its own — the node client owns transport.
            services.AddScoped<IAlgorandService, AzoaBackedAlgorandService>();
        }
        else if (algorandProvider == AlgorandProvider.Legacy)
        {
            services.AddHttpClient<IAlgorandService, AlgorandService>();
        }
        else
        {
            services.AddSingleton<IAlgorandService, SimulatedAlgorandService>();
        }

        return services;
    }

    private static EconomicOutboxDispatchOptions CreateOutboxOptions(AzoaSettings settings)
    {
        if (settings.EnableSettlementOutboxWorker
            && (settings.SettlementOutboxBatchSize is < 1 or > 100
                || settings.SettlementOutboxIntervalSeconds is < 1 or > 300))
        {
            throw new InvalidOperationException(
                "Enabled AZOA settlement outbox worker requires batch size 1-100 and interval 1-300 seconds.");
        }

        return new EconomicOutboxDispatchOptions
        {
            Enabled = settings.EnableSettlementOutboxWorker,
            BatchSize = settings.SettlementOutboxBatchSize,
            Interval = TimeSpan.FromSeconds(settings.SettlementOutboxIntervalSeconds),
        };
    }

    private static void ValidateAzoaMode(string? configuredMode)
    {
        if (!string.Equals(configuredMode, "Live", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(configuredMode, "Simulated", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unknown Azoa:Mode '{configuredMode}'. Allowed values are Live and Simulated.");
        }
    }

    private static void ValidateAzoaTransportSettings(
        AzoaSettings settings,
        bool isProduction)
    {
        if (settings.TimeoutSeconds is < 1 or > 120)
            throw new InvalidOperationException("Azoa:TimeoutSeconds must be between 1 and 120.");

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            if (isProduction && HasConfiguredAzoaCapability(settings))
            {
                throw new InvalidOperationException(
                    "Configured Production AZOA capabilities require an HTTPS Azoa:BaseUrl.");
            }

            return;
        }

        if (!IsValidAzoaBaseUrl(settings.BaseUrl, isProduction))
        {
            throw new InvalidOperationException(
                "Azoa:BaseUrl must be an absolute origin URL without credentials, path, query, or fragment (HTTPS in Production).");
        }
    }

    private static void ConfigureAzoaHttpClient(
        HttpClient client,
        AzoaSettings settings,
        string apiKey)
    {
        if (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var azoaBaseUri))
            client.BaseAddress = azoaBaseUri;
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        client.Timeout = TimeSpan.FromSeconds(
            settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
    }

    private static bool IsValidAzoaBaseUrl(string? value, bool isProduction)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && (!isProduction || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);

    private static bool HasConfiguredAzoaCapability(AzoaSettings settings)
        => settings.EnableCustodialAccounts
            || settings.EnableFundingCheckout
            || settings.EnableSettlementOutboxWorker
            || settings.Mode.Equals("Live", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(settings.CustodyApiKey)
            || !string.IsNullOrWhiteSpace(settings.ValueApiKey)
            || !string.IsNullOrWhiteSpace(settings.QuestApiKey);

    private static void ValidateCredentialSeparation(
        string custodyApiKey,
        string valueApiKey,
        string questApiKey,
        bool isProduction)
    {
        if (!AzoaCredentialSelection.AreCredentialsSeparated(
                custodyApiKey,
                valueApiKey,
                questApiKey,
                isProduction))
        {
            throw new InvalidOperationException(
                "Production AZOA custody, value, and quest credentials must be different scoped API keys.");
        }
    }

    private static void ValidateCustodialAccounts(
        AzoaSettings settings,
        string custodyApiKey,
        bool isProduction)
    {
        if (!settings.EnableCustodialAccounts)
            return;

        if (!IsValidAzoaBaseUrl(settings.BaseUrl, isProduction))
        {
            throw new InvalidOperationException(
                "Enabled AZOA custodial accounts require an absolute Azoa:BaseUrl (HTTPS in Production)."
            );
        }

        if (!Guid.TryParse(settings.TenantId, out _))
            throw new InvalidOperationException("Enabled AZOA custodial accounts require a canonical tenant GUID.");
        if (!IsValidAzoaCredential(custodyApiKey, isProduction))
        {
            throw new InvalidOperationException(
                "Enabled AZOA custodial accounts require a generated, non-placeholder Azoa:CustodyApiKey (at least 32 bytes in Production)."
            );
        }
    }

    private static void ValidateValueFeatures(
        AzoaSettings settings,
        string valueApiKey,
        bool isProduction)
    {
        if (!settings.EnableFundingCheckout
            && !settings.EnableSettlementOutboxWorker
            && !settings.Mode.Equals("Live", StringComparison.OrdinalIgnoreCase))
            return;

        if (!IsValidAzoaBaseUrl(settings.BaseUrl, isProduction)
            || !IsValidAzoaCredential(valueApiKey, isProduction))
        {
            throw new InvalidOperationException(
                "Enabled AZOA value features require an absolute Azoa:BaseUrl (HTTPS in Production) and a generated, non-placeholder Azoa:ValueApiKey (at least 32 bytes in Production)."
            );
        }
    }

    private static void ValidateQuestCredential(
        AzoaSettings settings,
        string questApiKey,
        bool isProduction)
    {
        if (string.IsNullOrWhiteSpace(questApiKey))
            return;

        if (!IsValidAzoaBaseUrl(settings.BaseUrl, isProduction)
            || !IsValidAzoaCredential(questApiKey, isProduction))
        {
            throw new InvalidOperationException(
                "Azoa:QuestApiKey requires an absolute Azoa:BaseUrl (HTTPS in Production) and a generated, non-placeholder key of at least 32 bytes in Production.");
        }
    }

    private static void ValidateAlgorandProvider(
        AlgorandProvider provider,
        AlgorandSettings algorand,
        AzoaSettings azoa,
        string valueApiKey,
        bool isProduction)
    {
        if (provider == AlgorandProvider.Legacy && isProduction)
        {
            if (!algorand.AllowLegacyCustodialSignerInProduction)
                throw new InvalidOperationException(
                    "Algorand:Provider=Legacy is forbidden in Production without Algorand:AllowLegacyCustodialSignerInProduction=true.");
            if (string.IsNullOrWhiteSpace(algorand.PlatformMnemonic)
                || string.IsNullOrWhiteSpace(algorand.PlatformAddress))
            {
                throw new InvalidOperationException(
                    "The Production legacy signer break-glass requires both PlatformMnemonic and PlatformAddress.");
            }
        }

        if (provider == AlgorandProvider.Azoa)
        {
            if (algorand.RequireCredentialOperations)
                throw new InvalidOperationException(
                    "The Azoa Algorand adapter does not support the address-based credential lifecycle required by Algorand:RequireCredentialOperations.");
            if (!IsValidAzoaBaseUrl(azoa.BaseUrl, isProduction)
                || !IsValidAzoaCredential(valueApiKey, isProduction))
            {
                throw new InvalidOperationException(
                    "Algorand:Provider=Azoa requires an absolute Azoa:BaseUrl (HTTPS in Production) and a generated, non-placeholder Azoa:ValueApiKey.");
            }
        }
    }

    private static bool IsValidAzoaCredential(string? value, bool isProduction)
        => isProduction
            ? GeneratedSecretValidator.IsValid(value)
            : !string.IsNullOrWhiteSpace(value);

    private static string GetConnectionString(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertPostgresUrlToConnectionString(databaseUrl);
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            "Database connection string not configured. Set DATABASE_URL or ConnectionStrings__DefaultConnection through the deployment or local process environment.");
    }

    /// <summary>
    /// Converts a PostgreSQL URL (postgres://user:pass@host:port/db) to a .NET connection string
    /// </summary>
    private static string ConvertPostgresUrlToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

        // Check for SSL in query string
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryParams = uri.Query.TrimStart('?').Split('&')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].ToLower(), p => p[1]);

            if (queryParams.TryGetValue("sslmode", out var sslMode))
            {
                connectionString += $";SSL Mode={sslMode}";
            }
        }

        // Default to requiring SSL for known cloud providers
        if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
        {
            if (databaseUrl.Contains("railway") || databaseUrl.Contains("render") ||
                databaseUrl.Contains("supabase") || databaseUrl.Contains("neon"))
            {
                connectionString += ";SSL Mode=Require;Trust Server Certificate=true";
            }
        }

        return connectionString;
    }
}
