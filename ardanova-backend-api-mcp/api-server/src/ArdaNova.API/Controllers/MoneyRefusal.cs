namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;

/// <summary>
/// Stable, machine-readable refusal codes for money endpoints (SR-05 P3a). A caller classifies on
/// <see cref="MoneyRefusalBody.Code"/>, never on the HTTP status or the prose. The mapping table and
/// the key-release contract live in <c>Controllers/AGENTS.md</c> §Money refusal codes.
/// </summary>
public static class MoneyRefusalCodes
{
    public const string SetupRequired = "setup_required";
    public const string UnderReview = "under_review";
    public const string ConsentRevoked = "consent_revoked";
    public const string AccountHolderRequired = "account_holder_required";
    public const string KybRequired = "kyb_required";
    public const string PaymentsNotConfigured = "payments_not_configured";
    public const string RailBusy = "rail_busy";

    /// <summary>
    /// The ONLY code a caller may release its saved request key on: nothing was, or ever will be,
    /// admitted under that key for this request.
    /// </summary>
    public const string Definite = "definite";

    public const string Unconfirmed = "unconfirmed";
    public const string Unauthenticated = "unauthenticated";
}

/// <summary>Why a refusal carries the code it does, when the code alone does not say.</summary>
public static class MoneyRefusalReasons
{
    /// <summary>The intent behind the key reached a final state; nothing more will happen under it.</summary>
    public const string Finished = "finished";

    /// <summary>The intent behind the key expired unpaid.</summary>
    public const string Expired = "expired";

    /// <summary>The key covers a different request, so this one was never admitted under it.</summary>
    public const string KeyInUse = "key_in_use";

    /// <summary>The intent behind the key is held for a human decision; its outcome is still open.</summary>
    public const string NeedsReview = "needs_review";
}

/// <summary>The error body every money endpoint carries, alongside whatever route-specific fields already exist.</summary>
/// <param name="Code">One of <see cref="MoneyRefusalCodes"/>, stable across releases.</param>
/// <param name="Message">The existing human-readable sentence; unchanged wording, now also machine-labelled.</param>
/// <param name="RetryAfterSeconds">Set only when the BFF actually knows a wait is warranted (rail busy); null otherwise - never guessed.</param>
/// <param name="Reason">One of <see cref="MoneyRefusalReasons"/> when an upstream code named the outcome; null otherwise.</param>
public sealed record MoneyRefusalBody(string Code, string Message, int? RetryAfterSeconds = null, string? Reason = null);

/// <summary>
/// Narrows which of several plausible node-side refusals a generic <see cref="ResultType.Forbidden"/>
/// maps to, by call site - the fallback used only when no upstream code named it.
/// </summary>
public enum MoneyRefusalContext
{
    /// <summary>No call-specific knowledge beyond the plain <see cref="ResultType"/> defaults.</summary>
    General,

    /// <summary>The add-funds checkout call (units:issue consent probe). Consent revocation is the dominant cause.</summary>
    TenantConsentProbe,

    /// <summary>Custodial account / KYC lifecycle calls, gated by KybCapability.CustodialAccess.</summary>
    CustodialAccountLifecycle,

    /// <summary>Payout destination SETUP (POST /payouts/{id}/account), where F-H8 is the only documented 403 cause.</summary>
    PayoutDestinationSetup,

    /// <summary>Any other cash-out call (quote, sellable units, withdraw, destination read, history) whose canned refusal text is generic.</summary>
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
        var (code, reason) = FromUpstreamCode(result.Code)
            ?? (ResolveByType(result.Type, result.Code, message, context), (string?)null);
        return new MoneyRefusalBody(code, message, RetryAfterFor(code), reason);
    }

    /// <summary>Builds a refusal body directly for a local branch that never produced a <see cref="Result{T}"/>.</summary>
    public static MoneyRefusalBody For(string code, string message) => new(code, message, RetryAfterFor(code));

    /// <summary>
    /// The wire shape every money-endpoint error carries: the pre-existing <c>error</c> field
    /// (so <c>readApiError</c> in the Next.js client keeps working unmodified) plus the
    /// <c>code</c>/<c>message</c>/<c>retryAfterSeconds</c>/<c>reason</c> fields callers classify on.
    /// </summary>
    public static object ToErrorBody(MoneyRefusalBody refusal) => new
    {
        error = refusal.Message,
        code = refusal.Code,
        message = refusal.Message,
        retryAfterSeconds = refusal.RetryAfterSeconds,
        reason = refusal.Reason,
    };

    private static int? RetryAfterFor(string code) =>
        code == MoneyRefusalCodes.RailBusy ? RailBusyRetryAfterSeconds : null;

    /// <summary>A real upstream code outranks every inference below; null when it names nothing this table knows.</summary>
    private static (string Code, string? Reason)? FromUpstreamCode(string? upstreamCode) => upstreamCode switch
    {
        UpstreamFailureCodes.KybRequired => (MoneyRefusalCodes.KybRequired, null),
        UpstreamFailureCodes.IntentFinished => (MoneyRefusalCodes.Definite, MoneyRefusalReasons.Finished),
        UpstreamFailureCodes.IntentExpired => (MoneyRefusalCodes.Definite, MoneyRefusalReasons.Expired),
        UpstreamFailureCodes.RequestKeyInUse => (MoneyRefusalCodes.Definite, MoneyRefusalReasons.KeyInUse),
        UpstreamFailureCodes.IntentNeedsReview => (MoneyRefusalCodes.Unconfirmed, MoneyRefusalReasons.NeedsReview),
        UpstreamFailureCodes.PaymentsNotConfigured
            or UpstreamFailureCodes.RouteNotAllowed
            or UpstreamFailureCodes.TransportNotConfigured => (MoneyRefusalCodes.PaymentsNotConfigured, null),
        UpstreamFailureCodes.UpstreamRateLimited => (MoneyRefusalCodes.RailBusy, null),
        _ => null,
    };

    private static string ResolveByType(ResultType type, string? upstreamCode, string message, MoneyRefusalContext context)
        => type switch
        {
            ResultType.Unauthorized => MoneyRefusalCodes.Unauthenticated,
            // Absence is not an answer: an unread payment or intent may still exist.
            ResultType.NotFound => MoneyRefusalCodes.Unconfirmed,
            ResultType.Conflict => MoneyRefusalCodes.Unconfirmed,
            ResultType.ValidationError => MoneyRefusalCodes.Definite,
            ResultType.BadRequest => MoneyRefusalCodes.Definite,
            ResultType.Forbidden => ResolveForbidden(upstreamCode, message, context),
            ResultType.Failure => ResolveFailure(message),
            _ => MoneyRefusalCodes.Unconfirmed,
        };

    private static string ResolveForbidden(string? upstreamCode, string message, MoneyRefusalContext context)
    {
        if (message.Contains(KybRequiredPrefix, StringComparison.Ordinal))
            return MoneyRefusalCodes.KybRequired;

        // Finished KYC waiting on a reviewer is the only "under review"; KYC_FORBIDDEN also fires for
        // KYC that was never finished, so it is setup the person still has to do.
        if (message.Contains(ReviewTeamMarker, StringComparison.Ordinal))
            return MoneyRefusalCodes.UnderReview;

        if (message.Contains(KycForbiddenPrefix, StringComparison.Ordinal))
            return MoneyRefusalCodes.SetupRequired;

        return context switch
        {
            MoneyRefusalContext.TenantConsentProbe => MoneyRefusalCodes.ConsentRevoked,
            // The node codes its custodial KYB gate (KYB_REQUIRED, handled above), so a coded 403
            // that is NOT it is the tenant's own authorization - operator configuration. Only an
            // uncoded 403 (an older node, a proxy) still falls back to the KYB inference.
            MoneyRefusalContext.CustodialAccountLifecycle => upstreamCode is null
                ? MoneyRefusalCodes.KybRequired
                : MoneyRefusalCodes.PaymentsNotConfigured,
            MoneyRefusalContext.PayoutDestinationSetup => MoneyRefusalCodes.AccountHolderRequired,
            _ => MoneyRefusalCodes.SetupRequired,
        };
    }

    private static string ResolveFailure(string message)
    {
        // Fallbacks for a Failure that reached here without its code; the code table above wins.
        if (message.Contains(RateLimitMarker, StringComparison.OrdinalIgnoreCase))
            return MoneyRefusalCodes.RailBusy;

        if (message.Contains(SwitchedOnMarker, StringComparison.OrdinalIgnoreCase)
            || message.Contains(TransportNotConfiguredMarker, StringComparison.OrdinalIgnoreCase))
        {
            return MoneyRefusalCodes.PaymentsNotConfigured;
        }

        // Timeouts, an empty/unparseable/no-payload 2xx, a dropped connection: the request may
        // have been admitted, so this is never "busy" and never final.
        return MoneyRefusalCodes.Unconfirmed;
    }
}
