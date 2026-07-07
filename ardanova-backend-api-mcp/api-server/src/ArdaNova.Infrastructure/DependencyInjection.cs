namespace ArdaNova.Infrastructure;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Services.Interfaces;
using System.Reflection;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Algorand;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using ArdaNova.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get connection string - prefer DATABASE_URL env var, fallback to config
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

        var mapEnumMethod = typeof(NpgsqlDataSourceBuilder)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "MapEnum" && m.IsGenericMethod);

        foreach (var enumType in enumTypes)
        {
            mapEnumMethod
                .MakeGenericMethod(enumType)
                .Invoke(dataSourceBuilder, new object[] { enumType.Name, translator });
        }

        var dataSource = dataSourceBuilder.Build();

        // DbContext - Note: Prisma manages schema/migrations, EF Core only reads/writes
        services.AddDbContext<ArdaNovaDbContext>(options =>
            options.UseNpgsql(dataSource));

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Entity-specific Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // AZOA shared-node integration (contract §2/§3/§11).
        services.Configure<AzoaSettings>(
            configuration.GetSection(AzoaSettings.SectionName));
        var azoaSettings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        services.AddHttpClient<IAzoaNodeClient, AzoaNodeClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(azoaSettings.BaseUrl))
                client.BaseAddress = new Uri(azoaSettings.BaseUrl);
            // Tenant API key — deploy-time secret (Azoa__TenantApiKey), never committed.
            if (!string.IsNullOrWhiteSpace(azoaSettings.TenantApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", azoaSettings.TenantApiKey);
            client.Timeout = TimeSpan.FromSeconds(
                azoaSettings.TimeoutSeconds > 0 ? azoaSettings.TimeoutSeconds : 30);
        });
        // Application-layer ports onto the node (dependency-inversion seam, like IAlgorandService).
        services.AddScoped<IAzoaAvatarNode, AzoaAvatarNodeAdapter>();
        services.AddScoped<IAzoaQuestNode, AzoaQuestNodeAdapter>();
        services.AddScoped<IAzoaAllocationNode, AzoaAllocationNodeAdapter>();

        // Algorand blockchain service.
        // The IAlgorandService implementation is selected by the Algorand:Provider
        // flag (Legacy | Azoa | Simulated). Default Legacy preserves current
        // behavior; the Azoa-backed adapter is registered by the provider-adapter track.
        // The interface is the seam — every consumer (CredentialUtilityService,
        // tokenomics, …) binds to IAlgorandService and is unaffected by the choice.
        services.Configure<AlgorandSettings>(
            configuration.GetSection(AlgorandSettings.SectionName));

        var algorandProvider = configuration[$"{AlgorandSettings.SectionName}:Provider"];
        if (string.Equals(algorandProvider, "Azoa", StringComparison.OrdinalIgnoreCase))
        {
            // Azoa-backed adapter: routes value moves to the shared/managed AZOA
            // node via IAzoaNodeClient (registered in the AZOA block above). No
            // HttpClient of its own — the node client owns transport.
            services.AddScoped<IAlgorandService, AzoaBackedAlgorandService>();
        }
        else
        {
            // Legacy (default) and Simulated both use the custodial-mnemonic
            // signer; Simulated is driven by AlgorandSettings/network config.
            services.AddHttpClient<IAlgorandService, AlgorandService>();
        }

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        // First try DATABASE_URL environment variable (used by Railway, Heroku, etc.)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(databaseUrl))
        {
            return ConvertPostgresUrlToConnectionString(databaseUrl);
        }

        // Fallback to traditional .NET connection string config
        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured. Set DATABASE_URL environment variable or ConnectionStrings:DefaultConnection in appsettings.json");
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
