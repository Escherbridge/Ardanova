namespace ArdaNova.Application.DTOs;

public record MembershipCredentialDto
{
    public string Id { get; init; }
    public string ProjectId { get; init; }
    public string UserId { get; init; }
    public string? AssetId { get; init; }
    public string Status { get; init; }
    public bool IsTransferable { get; init; }
    public string GrantedVia { get; init; }
    public string? GrantedByProposalId { get; init; }
    public string? MintTxHash { get; init; }
    public string? RevokeTxHash { get; init; }
    public DateTime? MintedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record GrantMembershipCredentialDto
{
    public required string ProjectId { get; init; }
    public required string UserId { get; init; }
    public required string GrantedVia { get; init; }
    public string? GrantedByProposalId { get; init; }
}

public record UpdateMembershipCredentialMintDto
{
    public required string MintTxHash { get; init; }
}

public record RevokeMembershipCredentialDto
{
    public string? RevokeTxHash { get; init; }
}
