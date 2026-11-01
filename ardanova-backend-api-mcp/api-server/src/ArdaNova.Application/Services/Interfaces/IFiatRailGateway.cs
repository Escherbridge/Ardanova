namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;

/// <summary>What one add-funds payment needs. The payer is the account id, never a body field.</summary>
public sealed record FiatCheckoutStart(
    Guid AccountId,
    string RequestKey,
    long AmountMinorUnits,
    string ReturnUrl,
    string CancelUrl);

/// <summary>Everything a return page needs to recognise the payment it came back to.</summary>
public sealed record FiatCheckoutSession(
    Guid IntentId,
    string Status,
    string SettlementState,
    string? PaymentUrl,
    bool Resumed,
    long? AmountPaidMinorUnits,
    string? CurrencyCode,
    DateTimeOffset? ExpiresAt);

/// <summary>The two answers a request-key void can give. Wire values; never rename.</summary>
public static class FiatCheckoutVoidOutcomes
{
    /// <summary>Nothing was or can be paid under the key; the caller may discard it.</summary>
    public const string Voided = "voided";

    /// <summary>A payment may exist under the key; the caller keeps it and follows <see cref="FiatCheckoutVoid.Payment"/>.</summary>
    public const string Revealed = "revealed";
}

/// <summary>What a void of one request key decided. <see cref="Payment"/> is set exactly when <see cref="Outcome"/> is revealed.</summary>
public sealed record FiatCheckoutVoid(string Outcome, Guid? IntentId, FiatCheckoutSession? Payment);

/// <summary>Starts and reads an add-funds payment on the node's fiat rail.</summary>
/// <remarks>See <c>src/ArdaNova.Infrastructure/Azoa/AGENTS.md</c> section on-ramp.</remarks>
public interface IFiatRailGateway
{
    /// <summary>Starts, or resumes on the same request key, one payment that adds funds.</summary>
    Task<Result<FiatCheckoutSession>> BeginAddFundsAsync(
        FiatCheckoutStart request, CancellationToken ct = default);

    /// <summary>Reads one payment for the account that owns it; a guessed id reads nothing.</summary>
    Task<Result<FiatCheckoutSession>> GetSessionAsync(
        Guid accountId, Guid intentId, CancellationToken ct = default);

    /// <summary>Voids <paramref name="requestKey"/> so nothing can be paid under it, or reveals what already was.</summary>
    Task<Result<FiatCheckoutVoid>> VoidRequestAsync(
        Guid accountId, string requestKey, CancellationToken ct = default);
}

/// <summary>
/// The off switch for the on-ramp, registered by <c>AddApplication</c> so the seam is always
/// bound; <c>AddInfrastructure</c> replaces it with the real gateway when the node transport is
/// configured. It refuses rather than pretending, so an unconfigured deployment cannot take money.
/// </summary>
public sealed class DisabledFiatRailGateway : IFiatRailGateway
{
    private const string Refusal =
        "Adding funds is not switched on for this deployment.";

    /// <inheritdoc />
    public Task<Result<FiatCheckoutSession>> BeginAddFundsAsync(
        FiatCheckoutStart request, CancellationToken ct = default)
        => Task.FromResult(Result<FiatCheckoutSession>.Failure(Refusal).WithCode(UpstreamFailureCodes.PaymentsNotConfigured));

    /// <inheritdoc />
    public Task<Result<FiatCheckoutSession>> GetSessionAsync(
        Guid accountId, Guid intentId, CancellationToken ct = default)
        => Task.FromResult(Result<FiatCheckoutSession>.Failure(Refusal).WithCode(UpstreamFailureCodes.PaymentsNotConfigured));

    /// <inheritdoc />
    /// <remarks>Never "voided": with no rail to ask, nothing here can prove a key is safe to drop.</remarks>
    public Task<Result<FiatCheckoutVoid>> VoidRequestAsync(
        Guid accountId, string requestKey, CancellationToken ct = default)
        => Task.FromResult(Result<FiatCheckoutVoid>.Failure(Refusal).WithCode(UpstreamFailureCodes.PaymentsNotConfigured));
}
