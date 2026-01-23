namespace ArdaNova.Domain.Models.Entities;

public class VerificationToken
{
    public string Identifier { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public DateTime Expires { get; private set; }

    private VerificationToken() { }

    public static VerificationToken Create(string identifier, string token, DateTime expires)
    {
        return new VerificationToken
        {
            Identifier = identifier,
            Token = token,
            Expires = expires
        };
    }

    public bool IsExpired() => DateTime.UtcNow > Expires;
}
