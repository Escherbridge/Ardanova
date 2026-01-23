namespace ArdaNova.Domain.Models.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Provider { get; private set; } = null!;
    public string ProviderAccountId { get; private set; } = null!;
    public string? RefreshToken { get; private set; }
    public string? AccessToken { get; private set; }
    public int? ExpiresAt { get; private set; }
    public string? TokenType { get; private set; }
    public string? Scope { get; private set; }
    public string? IdToken { get; private set; }
    public string? SessionState { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private Account() { }

    public static Account Create(
        Guid userId,
        string type,
        string provider,
        string providerAccountId,
        string? refreshToken = null,
        string? accessToken = null,
        int? expiresAt = null)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Provider = provider,
            ProviderAccountId = providerAccountId,
            RefreshToken = refreshToken,
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };
    }

    public void UpdateTokens(string? accessToken, string? refreshToken, int? expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }
}
