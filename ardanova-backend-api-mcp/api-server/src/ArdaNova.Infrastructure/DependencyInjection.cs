namespace ArdaNova.Infrastructure;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
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

        // Map all .NET enums to PostgreSQL native enums (created by Prisma)
        // Prisma uses PascalCase type names and SCREAMING_SNAKE_CASE values
        // INpgsqlNameTranslator.IdentityTranslation preserves the original names
        var translator = new NpgsqlNullNameTranslator();
        dataSourceBuilder.MapEnum<AchievementCategory>("AchievementCategory", translator);
        dataSourceBuilder.MapEnum<AchievementRarity>("AchievementRarity", translator);
        dataSourceBuilder.MapEnum<ActivityType>("ActivityType", translator);
        dataSourceBuilder.MapEnum<ApplicationStatus>("ApplicationStatus", translator);
        dataSourceBuilder.MapEnum<AttendeeStatus>("AttendeeStatus", translator);
        dataSourceBuilder.MapEnum<BacklogItemType>("BacklogItemType", translator);
        dataSourceBuilder.MapEnum<BacklogStatus>("BacklogStatus", translator);
        dataSourceBuilder.MapEnum<BidStatus>("BidStatus", translator);
        dataSourceBuilder.MapEnum<CampaignStatus>("CampaignStatus", translator);
        dataSourceBuilder.MapEnum<CompensationModel>("CompensationModel", translator);
        dataSourceBuilder.MapEnum<ContributionStatus>("ContributionStatus", translator);
        dataSourceBuilder.MapEnum<ConversationRole>("ConversationRole", translator);
        dataSourceBuilder.MapEnum<ConversationType>("ConversationType", translator);
        dataSourceBuilder.MapEnum<EpicStatus>("EpicStatus", translator);
        dataSourceBuilder.MapEnum<EscrowStatus>("EscrowStatus", translator);
        dataSourceBuilder.MapEnum<EventStatus>("EventStatus", translator);
        dataSourceBuilder.MapEnum<EventType>("EventType", translator);
        dataSourceBuilder.MapEnum<EventVisibility>("EventVisibility", translator);
        dataSourceBuilder.MapEnum<ExperienceLevel>("ExperienceLevel", translator);
        dataSourceBuilder.MapEnum<FundraisingStatus>("FundraisingStatus", translator);
        dataSourceBuilder.MapEnum<InvoiceStatus>("InvoiceStatus", translator);
        dataSourceBuilder.MapEnum<LeaderboardCategory>("LeaderboardCategory", translator);
        dataSourceBuilder.MapEnum<LeaderboardPeriod>("LeaderboardPeriod", translator);
        dataSourceBuilder.MapEnum<MessageStatus>("MessageStatus", translator);
        dataSourceBuilder.MapEnum<MimeType>("MimeType", translator);
        dataSourceBuilder.MapEnum<NotificationType>("NotificationType", translator);
        dataSourceBuilder.MapEnum<OpportunityStatus>("OpportunityStatus", translator);
        dataSourceBuilder.MapEnum<OpportunityType>("OpportunityType", translator);
        dataSourceBuilder.MapEnum<PaymentMethod>("PaymentMethod", translator);
        dataSourceBuilder.MapEnum<PBIStatus>("PBIStatus", translator);
        dataSourceBuilder.MapEnum<PBIType>("PBIType", translator);
        dataSourceBuilder.MapEnum<PhaseStatus>("PhaseStatus", translator);
        dataSourceBuilder.MapEnum<Priority>("Priority", translator);
        dataSourceBuilder.MapEnum<ProjectCategory>("ProjectCategory", translator);
        dataSourceBuilder.MapEnum<ProjectRole>("ProjectRole", translator);
        dataSourceBuilder.MapEnum<ProjectStatus>("ProjectStatus", translator);
        dataSourceBuilder.MapEnum<ProposalStatus>("ProposalStatus", translator);
        dataSourceBuilder.MapEnum<ProposalType>("ProposalType", translator);
        dataSourceBuilder.MapEnum<ReferralStatus>("ReferralStatus", translator);
        dataSourceBuilder.MapEnum<RoadmapStatus>("RoadmapStatus", translator);
        dataSourceBuilder.MapEnum<ShopCategory>("ShopCategory", translator);
        dataSourceBuilder.MapEnum<SprintStatus>("SprintStatus", translator);
        dataSourceBuilder.MapEnum<StreakType>("StreakType", translator);
        dataSourceBuilder.MapEnum<SubmissionStatus>("SubmissionStatus", translator);
        dataSourceBuilder.MapEnum<SubscriptionPlan>("SubscriptionPlan", translator);
        dataSourceBuilder.MapEnum<SupportType>("SupportType", translator);
        dataSourceBuilder.MapEnum<SwapStatus>("SwapStatus", translator);
        dataSourceBuilder.MapEnum<TaskPriority>("TaskPriority", translator);
        dataSourceBuilder.MapEnum<TaskStatus>("TaskStatus", translator);
        dataSourceBuilder.MapEnum<TaskType>("TaskType", translator);
        dataSourceBuilder.MapEnum<TransactionType>("TransactionType", translator);
        dataSourceBuilder.MapEnum<UserRole>("UserRole", translator);
        dataSourceBuilder.MapEnum<UserTier>("UserTier", translator);
        dataSourceBuilder.MapEnum<UserType>("UserType", translator);
        dataSourceBuilder.MapEnum<VerificationLevel>("VerificationLevel", translator);
        dataSourceBuilder.MapEnum<VestingFrequency>("VestingFrequency", translator);
        dataSourceBuilder.MapEnum<WalletProvider>("WalletProvider", translator);
        dataSourceBuilder.MapEnum<XPEventType>("XPEventType", translator);

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
