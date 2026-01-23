namespace ArdaNova.Infrastructure.Data;

using ArdaNova.Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for ArdaNova.
///
/// IMPORTANT: Prisma (in ardanova-client) owns the database schema and migrations.
/// This DbContext is read/write only - it does NOT run migrations.
///
/// When Prisma schema changes:
/// 1. Update prisma/schema.prisma in ardanova-client
/// 2. Run: npx prisma migrate dev
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

    // Agency entities
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<AgencyMember> AgencyMembers => Set<AgencyMember>();
    public DbSet<ProjectBid> ProjectBids => Set<ProjectBid>();
    public DbSet<AgencyReview> AgencyReviews => Set<AgencyReview>();

    // Business entities
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
    public DbSet<BusinessAnalytics> BusinessAnalytics => Set<BusinessAnalytics>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Bio).HasColumnType("text");
            entity.HasMany(e => e.Accounts).WithOne(a => a.User).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Sessions).WithOne(s => s.User).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Skills).WithOne(s => s.User).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Experiences).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Provider, e.ProviderAccountId }).IsUnique();
            entity.Property(e => e.RefreshToken).HasColumnType("text");
            entity.Property(e => e.AccessToken).HasColumnType("text");
            entity.Property(e => e.IdToken).HasColumnType("text");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
        });

        modelBuilder.Entity<VerificationToken>(entity =>
        {
            entity.HasKey(e => new { e.Identifier, e.Token });
            entity.HasIndex(e => e.Token).IsUnique();
        });

        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Skill }).IsUnique();
        });

        modelBuilder.Entity<UserExperience>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
        });

        // Project configurations
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ProblemStatement).HasColumnType("text");
            entity.Property(e => e.Solution).HasColumnType("text");
            entity.Property(e => e.Tags).HasColumnType("text");
            entity.Property(e => e.Images).HasColumnType("text");
            entity.Property(e => e.Videos).HasColumnType("text");
            entity.Property(e => e.Documents).HasColumnType("text");
            entity.Property(e => e.TargetAudience).HasColumnType("text");
            entity.Property(e => e.ExpectedImpact).HasColumnType("text");
            entity.Property(e => e.Timeline).HasColumnType("text");
            entity.Property(e => e.FundingGoal).HasPrecision(10, 2);
            entity.Property(e => e.CurrentFunding).HasPrecision(10, 2);
            entity.HasOne(e => e.CreatedBy).WithMany(u => u.Projects).HasForeignKey(e => e.CreatedById);
            entity.HasOne(e => e.AssignedAgency).WithMany(a => a.Projects).HasForeignKey(e => e.AssignedAgencyId);
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.HasOne(e => e.Project).WithMany(p => p.Tasks).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AssignedTo).WithMany(u => u.AssignedTasks).HasForeignKey(e => e.AssignedToId);
        });

        modelBuilder.Entity<ProjectTaskDependency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TaskId, e.DependsOnId }).IsUnique();
            entity.HasOne(e => e.Task).WithMany(t => t.Dependencies).HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DependsOn).WithMany(t => t.Dependents).HasForeignKey(e => e.DependsOnId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProjectResource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.EstimatedCost).HasPrecision(10, 2);
            entity.HasOne(e => e.Project).WithMany(p => p.Resources).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.HasOne(e => e.Project).WithMany(p => p.Milestones).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectSupport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.UserId, e.SupportType }).IsUnique();
            entity.Property(e => e.MonthlyAmount).HasPrecision(10, 2);
            entity.Property(e => e.Message).HasColumnType("text");
            entity.HasOne(e => e.Project).WithMany(p => p.Supports).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.ProjectSupports).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.UserId, e.RoleTitle }).IsUnique();
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.Skills).HasColumnType("text");
            entity.Property(e => e.Experience).HasColumnType("text");
            entity.Property(e => e.ReviewMessage).HasColumnType("text");
            entity.HasOne(e => e.Project).WithMany(p => p.Applications).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.ProjectApplications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.HasOne(e => e.Project).WithMany(p => p.Comments).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.ProjectComments).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Parent).WithMany(c => c.Replies).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProjectUpdate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.Images).HasColumnType("text");
            entity.HasOne(e => e.Project).WithMany(p => p.Updates).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.ProjectUpdates).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectEquity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.UserId }).IsUnique();
            entity.Property(e => e.SharePercent).HasPrecision(5, 4);
            entity.Property(e => e.InvestmentAmount).HasPrecision(10, 2);
            entity.HasOne(e => e.Project).WithMany(p => p.Equity).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.ProjectEquity).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Agency configurations
        modelBuilder.Entity<Agency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.OwnerId).IsUnique();
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.Portfolio).HasColumnType("text");
            entity.Property(e => e.Specialties).HasColumnType("text");
            entity.Property(e => e.Rating).HasPrecision(3, 2);
            entity.HasOne(e => e.Owner).WithOne(u => u.Agency).HasForeignKey<Agency>(e => e.OwnerId);
        });

        modelBuilder.Entity<AgencyMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AgencyId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Agency).WithMany(a => a.Members).HasForeignKey(e => e.AgencyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.AgencyMembers).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectBid>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.AgencyId }).IsUnique();
            entity.Property(e => e.Proposal).HasColumnType("text");
            entity.Property(e => e.Timeline).HasColumnType("text");
            entity.Property(e => e.Deliverables).HasColumnType("text");
            entity.Property(e => e.Budget).HasPrecision(10, 2);
            entity.HasOne(e => e.Project).WithMany(p => p.Bids).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Agency).WithMany(a => a.Bids).HasForeignKey(e => e.AgencyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.Bids).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgencyReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AgencyId, e.UserId, e.ProjectId }).IsUnique();
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.HasOne(e => e.Agency).WithMany(a => a.Reviews).HasForeignKey(e => e.AgencyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Reviewer).WithMany(u => u.GivenReviews).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Business configurations
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.HasOne(e => e.Owner).WithMany(u => u.Businesses).HasForeignKey(e => e.OwnerId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).HasColumnType("text");
            entity.HasOne(e => e.Business).WithMany(b => b.Customers).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.Customers).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Cost).HasPrecision(10, 2);
            entity.HasOne(e => e.Business).WithMany(b => b.Products).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.Products).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.Tax).HasPrecision(10, 2);
            entity.Property(e => e.Discount).HasPrecision(10, 2);
            entity.Property(e => e.Total).HasPrecision(10, 2);
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.HasOne(e => e.Business).WithMany(b => b.Invoices).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Customer).WithMany(c => c.Invoices).HasForeignKey(e => e.CustomerId);
            entity.HasOne(e => e.User).WithMany(u => u.Invoices).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Total).HasPrecision(10, 2);
            entity.Property(e => e.Tax).HasPrecision(10, 2);
            entity.Property(e => e.Discount).HasPrecision(10, 2);
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.HasOne(e => e.Business).WithMany(b => b.Sales).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Customer).WithMany(c => c.Sales).HasForeignKey(e => e.CustomerId);
            entity.HasOne(e => e.User).WithMany(u => u.Sales).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Total).HasPrecision(10, 2);
            entity.HasOne(e => e.Sale).WithMany(s => s.Items).HasForeignKey(e => e.SaleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(p => p.SaleItems).HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.HasOne(e => e.Business).WithMany(b => b.Inventory).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithOne(p => p.InventoryItem).HasForeignKey<InventoryItem>(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.InventoryItems).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<MarketingCampaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.MediaUrls).HasColumnType("text");
            entity.HasOne(e => e.Business).WithMany(b => b.Campaigns).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.MarketingCampaigns).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<BusinessAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BusinessId, e.Date }).IsUnique();
            entity.Property(e => e.Revenue).HasPrecision(10, 2);
            entity.Property(e => e.Expenses).HasPrecision(10, 2);
            entity.Property(e => e.Profit).HasPrecision(10, 2);
            entity.HasOne(e => e.Business).WithMany(b => b.Analytics).HasForeignKey(e => e.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });

        // Wallet configurations
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Address }).IsUnique();
            entity.Property(e => e.Provider).HasConversion<string>();
            entity.HasOne(e => e.User).WithMany(u => u.Wallets).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Task Escrow configurations
        modelBuilder.Entity<TaskEscrow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskId).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Amount).HasPrecision(18, 8);
            entity.HasOne(e => e.Task).WithOne(t => t.Escrow).HasForeignKey<TaskEscrow>(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Funder).WithMany(u => u.FundedEscrows).HasForeignKey(e => e.FunderId).OnDelete(DeleteBehavior.NoAction);
        });

        // User Streak configurations
        modelBuilder.Entity<UserStreak>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.StreakType }).IsUnique();
            entity.Property(e => e.StreakType).HasConversion<string>();
            entity.HasOne(e => e.User).WithMany(u => u.Streaks).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Referral configurations
        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReferralCode).IsUnique();
            entity.HasIndex(e => new { e.ReferrerId, e.ReferredId }).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.TokenRewarded).HasPrecision(18, 8);
            entity.HasOne(e => e.Referrer).WithMany(u => u.Referrals).HasForeignKey(e => e.ReferrerId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Referred).WithMany(u => u.ReferredBy).HasForeignKey(e => e.ReferredId).OnDelete(DeleteBehavior.NoAction);
        });

        // Notification configurations
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.Data).HasColumnType("text");
            entity.HasOne(e => e.User).WithMany(u => u.Notifications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Activity configurations
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Action).HasColumnType("text");
            entity.HasOne(e => e.User).WithMany(u => u.Activities).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Project).WithMany(p => p.Activities).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        // Delegated Vote configurations
        modelBuilder.Entity<DelegatedVote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.DelegatorId, e.TokenId }).IsUnique();
            entity.Property(e => e.Amount).HasPrecision(18, 8);
            entity.HasOne(e => e.Project).WithMany(p => p.DelegatedVotes).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Delegator).WithMany(u => u.DelegatedVotesGiven).HasForeignKey(e => e.DelegatorId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Delegatee).WithMany(u => u.DelegatedVotesReceived).HasForeignKey(e => e.DelegateeId).OnDelete(DeleteBehavior.NoAction);
        });

        // Token Swap configurations
        modelBuilder.Entity<TokenSwap>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.FromAmount).HasPrecision(18, 8);
            entity.Property(e => e.ToAmount).HasPrecision(18, 8);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 8);
            entity.Property(e => e.Fee).HasPrecision(18, 8);
            entity.HasOne(e => e.User).WithMany(u => u.TokenSwaps).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Liquidity Pool configurations
        modelBuilder.Entity<LiquidityPool>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Token1Id, e.Token2Id }).IsUnique();
            entity.Property(e => e.Reserve1).HasPrecision(18, 8);
            entity.Property(e => e.Reserve2).HasPrecision(18, 8);
            entity.Property(e => e.TotalShares).HasPrecision(18, 8);
            entity.Property(e => e.FeePercent).HasPrecision(5, 4);
        });

        // Liquidity Provider configurations
        modelBuilder.Entity<LiquidityProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PoolId, e.UserId }).IsUnique();
            entity.Property(e => e.Token1In).HasPrecision(18, 8);
            entity.Property(e => e.Token2In).HasPrecision(18, 8);
            entity.Property(e => e.Shares).HasPrecision(18, 8);
            entity.HasOne(e => e.Pool).WithMany(p => p.Providers).HasForeignKey(e => e.PoolId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany(u => u.LiquidityProvisions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
