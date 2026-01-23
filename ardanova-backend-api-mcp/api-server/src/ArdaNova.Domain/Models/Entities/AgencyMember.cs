namespace ArdaNova.Domain.Models.Entities;

public class AgencyMember
{
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public DateTime JoinedAt { get; private set; }

    // Navigation properties
    public Agency Agency { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private AgencyMember() { }

    public static AgencyMember Create(Guid agencyId, Guid userId, string role)
    {
        return new AgencyMember
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void ChangeRole(string role)
    {
        Role = role;
    }
}
