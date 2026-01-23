namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class Project
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string ProblemStatement { get; private set; } = null!;
    public string Solution { get; private set; } = null!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectCategory Category { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectStatus Status { get; private set; }

    public decimal? FundingGoal { get; private set; }
    public decimal CurrentFunding { get; private set; }
    public int SupportersCount { get; private set; }
    public int VotesCount { get; private set; }
    public int ViewsCount { get; private set; }
    public bool Featured { get; private set; }
    public string? Tags { get; private set; }
    public string? Images { get; private set; }
    public string? Videos { get; private set; }
    public string? Documents { get; private set; }
    public string? TargetAudience { get; private set; }
    public string? ExpectedImpact { get; private set; }
    public string? Timeline { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? FundedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid CreatedById { get; private set; }
    public Guid? AssignedAgencyId { get; private set; }

    // Navigation properties
    public User CreatedBy { get; private set; } = null!;
    public Agency? AssignedAgency { get; private set; }
    public ICollection<ProjectTask> Tasks { get; private set; } = new List<ProjectTask>();
    public ICollection<ProjectResource> Resources { get; private set; } = new List<ProjectResource>();
    public ICollection<ProjectMilestone> Milestones { get; private set; } = new List<ProjectMilestone>();
    public ICollection<ProjectSupport> Supports { get; private set; } = new List<ProjectSupport>();
    public ICollection<ProjectApplication> Applications { get; private set; } = new List<ProjectApplication>();
    public ICollection<ProjectComment> Comments { get; private set; } = new List<ProjectComment>();
    public ICollection<ProjectUpdate> Updates { get; private set; } = new List<ProjectUpdate>();
    public ICollection<ProjectBid> Bids { get; private set; } = new List<ProjectBid>();
    public ICollection<ProjectEquity> Equity { get; private set; } = new List<ProjectEquity>();
    public ICollection<Activity> Activities { get; private set; } = new List<Activity>();
    public ICollection<DelegatedVote> DelegatedVotes { get; private set; } = new List<DelegatedVote>();

    private Project() { }

    public static Project Create(
        Guid createdById,
        string title,
        string description,
        string problemStatement,
        string solution,
        ProjectCategory category,
        decimal? fundingGoal = null)
    {
        var slug = GenerateSlug(title);
        return new Project
        {
            Id = Guid.NewGuid(),
            CreatedById = createdById,
            Title = title,
            Slug = slug,
            Description = description,
            ProblemStatement = problemStatement,
            Solution = solution,
            Category = category,
            Status = ProjectStatus.DRAFT,
            FundingGoal = fundingGoal,
            CurrentFunding = 0,
            SupportersCount = 0,
            VotesCount = 0,
            ViewsCount = 0,
            Featured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public void Update(string title, string description, string problemStatement, string solution, ProjectCategory category)
    {
        Title = title;
        Description = description;
        ProblemStatement = problemStatement;
        Solution = solution;
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMedia(string? images, string? videos, string? documents)
    {
        Images = images;
        Videos = videos;
        Documents = documents;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDetails(string? tags, string? targetAudience, string? expectedImpact, string? timeline)
    {
        Tags = tags;
        TargetAudience = targetAudience;
        ExpectedImpact = expectedImpact;
        Timeline = timeline;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        Status = ProjectStatus.PUBLISHED;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SeekSupport()
    {
        Status = ProjectStatus.SEEKING_SUPPORT;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFunded()
    {
        Status = ProjectStatus.FUNDED;
        FundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartProgress()
    {
        Status = ProjectStatus.IN_PROGRESS;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ProjectStatus.COMPLETED;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = ProjectStatus.CANCELLED;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.PUBLISHED:
                Publish();
                break;
            case ProjectStatus.SEEKING_SUPPORT:
                SeekSupport();
                break;
            case ProjectStatus.FUNDED:
                MarkFunded();
                break;
            case ProjectStatus.IN_PROGRESS:
                StartProgress();
                break;
            case ProjectStatus.COMPLETED:
                Complete();
                break;
            case ProjectStatus.CANCELLED:
                Cancel();
                break;
            default:
                Status = status;
                UpdatedAt = DateTime.UtcNow;
                break;
        }
    }

    public void SetFeatured(bool featured)
    {
        Featured = featured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddFunding(decimal amount)
    {
        CurrentFunding += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViews()
    {
        ViewsCount++;
    }

    public void IncrementVotes()
    {
        VotesCount++;
    }

    public void IncrementSupporters()
    {
        SupportersCount++;
    }

    public void AssignAgency(Guid agencyId)
    {
        AssignedAgencyId = agencyId;
        UpdatedAt = DateTime.UtcNow;
    }
}
