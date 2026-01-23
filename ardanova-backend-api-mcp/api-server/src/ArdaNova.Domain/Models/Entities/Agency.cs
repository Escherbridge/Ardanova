namespace ArdaNova.Domain.Models.Entities;

public class Agency
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? Website { get; private set; }
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Logo { get; private set; }
    public string? Portfolio { get; private set; }
    public string? Specialties { get; private set; }
    public bool IsVerified { get; private set; }
    public decimal? Rating { get; private set; }
    public int ReviewsCount { get; private set; }
    public int ProjectsCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid OwnerId { get; private set; }

    // Navigation properties
    public User Owner { get; private set; } = null!;
    public ICollection<AgencyMember> Members { get; private set; } = new List<AgencyMember>();
    public ICollection<ProjectBid> Bids { get; private set; } = new List<ProjectBid>();
    public ICollection<AgencyReview> Reviews { get; private set; } = new List<AgencyReview>();
    public ICollection<Project> Projects { get; private set; } = new List<Project>();

    private Agency() { }

    public static Agency Create(
        Guid ownerId,
        string name,
        string description,
        string email,
        string? website = null,
        string? phone = null)
    {
        var slug = GenerateSlug(name);
        return new Agency
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Slug = slug,
            Description = description,
            Email = email,
            Website = website,
            Phone = phone,
            IsVerified = false,
            ReviewsCount = 0,
            ProjectsCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public void Update(string name, string description, string email, string? website, string? phone, string? address)
    {
        Name = name;
        Description = description;
        Email = email;
        Website = website;
        Phone = phone;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLogo(string? logo)
    {
        Logo = logo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPortfolio(string? portfolio)
    {
        Portfolio = portfolio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSpecialties(string? specialties)
    {
        Specialties = specialties;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRating(decimal rating)
    {
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementReviewsCount()
    {
        ReviewsCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementProjectsCount()
    {
        ProjectsCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}
