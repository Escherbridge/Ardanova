namespace ArdaNova.Domain.Models.Entities;

public class UserExperience
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Company { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private UserExperience() { }

    public static UserExperience Create(
        Guid userId,
        string title,
        string company,
        DateTime startDate,
        string? description = null,
        DateTime? endDate = null,
        bool isCurrent = false)
    {
        return new UserExperience
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Company = company,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            IsCurrent = isCurrent
        };
    }

    public void Update(string title, string company, string? description, DateTime startDate, DateTime? endDate, bool isCurrent)
    {
        Title = title;
        Company = company;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
    }

    public void MarkAsCurrent()
    {
        IsCurrent = true;
        EndDate = null;
    }

    public void EndPosition(DateTime endDate)
    {
        IsCurrent = false;
        EndDate = endDate;
    }
}
