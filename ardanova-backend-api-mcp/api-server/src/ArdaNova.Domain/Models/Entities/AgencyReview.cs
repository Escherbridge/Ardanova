namespace ArdaNova.Domain.Models.Entities;

public class AgencyReview
{
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Agency Agency { get; private set; } = null!;
    public User Reviewer { get; private set; } = null!;

    private AgencyReview() { }

    public static AgencyReview Create(
        Guid agencyId,
        Guid userId,
        int rating,
        string? comment = null,
        Guid? projectId = null)
    {
        return new AgencyReview
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            UserId = userId,
            ProjectId = projectId,
            Rating = Math.Clamp(rating, 1, 5),
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateReview(int rating, string? comment)
    {
        Rating = Math.Clamp(rating, 1, 5);
        Comment = comment;
    }
}
