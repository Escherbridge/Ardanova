namespace ArdaNova.Application.DTOs;

/// <summary>Creates a server-owned funding intent for a specific project token configuration.</summary>
public sealed record CreateFundingIntentDto
{
    public required string ProjectTokenConfigId { get; init; }
    public required string Amount { get; init; }
    public required string DisclosureVersion { get; init; }

    /// <summary>
    /// The sponsor's own choice to be named against this sponsorship on the project's money page.
    /// Deliberately NOT <c>required</c> and deliberately defaulted to false: a caller that says
    /// nothing has consented to nothing, so an omitted field can only ever mean "leave me unnamed".
    /// </summary>
    public bool DisplayNameConsent { get; init; }
}

/// <summary>Opaque checkout handoff for an actor-owned funding intent.</summary>
public sealed record FundingCheckoutDto
{
    public required string IntentId { get; init; }
    public required string CheckoutUrl { get; init; }
}

/// <summary>Actor-scoped durable funding state; redirects and query parameters are not inputs.</summary>
public sealed record FundingIntentStatusDto
{
    public required string IntentId { get; init; }
    public required string ProjectTokenConfigId { get; init; }
    public required string CurrencyCode { get; init; }
    public required string Amount { get; init; }
    public required string Status { get; init; }
    public DateTime? PaymentVerifiedAt { get; init; }

    /// <summary>
    /// When the freshly issued stake becomes the sponsor's to move, as the server recorded it -
    /// the node's own release time when it gave one, otherwise the window ArdaNova declared. Null
    /// only while no window has been written, which no settled sponsorship can be.
    /// </summary>
    public DateTime? ReleaseNotBefore { get; init; }

    /// <summary>
    /// The consumer-safe sentence the server already wrote for a sponsorship that is holding,
    /// failed, rejected or cancelled. Null while nothing has gone wrong.
    /// </summary>
    public string? HoldingReasonDetail { get; init; }
}
