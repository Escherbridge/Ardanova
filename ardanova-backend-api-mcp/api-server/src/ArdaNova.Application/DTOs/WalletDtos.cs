namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record WalletDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Address { get; init; } = null!;
    public WalletProvider Provider { get; init; }
    public string? Label { get; init; }
    public bool IsVerified { get; init; }
    public bool IsPrimary { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateWalletDto
{
    public required Guid UserId { get; init; }
    public required string Address { get; init; }
    public WalletProvider Provider { get; init; } = WalletProvider.PERA;
    public string? Label { get; init; }
    public bool IsPrimary { get; init; } = false;
}

public record UpdateWalletDto
{
    public string? Label { get; init; }
    public bool? IsPrimary { get; init; }
}
