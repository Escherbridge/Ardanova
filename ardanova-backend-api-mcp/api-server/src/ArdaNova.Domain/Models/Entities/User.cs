using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(email), IsUnique = true)]
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

    [Column(TypeName = "text")]
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
    [Precision(18, 8)]
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

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<ProjectSupport> ProjectSupports { get; set; } = new List<ProjectSupport>();

    public virtual ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectUpdate> ProjectUpdates { get; set; } = new List<ProjectUpdate>();

    public virtual ICollection<Epic> Epics { get; set; } = new List<Epic>();

    public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

    public virtual ICollection<Feature> Features { get; set; } = new List<Feature>();

    public virtual ICollection<ProductBacklogItem> ProductBacklogItems { get; set; } = new List<ProductBacklogItem>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual ICollection<Guild> Guilds { get; set; } = new List<Guild>();

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

    public virtual ICollection<GuildReview> GuildReviews { get; set; } = new List<GuildReview>();

    public virtual ICollection<GuildUpdate> GuildUpdates { get; set; } = new List<GuildUpdate>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<ProjectEquity> ProjectEquities { get; set; } = new List<ProjectEquity>();

    public virtual ICollection<ShareHolder> ShareHolders { get; set; } = new List<ShareHolder>();

    public virtual ICollection<FundraisingContribution> FundraisingContributions { get; set; } = new List<FundraisingContribution>();

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

    public virtual ICollection<TaskEscrow> TaskEscrows { get; set; } = new List<TaskEscrow>();

    public virtual ICollection<ShareSwap> ShareSwaps { get; set; } = new List<ShareSwap>();

    public virtual ICollection<LiquidityProvider> LiquidityProviders { get; set; } = new List<LiquidityProvider>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    public virtual ICollection<PostComment> PostComments { get; set; } = new List<PostComment>();

    public virtual ICollection<PostBookmark> PostBookmarks { get; set; } = new List<PostBookmark>();

    public virtual ICollection<PostShare> PostShares { get; set; } = new List<PostShare>();

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<EventAttendee> EventAttendees { get; set; } = new List<EventAttendee>();

    public virtual ICollection<EventCoHost> EventCoHosts { get; set; } = new List<EventCoHost>();

    public virtual ICollection<EventReminder> EventReminders { get; set; } = new List<EventReminder>();

    public virtual ICollection<ProjectFollow> ProjectFollows { get; set; } = new List<ProjectFollow>();

    public virtual ICollection<GuildFollow> GuildFollows { get; set; } = new List<GuildFollow>();

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual ICollection<OpportunityApplication> OpportunityApplications { get; set; } = new List<OpportunityApplication>();

    public virtual ICollection<OpportunityUpdate> OpportunityUpdates { get; set; } = new List<OpportunityUpdate>();

    public virtual ICollection<OpportunityComment> OpportunityComments { get; set; } = new List<OpportunityComment>();

    public virtual ICollection<OpportunityBid> OpportunityBids { get; set; } = new List<OpportunityBid>();

    public virtual ICollection<Referral> ReferralsAsReferrer { get; set; } = new List<Referral>();

    public virtual ICollection<Referral> ReferralsAsReferred { get; set; } = new List<Referral>();

    public virtual ICollection<ProjectInvitation> ProjectInvitationsAsInvitedBy { get; set; } = new List<ProjectInvitation>();

    public virtual ICollection<ProjectInvitation> ProjectInvitationsAsInvitedUser { get; set; } = new List<ProjectInvitation>();

    public virtual ICollection<ProjectMembershipRequest> ProjectMembershipRequestsAsUser { get; set; } = new List<ProjectMembershipRequest>();

    public virtual ICollection<ProjectMembershipRequest> ProjectMembershipRequestsAsReviewedBy { get; set; } = new List<ProjectMembershipRequest>();

    public virtual ICollection<TaskSubmission> TaskSubmissionsAsSubmittedBy { get; set; } = new List<TaskSubmission>();

    public virtual ICollection<TaskSubmission> TaskSubmissionsAsReviewedBy { get; set; } = new List<TaskSubmission>();

    public virtual ICollection<DelegatedVote> DelegatedVotesAsDelegator { get; set; } = new List<DelegatedVote>();

    public virtual ICollection<DelegatedVote> DelegatedVotesAsDelegatee { get; set; } = new List<DelegatedVote>();

    public virtual ICollection<GuildInvitation> GuildInvitationsAsInvitedBy { get; set; } = new List<GuildInvitation>();

    public virtual ICollection<GuildInvitation> GuildInvitationsAsInvitedUser { get; set; } = new List<GuildInvitation>();

    public virtual ICollection<GuildApplication> GuildApplicationsAsUser { get; set; } = new List<GuildApplication>();

    public virtual ICollection<GuildApplication> GuildApplicationsAsReviewedBy { get; set; } = new List<GuildApplication>();

    public virtual ICollection<ChatMessage> ChatMessagesAsUserTo { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatMessage> ChatMessagesAsUserFrom { get; set; } = new List<ChatMessage>();

    public virtual ICollection<UserFollow> UserFollowsAsFollower { get; set; } = new List<UserFollow>();

    public virtual ICollection<UserFollow> UserFollowsAsFollowing { get; set; } = new List<UserFollow>();

}
