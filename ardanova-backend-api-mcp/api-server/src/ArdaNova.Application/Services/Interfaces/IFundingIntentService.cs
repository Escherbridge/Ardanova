namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// Owns durable sponsorship authorization and status reads.
/// </summary>
/// <remarks>
/// There is no payment-provider surface here any more. The node terminates the fiat rail, so
/// ArdaNova never records a card event against a sponsorship and no provider type crosses this
/// seam — the absence of a <c>RecordPayment*</c> method is the control, not a comment about one.
/// </remarks>
public interface IFundingIntentService
{
    /// <summary>Commits an actor-owned sponsorship, locking both exchange legs and the rate before value moves.</summary>
    Task<Result<FundingCheckoutDto>> CreateCheckoutAsync(
        CreateFundingIntentDto request,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>Reads durable funding state only when the actor owns the intent.</summary>
    Task<Result<FundingIntentStatusDto>> GetStatusAsync(
        string intentId,
        string actorId,
        CancellationToken ct = default);
}
