namespace ArdaNova.Domain.Models.Entities;

public class Session
{
    public Guid Id { get; private set; }
    public string SessionToken { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime Expires { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private Session() { }

    public static Session Create(Guid userId, string sessionToken, DateTime expires)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = sessionToken,
            Expires = expires
        };
    }

    public void ExtendSession(DateTime newExpires)
    {
        Expires = newExpires;
    }

    public bool IsExpired() => DateTime.UtcNow > Expires;
}
