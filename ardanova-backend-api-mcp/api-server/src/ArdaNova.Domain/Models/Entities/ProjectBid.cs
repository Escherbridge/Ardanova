namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class ProjectBid
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Proposal { get; private set; } = null!;
    public string? Timeline { get; private set; }
    public decimal Budget { get; private set; }
    public string? Deliverables { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BidStatus Status { get; private set; }

    public DateTime SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public Agency Agency { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ProjectBid() { }

    public static ProjectBid Create(
        Guid projectId,
        Guid agencyId,
        Guid userId,
        string proposal,
        decimal budget,
        string? timeline = null,
        string? deliverables = null)
    {
        return new ProjectBid
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AgencyId = agencyId,
            UserId = userId,
            Proposal = proposal,
            Budget = budget,
            Timeline = timeline,
            Deliverables = deliverables,
            Status = BidStatus.SUBMITTED,
            SubmittedAt = DateTime.UtcNow
        };
    }

    public void SubmitForReview()
    {
        Status = BidStatus.UNDER_REVIEW;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Accept()
    {
        Status = BidStatus.ACCEPTED;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BidStatus.REJECTED;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Withdraw()
    {
        Status = BidStatus.WITHDRAWN;
    }

    public void UpdateProposal(string proposal, decimal budget, string? timeline, string? deliverables)
    {
        Proposal = proposal;
        Budget = budget;
        Timeline = timeline;
        Deliverables = deliverables;
    }
}
