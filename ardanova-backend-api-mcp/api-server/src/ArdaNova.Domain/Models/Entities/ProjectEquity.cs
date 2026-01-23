namespace ArdaNova.Domain.Models.Entities;

public class ProjectEquity
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal SharePercent { get; private set; }
    public decimal InvestmentAmount { get; private set; }
    public DateTime GrantedAt { get; private set; }

    // Navigation properties
    public Project Project { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private ProjectEquity() { }

    public static ProjectEquity Create(
        Guid projectId,
        Guid userId,
        decimal sharePercent,
        decimal investmentAmount)
    {
        return new ProjectEquity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            SharePercent = sharePercent,
            InvestmentAmount = investmentAmount,
            GrantedAt = DateTime.UtcNow
        };
    }

    public void UpdateShare(decimal sharePercent, decimal investmentAmount)
    {
        SharePercent = sharePercent;
        InvestmentAmount = investmentAmount;
    }
}
