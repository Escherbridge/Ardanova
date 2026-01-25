namespace ArdaNova.Infrastructure.Data;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Conventions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for ArdaNova.
///
/// IMPORTANT: Prisma (in ardanova-client) owns the database schema and migrations.
/// This DbContext is read/write only - it does NOT run migrations.
///
/// When Prisma schema changes:
/// 1. Update prisma/schema.prisma in ardanova-client
/// 2. Run: npx prisma migrate dev or npx prisma db push
/// 3. Update corresponding .NET entities in ArdaNova.Domain to match
/// </summary>
public class ArdaNovaDbContext : DbContext
{
    public ArdaNovaDbContext(DbContextOptions<ArdaNovaDbContext> options) : base(options)
    {
    }

    // User entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<UserExperience> UserExperiences => Set<UserExperience>();

    // Project entities
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<ProjectTaskDependency> ProjectTaskDependencies => Set<ProjectTaskDependency>();
    public DbSet<ProjectResource> ProjectResources => Set<ProjectResource>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<ProjectSupport> ProjectSupports => Set<ProjectSupport>();
    public DbSet<ProjectApplication> ProjectApplications => Set<ProjectApplication>();
    public DbSet<ProjectComment> ProjectComments => Set<ProjectComment>();
    public DbSet<ProjectUpdate> ProjectUpdates => Set<ProjectUpdate>();
    public DbSet<ProjectEquity> ProjectEquities => Set<ProjectEquity>();

    // Guild entities
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<GuildReview> GuildReviews => Set<GuildReview>();
    public DbSet<ProjectBid> ProjectBids => Set<ProjectBid>();

    // Business entities
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();

    // Wallet entities
    public DbSet<Wallet> Wallets => Set<Wallet>();

    // Escrow entities
    public DbSet<TaskEscrow> TaskEscrows => Set<TaskEscrow>();

    // Gamification extended entities
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();
    public DbSet<Referral> Referrals => Set<Referral>();

    // Notification & Activity entities
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Activity> Activities => Set<Activity>();

    // Governance entities
    public DbSet<DelegatedVote> DelegatedVotes => Set<DelegatedVote>();

    // Exchange entities
    public DbSet<TokenSwap> TokenSwaps => Set<TokenSwap>();
    public DbSet<LiquidityPool> LiquidityPools => Set<LiquidityPool>();
    public DbSet<LiquidityProvider> LiquidityProviders => Set<LiquidityProvider>();
    public DbSet<XPEvent> XPEvents => Set<XPEvent>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<Leaderboard> Leaderboards => Set<Leaderboard>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
    public DbSet<Roadmap> Roadmaps => Set<Roadmap>();
    public DbSet<RoadmapPhase> RoadmapPhases => Set<RoadmapPhase>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<ProductBacklogItem> ProductBacklogItems => Set<ProductBacklogItem>();
    public DbSet<BacklogItem> BacklogItems => Set<BacklogItem>();
    public DbSet<SprintItem> SprintItems => Set<SprintItem>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskCompensation> TaskCompensations => Set<TaskCompensation>();
    public DbSet<TaskSubmission> TaskSubmissions => Set<TaskSubmission>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<ProposalExecution> ProposalExecutions => Set<ProposalExecution>();
    public DbSet<ProjectToken> ProjectTokens => Set<ProjectToken>();
    public DbSet<Treasury> Treasuries => Set<Treasury>();
    public DbSet<TreasuryTransaction> TreasuryTransactions => Set<TreasuryTransaction>();
    public DbSet<TokenHolder> TokenHolders => Set<TokenHolder>();
    public DbSet<TokenVesting> TokenVestings => Set<TokenVesting>();
    public DbSet<ICO> ICOs => Set<ICO>();
    public DbSet<ICOContribution> ICOContributions => Set<ICOContribution>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ShopAnalytics> ShopAnalytics => Set<ShopAnalytics>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All enums stored as strings in the database
        modelBuilder.ApplyEnumStringConvention();

        // Composite indexes and other generated configurations
        modelBuilder.ApplyGeneratedConfigurations();
    }
}
