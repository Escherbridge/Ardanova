namespace ArdaNova.Application.DTOs;

public record MembershipCredentialDto
{
    public string Id { get; init; }
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
    public string UserId { get; init; }
    public string? AssetId { get; init; }
    public string Status { get; init; }
    public bool IsTransferable { get; init; }
    public string? Tier { get; init; }
    public string GrantedVia { get; init; }
    public string? GrantedByProposalId { get; init; }
    public string? MetadataUri { get; init; }
    public string? MintTxHash { get; init; }
    public string? RevokeTxHash { get; init; }
    public DateTime? MintedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// True only when this credential went through the credential ISSUANCE NODE — the one
    /// path that puts a proof in the MEMBER's hands — and the node returned an issuance
    /// reference for it.
    /// <para>
    /// It is derived from <c>mintTxHash</c> and NEVER from <c>assetId</c>. That column is
    /// written in exactly one place, <c>MembershipCredentialServices.UpdateMintInfoAsync</c>,
    /// whose only production caller is <c>CredentialUtilityService.IssueThroughNodeAsync</c>,
    /// where it carries <c>IssuanceReference</c> off the node's own result. AssetId is not
    /// usable for this: the shipped simulated provider fabricated one outright, and the
    /// address-based mint it replaced put a real asset in the PLATFORM's hands, which proves
    /// nothing about the member. A re-grant of a withdrawn credential clears both columns,
    /// so the flag goes back to false until the node issues again.
    /// </para>
    /// </summary>
    public bool IsIndependentlyVerifiable { get; init; }
}

public record GrantMembershipCredentialDto
{
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
    public required string UserId { get; init; }
    public required string GrantedVia { get; init; }
    public string? GrantedByProposalId { get; init; }
}

public record UpdateMembershipCredentialMintDto
{
    public required string MintTxHash { get; init; }
    public string? AssetId { get; init; }
    public string? MetadataUri { get; init; }
}

public record RevokeMembershipCredentialDto
{
    public string? RevokeTxHash { get; init; }
}

public record UpdateCredentialTierDto
{
    public required string Tier { get; init; }
}

public record CredentialEligibilityDto
{
    public bool IsEligible { get; init; }
    public string? Reason { get; init; }
    public string? RequiredTier { get; init; }
}

/// <summary>
/// Credential enriched with on-chain Algorand data.
/// </summary>
public record CredentialWithChainDataDto
{
    /// <summary>All off-chain credential fields</summary>
    public required MembershipCredentialDto Credential { get; init; }

    /// <summary>On-chain ASA state (null when not minted)</summary>
    public AsaInfoDto? AsaInfo { get; init; }

    /// <summary>True if the credential has been minted on-chain (assetId is not null)</summary>
    public bool IsOnChain { get; init; }

    /// <summary>True if on-chain ownership has been confirmed</summary>
    public bool ChainVerified { get; init; }
}

/// <summary>
/// Request body for the check-auto-grant endpoint.
/// </summary>
public record CheckAutoGrantRequestDto
{
    public required string UserId { get; init; }
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
}
