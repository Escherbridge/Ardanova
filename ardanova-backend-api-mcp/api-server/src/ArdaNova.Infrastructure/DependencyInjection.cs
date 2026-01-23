namespace ArdaNova.Infrastructure;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using ArdaNova.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get connection string - prefer DATABASE_URL env var, fallback to config
        var connectionString = GetConnectionString(configuration);

        // DbContext - Note: Prisma manages schema/migrations, EF Core only reads/writes
        services.AddDbContext<ArdaNovaDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

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
