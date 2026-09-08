namespace ArdaNova.Application.DTOs;

/// <summary>Server-owned result of accepting a bid into the local commerce workflow.</summary>
public record TaskCommerceAcceptanceDto
{
    public required string BidId { get; init; }
    public required string TaskId { get; init; }
    public required string AgreementId { get; init; }
    public required string CommerceUrl { get; init; }
}

/// <summary>Actor-authorized, read-only local state for a task commerce agreement.</summary>
public record TaskCommerceViewDto
{
    public required string TaskId { get; init; }
    public required string AgreementId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string AssetCode { get; init; }
    public required decimal AwardAmount { get; init; }
    public required int Scale { get; init; }
    public required string AgreementStatus { get; init; }
    public required string EscrowStatus { get; init; }

    /// <summary>
    /// When the worker's own window ends. Null until the owner has approved the completed
    /// work, because the window only opens then.
    /// </summary>
    /// <remarks>
    /// This is what a worker must be TOLD, not just what the row records: the award is theirs
    /// and they can move it from this date. It is restricted until then so they are not taxed
    /// on it before they can realise it. It is never a refund or chargeback window — these
    /// units come from the platform reserve and were never bought with a card, so there is
    /// nothing to reverse.
    /// </remarks>
    public DateTime? WorkerHoldReleasesAt { get; init; }

    /// <summary>True while the award is held and cannot yet be moved.</summary>
    public bool IsRestricted { get; init; }
}
