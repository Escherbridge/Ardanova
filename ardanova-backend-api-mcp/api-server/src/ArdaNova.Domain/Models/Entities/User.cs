using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("User")]
public class User
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string email { get; set; } = string.Empty;

    public DateTime? emailVerified { get; set; }

    public string? name { get; set; }

    public string? image { get; set; }

    public string? bio { get; set; }

    public string? location { get; set; }

    public string? phone { get; set; }

    public string? website { get; set; }

    public string? linkedIn { get; set; }

    public string? twitter { get; set; }

    [Required]
    public UserRole role { get; set; }

    [Required]
    public UserType userType { get; set; }

    [Required]
    public bool isVerified { get; set; }

    [Required]
    public int totalXP { get; set; }

    [Required]
    public int level { get; set; }

    [Required]
    public UserTier tier { get; set; }

    [Required]
    public decimal trustScore { get; set; }

    [Required]
    public VerificationLevel verificationLevel { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

    public virtual ICollection<UserExperience> UserExperiences { get; set; } = new List<UserExperience>();

    public virtual ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<XPEvent> XPEvents { get; set; } = new List<XPEvent>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual ICollection<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();

    public virtual ICollection<UserStreak> UserStreaks { get; set; } = new List<UserStreak>();

    public virtual ICollection<Referral> Referrals { get; set; } = new List<Referral>();

    public virtual ICollection<Referral> Referrals1 { get; set; } = new List<Referral>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectSupport> ProjectSupports { get; set; } = new List<ProjectSupport>();

    public virtual ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();

    public virtual ICollection<TaskSubmission> TaskSubmissions1 { get; set; } = new List<TaskSubmission>();

    public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual ICollection<DelegatedVote> DelegatedVotes { get; set; } = new List<DelegatedVote>();

    public virtual ICollection<DelegatedVote> DelegatedVotes1 { get; set; } = new List<DelegatedVote>();

    public virtual ICollection<Guild> Guilds { get; set; } = new List<Guild>();

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

    public virtual ICollection<ProjectBid> ProjectBids { get; set; } = new List<ProjectBid>();

    public virtual ICollection<GuildReview> GuildReviews { get; set; } = new List<GuildReview>();

    public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Invoice> Invoices1 { get; set; } = new List<Invoice>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<Sale> Sales1 { get; set; } = new List<Sale>();

    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

    public virtual ICollection<MarketingCampaign> MarketingCampaigns { get; set; } = new List<MarketingCampaign>();

    public virtual ICollection<ProjectEquity> ProjectEquities { get; set; } = new List<ProjectEquity>();

    public virtual ICollection<TokenHolder> TokenHolders { get; set; } = new List<TokenHolder>();

    public virtual ICollection<ICOContribution> ICOContributions { get; set; } = new List<ICOContribution>();

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

    public virtual ICollection<TaskEscrow> TaskEscrows { get; set; } = new List<TaskEscrow>();

    public virtual ICollection<TokenSwap> TokenSwaps { get; set; } = new List<TokenSwap>();

    public virtual ICollection<LiquidityProvider> LiquidityProviders { get; set; } = new List<LiquidityProvider>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatMessage> ChatMessages1 { get; set; } = new List<ChatMessage>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

}
