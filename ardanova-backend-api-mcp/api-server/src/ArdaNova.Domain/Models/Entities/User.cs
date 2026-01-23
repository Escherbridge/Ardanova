namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public DateTime? EmailVerified { get; private set; }
    public string? Name { get; private set; }
    public string? Image { get; private set; }
    public string? Bio { get; private set; }
    public string? Location { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public string? LinkedIn { get; private set; }
    public string? Twitter { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserType UserType { get; private set; }

    public bool IsVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<Account> Accounts { get; private set; } = new List<Account>();
    public ICollection<Session> Sessions { get; private set; } = new List<Session>();
    public ICollection<UserSkill> Skills { get; private set; } = new List<UserSkill>();
    public ICollection<UserExperience> Experiences { get; private set; } = new List<UserExperience>();
    public ICollection<Project> Projects { get; private set; } = new List<Project>();
    public ICollection<ProjectSupport> ProjectSupports { get; private set; } = new List<ProjectSupport>();
    public ICollection<ProjectApplication> ProjectApplications { get; private set; } = new List<ProjectApplication>();
    public ICollection<ProjectComment> ProjectComments { get; private set; } = new List<ProjectComment>();
    public ICollection<ProjectUpdate> ProjectUpdates { get; private set; } = new List<ProjectUpdate>();
    public ICollection<ProjectTask> AssignedTasks { get; private set; } = new List<ProjectTask>();
    public Agency? Agency { get; private set; }
    public ICollection<AgencyMember> AgencyMembers { get; private set; } = new List<AgencyMember>();
    public ICollection<ProjectBid> Bids { get; private set; } = new List<ProjectBid>();
    public ICollection<AgencyReview> GivenReviews { get; private set; } = new List<AgencyReview>();
    public ICollection<ProjectEquity> ProjectEquity { get; private set; } = new List<ProjectEquity>();
    public ICollection<Business> Businesses { get; private set; } = new List<Business>();
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();
    public ICollection<Customer> Customers { get; private set; } = new List<Customer>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<Sale> Sales { get; private set; } = new List<Sale>();
    public ICollection<InventoryItem> InventoryItems { get; private set; } = new List<InventoryItem>();
    public ICollection<MarketingCampaign> MarketingCampaigns { get; private set; } = new List<MarketingCampaign>();

    // Wallet relations
    public ICollection<Wallet> Wallets { get; private set; } = new List<Wallet>();

    // Task Escrow relations
    public ICollection<TaskEscrow> FundedEscrows { get; private set; } = new List<TaskEscrow>();

    // Gamification extended relations
    public ICollection<UserStreak> Streaks { get; private set; } = new List<UserStreak>();
    public ICollection<Referral> Referrals { get; private set; } = new List<Referral>();
    public ICollection<Referral> ReferredBy { get; private set; } = new List<Referral>();

    // Notification & Activity relations
    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();
    public ICollection<Activity> Activities { get; private set; } = new List<Activity>();

    // Governance relations
    public ICollection<DelegatedVote> DelegatedVotesGiven { get; private set; } = new List<DelegatedVote>();
    public ICollection<DelegatedVote> DelegatedVotesReceived { get; private set; } = new List<DelegatedVote>();

    // Exchange relations
    public ICollection<TokenSwap> TokenSwaps { get; private set; } = new List<TokenSwap>();
    public ICollection<LiquidityProvider> LiquidityProvisions { get; private set; } = new List<LiquidityProvider>();

    private User() { }

    public static User Create(
        string email,
        UserRole role = UserRole.INDIVIDUAL,
        UserType userType = UserType.INNOVATOR,
        string? name = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Role = role,
            UserType = userType,
            Name = name,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string? name, string? bio, string? location, string? phone, string? website)
    {
        Name = name;
        Bio = bio;
        Location = location;
        Phone = phone;
        Website = website;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSocialLinks(string? linkedIn, string? twitter)
    {
        LinkedIn = linkedIn;
        Twitter = twitter;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImage(string? image)
    {
        Image = image;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        IsVerified = true;
        EmailVerified = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeUserType(UserType userType)
    {
        UserType = userType;
        UpdatedAt = DateTime.UtcNow;
    }
}
