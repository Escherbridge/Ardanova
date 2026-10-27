namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;

/// <summary>
/// Stable, machine-readable refusal codes for money endpoints (SR-05 P3a). A host-injected SDK
/// transport maps on <see cref="MoneyRefusalBody.Code"/> instead of parsing
/// <see cref="MoneyRefusalBody.Message"/> prose.
/// </summary>
/// <remarks>
/// <para><b>Mapping table</b> (node/BFF signal -&gt; code -&gt; controller):</para>
/// <list type="bullet">
/// <item>KYB gate (oasis-sleek <c>KybGate.RequiredCode</c> = <c>KYB_REQUIRED</c>), reached through
/// custodial-account/KYC lifecycle calls (<c>KybCapability.CustodialAccess</c>) -&gt;
/// <see cref="MoneyRefusalCodes.KybRequired"/> -&gt; <c>AzoaCustodialAccountController</c>.</item>
/// <item>Consent grant missing/revoked at the units:issue checkout probe (oasis-sleek
/// <c>FiatCheckoutController.AuthoriseSubjectAsync</c>, <c>TenantConsentGate.EnsureAllowedAsync</c>)
/// -&gt; <see cref="MoneyRefusalCodes.ConsentRevoked"/> -&gt; <c>AddFundsController</c>.</item>
/// <item>F-H8 tenant principal refused at payout-account setup (oasis-sleek
/// <c>FiatPayoutController.BeginAccountSetup</c> ~112-117, the node's only documented 403 on the
/// <c>/payouts/{id}/account</c> route family) -&gt; <see cref="MoneyRefusalCodes.AccountHolderRequired"/>
/// -&gt; <c>PayoutsController</c> withdraw/destination actions.</item>
/// <item>AddFunds/PayoutService readiness gate (<c>IAzoaValueEligibilityService.RequireReadyAsync</c>):
/// <see cref="ResultType.Conflict"/> -&gt; <see cref="MoneyRefusalCodes.Unconfirmed"/>;
/// <see cref="ResultType.Forbidden"/> carrying <c>ValueEligibilityMessages.UnderReview</c> -&gt;
/// <see cref="MoneyRefusalCodes.UnderReview"/>; any other <see cref="ResultType.Forbidden"/> -&gt;
/// <see cref="MoneyRefusalCodes.SetupRequired"/>.</item>
/// <item>Deployment switched off (<c>DisabledFiatRailGateway</c>, a null <c>ICashOutService</c>, or
/// any "switched on" / "not configured" wording) -&gt; <see cref="MoneyRefusalCodes.PaymentsNotConfigured"/>.</item>
/// <item>Node rate limit (429, <c>AzoaNodeClient</c> "rate limit exceeded") -&gt;
/// <see cref="MoneyRefusalCodes.RailBusy"/>.</item>
/// <item>Node definite decline (422 -&gt; <see cref="ResultType.BadRequest"/> "declined ... nothing was
/// moved", or local client-input validation) -&gt; <see cref="MoneyRefusalCodes.Definite"/>.</item>
/// <item>Ambiguous/unresolvable reads (<see cref="ResultType.NotFound"/>, or
/// <see cref="ResultType.Conflict"/> outside the readiness gate) -&gt; <see cref="MoneyRefusalCodes.Unconfirmed"/>.</item>
/// <item><see cref="ResultType.Unauthorized"/> -&gt; <see cref="MoneyRefusalCodes.Unauthenticated"/>.</item>
/// </list>
/// <para><b>Known gap.</b> <c>AzoaNodeClient.MapError</c> and <c>AzoaCashOutClient.Refused</c>
/// (Infrastructure layer, out of this phase's scope) already collapse the node's own code/message
/// for a generic 403 before a <see cref="Result{T}"/> reaches a controller - only the
/// <c>"KYC_FORBIDDEN:"</c> prefix currently survives as a distinct message. So
/// <see cref="MoneyRefusalCodes.ConsentRevoked"/>, <see cref="MoneyRefusalCodes.KybRequired"/> and
/// <see cref="MoneyRefusalCodes.AccountHolderRequired"/> below are inferred from WHICH call site
/// (<see cref="MoneyRefusalContext"/>) produced the Forbidden, not from re-reading the node's own
/// code - each route family has exactly one documented node-side 403 cause today. A later phase
/// widening <c>AzoaNodeClient</c> to preserve <c>envelope.Code</c> would let the message-prefix
/// checks here fire directly, with the context defaults becoming a true fallback.</para>
/// </remarks>
public static class MoneyRefusalCodes
{
    public const string SetupRequired = "setup_required";
    public const string UnderReview = "under_review";
    public const string ConsentRevoked = "consent_revoked";
    public const string AccountHolderRequired = "account_holder_required";
    public const string KybRequired = "kyb_required";
    public const string PaymentsNotConfigured = "payments_not_configured";
    public const string RailBusy = "rail_busy";
    public const string Definite = "definite";
    public const string Unconfirmed = "unconfirmed";
    public const string Unauthenticated = "unauthenticated";
}

/// <summary>The error body every money endpoint carries, alongside whatever route-specific fields already exist.</summary>
/// <param name="Code">One of <see cref="MoneyRefusalCodes"/>, stable across releases.</param>
/// <param name="Message">The existing human-readable sentence; unchanged wording, now also machine-labelled.</param>
/// <param name="RetryAfterSeconds">Set only when the BFF actually knows a wait is warranted (rail busy); null otherwise - never guessed.</param>
public sealed record MoneyRefusalBody(string Code, string Message, int? RetryAfterSeconds = null);

/// <summary>
/// Narrows which of several plausible node-side refusals a generic <see cref="ResultType.Forbidden"/>
/// maps to, by call site. See the mapping table on <see cref="MoneyRefusalCodes"/>.
/// </summary>
public enum MoneyRefusalContext
{
    /// <summary>No call-specific knowledge beyond the plain <see cref="ResultType"/> defaults.</summary>
    General,

    /// <summary>The add-funds checkout call (units:issue consent probe). Consent revocation is the dominant cause.</summary>
    TenantConsentProbe,

    /// <summary>Custodial account / KYC lifecycle calls, gated by KybCapability.CustodialAccess.</summary>
    CustodialAccountLifecycle,

    /// <summary>The payout destination ("where your money goes") route family; F-H8 is its only documented 403 cause.</summary>
    PayoutDestinationSetup,

    /// <summary>Any other cash-out call (quote, sellable units, withdraw, history) whose canned refusal text is generic.</summary>
    CashOutGeneral,
}

/// <summary>Resolves a <see cref="Result{T}"/> (or a local, non-Result branch) to a <see cref="MoneyRefusalBody"/>.</summary>
public static class MoneyRefusal
{
    private const string KybRequiredPrefix = "KYB_REQUIRED:";
    private const string KycForbiddenPrefix = "KYC_FORBIDDEN:";
    private const string ReviewTeamMarker = "review team is finishing one last check";
    private const string RateLimitMarker = "rate limit exceeded";
    private const string SwitchedOnMarker = "switched on";
    private const string TransportNotConfiguredMarker = "transport is not configured";

    /// <summary>Fixed hint for a busy rail; AZOA's 429 carries no Retry-After through this transport today.</summary>
    private const int RailBusyRetryAfterSeconds = 30;

    /// <summary>Maps a failed <see cref="Result{T}"/> to a refusal body. Never call this on success.</summary>
    public static MoneyRefusalBody From<T>(
        Result<T> result, MoneyRefusalContext context = MoneyRefusalContext.General, string? fallbackMessage = null)
    {
        var message = result.Error ?? fallbackMessage ?? "The request could not be completed.";
        var code = ResolveCode(result.Type, message, context);
        return new MoneyRefusalBody(code, message, RetryAfterFor(code));
    }

    /// <summary>Builds a refusal body directly for a local branch that never produced a <see cref="Result{T}"/>.</summary>
    public static MoneyRefusalBody For(string code, string message) => new(code, message, RetryAfterFor(code));

    /// <summary>
    /// The wire shape every money-endpoint error carries: the pre-existing <c>error</c> field
    /// (so <c>readApiError</c> in the Next.js client keeps working unmodified) plus the new
    /// <c>code</c>/<c>message</c>/<c>retryAfterSeconds</c> fields the SDK transport reads.
    /// </summary>
    public static object ToErrorBody(MoneyRefusalBody refusal) => new
    {
        error = refusal.Message,
        code = refusal.Code,
        message = refusal.Message,
        retryAfterSeconds = refusal.RetryAfterSeconds,
    };

    private static int? RetryAfterFor(string code) =>
        code == MoneyRefusalCodes.RailBusy ? RailBusyRetryAfterSeconds : null;

    private static string ResolveCode(ResultType type, string message, MoneyRefusalContext context) => type switch
    {
        ResultType.Unauthorized => MoneyRefusalCodes.Unauthenticated,
        ResultType.NotFound => MoneyRefusalCodes.Unconfirmed,
        ResultType.Conflict => MoneyRefusalCodes.Unconfirmed,
        ResultType.ValidationError => MoneyRefusalCodes.Definite,
        ResultType.BadRequest => MoneyRefusalCodes.Definite,
        ResultType.Forbidden => ResolveForbidden(message, context),
        ResultType.Failure => ResolveFailure(message),
        _ => MoneyRefusalCodes.Definite,
    };

    private static string ResolveForbidden(string message, MoneyRefusalContext context)
    {
        // These two prefixes are the only node-provided markers that currently survive
        // AzoaNodeClient.MapError's collapse of a generic 403 (see class remarks). When they are
        // present, trust them over the call-site default.
        if (message.Contains(KybRequiredPrefix, StringComparison.Ordinal))
            return MoneyRefusalCodes.KybRequired;

        if (message.Contains(KycForbiddenPrefix, StringComparison.Ordinal)
            || message.Contains(ReviewTeamMarker, StringComparison.Ordinal))
        {
            return MoneyRefusalCodes.UnderReview;
        }

        return context switch
        {
            MoneyRefusalContext.TenantConsentProbe => MoneyRefusalCodes.ConsentRevoked,
            MoneyRefusalContext.CustodialAccountLifecycle => MoneyRefusalCodes.KybRequired,
            MoneyRefusalContext.PayoutDestinationSetup => MoneyRefusalCodes.AccountHolderRequired,
            MoneyRefusalContext.CashOutGeneral => MoneyRefusalCodes.SetupRequired,
            _ => MoneyRefusalCodes.SetupRequired,
        };
    }

    private static string ResolveFailure(string message)
    {
        if (message.Contains(RateLimitMarker, StringComparison.OrdinalIgnoreCase))
            return MoneyRefusalCodes.RailBusy;

        if (message.Contains(SwitchedOnMarker, StringComparison.OrdinalIgnoreCase)
            || message.Contains(TransportNotConfiguredMarker, StringComparison.OrdinalIgnoreCase))
        {
            return MoneyRefusalCodes.PaymentsNotConfigured;
        }

        // Timeouts, unparseable responses, generic node failures: all transient/retryable.
        return MoneyRefusalCodes.RailBusy;
    }
}
