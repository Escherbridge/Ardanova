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
    /// It is derived from <c>issuedThroughNodeAt</c> — a column of its OWN — and never from
    /// <c>mintTxHash</c> or <c>assetId</c>. Neither of those can carry the claim: the removed
    /// address-based mint wrote BOTH of them from <c>MintSoulboundASAAsync(_platformAddress, …)</c>,
    /// which put a real asset in the PLATFORM's hands and proves nothing about the member, and
    /// the shipped simulated provider fabricated an asset id outright. Rows predating the node
    /// path therefore still carry both, and deriving the flag from them would read "Verified" over
    /// exactly the credentials that were never issued to anyone.
    /// </para>
    /// <para>
    /// <c>issuedThroughNodeAt</c> is written in exactly one place,
    /// <c>MembershipCredentialServices.UpdateMintInfoAsync</c>, and only when the caller states
    /// <see cref="CredentialProofOrigin.IssuanceNode"/> — a REQUIRED field, so a new writer cannot
    /// reach the column without answering which path produced the proof, and the enum's default
    /// value is the one that clears it. A row that predates the column reads null, so the flag is
    /// false without any backfill having to have run. A re-grant of a withdrawn credential clears
    /// it too, so the flag goes back to false until the node issues again.
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

/// <summary>
/// Which path produced a credential's proof. Stated by the caller rather than inferred, because
/// the two paths write the SAME legacy columns and only one of them puts a proof in the member's
/// hands. <see cref="PlatformHeldMint"/> is value 0, so a shape that omits the field entirely —
/// a JSON body, a default-constructed record — resolves to the reading that clears the flag.
/// </summary>
public enum CredentialProofOrigin
{
    /// <summary>
    /// Anything that is not the issuance node: the removed address-based mint, which minted to
    /// the platform's own address, and the simulated provider, which fabricated an asset id.
    /// Records the identifiers and CLEARS <c>issuedThroughNodeAt</c>.
    /// </summary>
    PlatformHeldMint = 0,

    /// <summary>
    /// The credential issuance node — the one path that issues to the MEMBER. The only value
    /// that stamps <c>issuedThroughNodeAt</c>, and so the only one that can make a credential
    /// read as independently verifiable.
    /// </summary>
    IssuanceNode = 1,
}

public record UpdateMembershipCredentialMintDto
{
    public required string MintTxHash { get; init; }
    public string? AssetId { get; init; }
    public string? MetadataUri { get; init; }

    /// <summary>
    /// Which path produced this proof. Required: the compiler refuses a writer that does not say.
    /// </summary>
    public required CredentialProofOrigin Origin { get; init; }
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
