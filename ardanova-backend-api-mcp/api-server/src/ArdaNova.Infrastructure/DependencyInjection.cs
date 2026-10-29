namespace ArdaNova.Infrastructure;

using System.Globalization;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Security;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Algorand;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Feed;
using ArdaNova.Infrastructure.Ogai;
using ArdaNova.Infrastructure.Outbox;
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
        // No payment-provider webhook inbox: AZOA terminates the fiat rail and ArdaNova reads the
        // on-ramp operation, so nothing here consumes a provider callback.
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
        services.AddScoped<IProjectDeletionBlockers, ProjectDeletionBlockers>();

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
        // The issuance credential is read straight from configuration rather than bound onto
        // AzoaSettings: it belongs to the credential-issue door alone and no other client may
        // resolve it. A blank value means NOT CONFIGURED and there is no fallback to another
        // key - see AzoaCredentialIssuanceRegistration.
        var issuanceApiKey =
            configuration[AzoaCredentialIssuanceRegistration.IssuanceApiKeyPath]?.Trim() ?? string.Empty;
        ValidateAzoaMode(azoaSettings.Mode);
        ValidateAzoaTransportSettings(azoaSettings, isProduction);
        ValidateCredentialSeparation(custodyApiKey, valueApiKey, questApiKey, isProduction);
        ValidateCustodialAccounts(azoaSettings, custodyApiKey, isProduction);
        ValidateValueFeatures(azoaSettings, valueApiKey, isProduction);
        ValidateQuestCredential(azoaSettings, questApiKey, isProduction);
        ValidateIssuanceCredential(
            azoaSettings, issuanceApiKey, custodyApiKey, valueApiKey, questApiKey, isProduction);
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
        // Application-layer ports onto the node (dependency-inversion seam, like IAlgorandService).
        if (azoaSettings.EnableCustodialAccounts)
            services.AddScoped<IAzoaCustodialAccountGateway, AzoaCustodialAccountGateway>();
        else
            services.AddScoped<IAzoaCustodialAccountGateway, DisabledAzoaCustodialAccountGateway>();
        services.AddScoped<IAzoaQuestNode, AzoaQuestNodeAdapter>();
        services.AddScoped<IAzoaAllocationNode, AzoaAllocationNodeAdapter>();
        // The settlement gateway an operator actually selected. The capability is computed from the
        // settings validated above plus an EXPIRING attestation, so the funding-readiness gate reads
        // configuration facts rather than a constant: an unset or lapsed
        // Azoa:SettlementAttestationExpiresAt closes checkout on its own.
        var settlementCapability = new AzoaSettlementGatewayCapability(
            SelectedNodeId: azoaSettings.SelectedSettlementNodeId?.Trim() ?? string.Empty,
            TransportConfigured: IsValidAzoaBaseUrl(azoaSettings.BaseUrl, isProduction)
                && IsValidAzoaCredential(valueApiKey, isProduction),
            OperatorAttestationExpiresAt: ReadSettlementAttestationExpiry(configuration));
        services.AddSingleton(settlementCapability);
        if (string.IsNullOrWhiteSpace(settlementCapability.SelectedNodeId))
            services.AddScoped<IAzoaSettlementGateway, DisabledAzoaSettlementGateway>();
        else
            services.AddScoped<IAzoaSettlementGateway, AzoaSettlementGateway>();
        services.AddScoped<IFundingSettlementReadiness, AzoaFundingSettlementReadiness>();
        // The exchange ports (node exchange, project-treasury and holdings read models). These
        // never fall back to the database, so without this registration TreasuryService.ReconcileAsync
        // and TokenBalanceService.GetArdaBalanceAsync refuse every call.
        services.AddAzoaExchangeClient(configuration, environmentName);

        // The worker-award hold port. TaskCommerceService reserves and releases worker awards
        // through this client; without the registration the award hold path never resolves.
        services.AddAzoaHoldClient(configuration, environmentName);

        // The add-funds (on-ramp) adapter. Application binds IFiatRailGateway to the fail-closed
        // DisabledFiatRailGateway; this call replaces it with the node-backed rail and registers
        // the price schedule ONLY when Azoa:OnRamp:Enabled is set and the transport resolves.
        // Without this line the refusal is permanent and no price is ever registered.
        services.AddAzoaFiatRail(configuration, environmentName);

        // The cash-out (off-ramp) adapter — the withdraw half. Registers the IAzoaCashOutNode
        // binding ONLY when the node transport resolves; without it PayoutService's own
        // fail-closed path reports that cashing out is not available rather than throwing at
        // the first withdrawal. Without this line the withdraw leg is unbound and inert.
        services.AddAzoaCashOut(configuration, environmentName);

        // The project funding gateway - where a project's money lands, and how much of its stake
        // each side of the project may be issued. Registers IAzoaProjectTokenNodeGateway ONLY when
        // both credentials it needs resolve; without this line ProjectTokenService.ProvisionAsync
        // refuses every project by name and nothing on the platform can be funded at all.
        services.AddAzoaProjectTokenGateway(configuration, environmentName);

        // The credential issuance adapter. Registers IAzoaCredentialIssuanceNode ONLY when
        // Azoa:IssuanceApiKey resolves; without it CredentialUtilityService records the grant and
        // leaves the member's card reading "Verification pending" rather than claiming a proof it
        // does not hold.
        services.AddAzoaCredentialIssuance(configuration);

        // Settlement rows that fell pending are promoted on a timer, and parked unit
        // sales are chased on one, so an operator read is a fallback rather than the only
        // path (OI-88b, OI-86b). Each registers its options always and the hosted service
        // unless its own flag is explicitly "false".
        services.AddSettlementPendingPromoter(configuration);
        services.AddParkedPayoutReconciler(configuration);

        // The feed projector writes the system cards of the one polymorphic feed (SR-03). Its timer is OFF in
        // every environment unless Feed:Projector:Enabled=true: a Development API can point at a shared
        // database (the dev compose's .env does), and the projector's first cycle deletes. See Feed/AGENTS.md.
        services.AddFeedProjector(configuration);

        // Nova AI upstream (OVERSEASGENAI partner API); see Ogai/AGENTS.md.
        services.AddOgai(configuration, isProduction);

        var outboxOptions = CreateOutboxOptions(azoaSettings);
        services.AddSingleton(outboxOptions);
        services.AddSingleton<ISettlementOutboxRuntimeCapability>(
            new SettlementOutboxRuntimeCapability(outboxOptions.Enabled));
        services.AddScoped<EconomicOutboxDispatchWorker>();
        if (outboxOptions.Enabled)
            services.AddHostedService<EconomicOutboxHostedService>();

        // Algorand blockchain service.
        // The IAlgorandService implementation is selected by the Algorand:Provider
        // flag (Legacy | Azoa | Simulated). An unset flag resolves to Simulated, which is a
        // DEVELOPER default only - ValidateAlgorandProvider rejects it in Production.
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

    /// <summary>
    /// Reads the operator's settlement attestation expiry. Absent means "never attested", which
    /// keeps funding checkout closed; a malformed value is a startup failure rather than a silent
    /// downgrade, because a typo that quietly disables live settlement is indistinguishable from a
    /// node outage until someone reads the logs.
    /// </summary>
    private static DateTimeOffset? ReadSettlementAttestationExpiry(IConfiguration configuration)
    {
        var configured = configuration[$"{AzoaSettings.SectionName}:SettlementAttestationExpiresAt"];
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        if (!DateTimeOffset.TryParse(
                configured,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
        {
            throw new InvalidOperationException(
                "Azoa:SettlementAttestationExpiresAt must be an ISO-8601 instant (for example 2026-12-31T00:00:00Z).");
        }

        return expiresAt;
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
        // Checked before the enablement early-return: a mis-promoted dev flag must be loud
        // even on a deploy that has custody off (mirrors AZOA's KycRuntimeSafety stance).
        if (settings.AllowInsecureDevVerificationUrl && isProduction)
        {
            throw new InvalidOperationException(
                "Azoa:AllowInsecureDevVerificationUrl is a development-only flag and must not be set in Production.");
        }

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

    /// <summary>
    /// Fail-fast boot guard on the credential-issue key. Blank is a supported state and means the
    /// adapter is not registered, so nothing is issued and no card claims a proof. A key that IS
    /// present must be usable and must be its OWN key: it carries the <c>dapp:manager</c> role and
    /// the <c>nft:mint</c> scope, and reusing custody, value or quest here would widen one of them
    /// to issuing credentials in ArdaNova's name.
    /// </summary>
    private static void ValidateIssuanceCredential(
        AzoaSettings settings,
        string issuanceApiKey,
        string custodyApiKey,
        string valueApiKey,
        string questApiKey,
        bool isProduction)
    {
        if (string.IsNullOrWhiteSpace(issuanceApiKey))
            return;

        if (!IsValidAzoaBaseUrl(settings.BaseUrl, isProduction)
            || !IsValidAzoaCredential(issuanceApiKey, isProduction))
        {
            throw new InvalidOperationException(
                "Azoa:IssuanceApiKey requires an absolute Azoa:BaseUrl (HTTPS in Production) and a "
                + "generated, non-placeholder key of at least 32 bytes in Production.");
        }

        if (!isProduction)
            return;

        var shared = new[] { custodyApiKey, valueApiKey, questApiKey }
            .Any(other => !string.IsNullOrWhiteSpace(other)
                && string.Equals(other, issuanceApiKey, StringComparison.Ordinal));

        if (shared)
        {
            throw new InvalidOperationException(
                "Production AZOA custody, value, quest, and issuance credentials must be different "
                + "scoped API keys.");
        }
    }

    /// <summary>Fail-fast boot guard on the Algorand boundary a host actually resolved.</summary>
    /// <remarks>
    /// <para>The Simulated rule is deliberately unconditional and has no acknowledgement flag,
    /// unlike the Legacy break-glass below it. The Legacy signer moves real value with real keys,
    /// so an operator can own that risk; <c>SimulatedAlgorandService</c> instead FABRICATES asset
    /// identifiers and transaction hashes, and there is no operator choice that makes a fabricated
    /// proof true. A flag here would only heal over a Production node telling members their
    /// credentials are independently verifiable when nothing was ever issued.</para>
    /// <para>An unset <c>Algorand:Provider</c> resolves to Simulated
    /// (<see cref="AlgorandProviderSelection.UnsetProviderDefault"/>), so a Production host that
    /// forgets the key fails here at boot rather than at the first credential.</para>
    /// </remarks>
    private static void ValidateAlgorandProvider(
        AlgorandProvider provider,
        AlgorandSettings algorand,
        AzoaSettings azoa,
        string valueApiKey,
        bool isProduction)
    {
        if (provider == AlgorandProvider.Simulated && isProduction)
        {
            throw new InvalidOperationException(
                "Algorand:Provider resolves to \"Simulated\" in a Production environment (an unset "
                + "value resolves there too). The simulated provider fabricates asset identifiers "
                + "and transaction hashes, so a Production node running it would record and show "
                + "members proofs that do not exist. Set Algorand:Provider to Azoa, or to Legacy "
                + "with Algorand:AllowLegacyCustodialSignerInProduction=true and a real platform "
                + "signer.");
        }

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
