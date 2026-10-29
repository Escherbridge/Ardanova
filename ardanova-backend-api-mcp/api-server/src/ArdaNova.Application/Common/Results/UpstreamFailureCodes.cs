namespace ArdaNova.Application.Common.Results;

/// <summary>
/// Values <see cref="Result{T}.Code"/> may carry: the AZOA node's own machine codes this BFF acts on,
/// and the BFF transport's own codes for failures no node ever saw. See <c>Common/AGENTS.md</c> §Failure codes.
/// </summary>
public static class UpstreamFailureCodes
{
    // -- The node's own codes (oasis-sleek), carried verbatim from its envelope --------------------

    /// <summary>The tenant's business verification is missing or lapsed (oasis-sleek <c>KybGate.RequiredCode</c>).</summary>
    public const string KybRequired = "KYB_REQUIRED";

    /// <summary>The intent behind this request key reached a terminal state - which may be PAID. Never proof of "nothing paid".</summary>
    public const string IntentFinished = "INTENT_FINISHED";

    /// <summary>A fresh provider read proved nothing was paid under this key and nothing can be.</summary>
    public const string IntentExpired = "INTENT_EXPIRED";

    /// <summary>This key already covers a DIFFERENT request, which itself may have been paid.</summary>
    public const string RequestKeyInUse = "REQUEST_KEY_IN_USE";

    /// <summary>The intent behind this request key is held for a human decision; its outcome is still open.</summary>
    public const string IntentNeedsReview = "INTENT_NEEDS_REVIEW";

    /// <summary>This key was voided: nothing was, or ever can be, paid under it (oasis-sleek <c>FiatConsumerErrors.RequestVoidedCode</c>).</summary>
    public const string RequestVoided = "REQUEST_VOIDED";

    /// <summary>The rail refused AFTER the node claimed the key, so the claim (and a later replay) still stands.</summary>
    public const string RailRefused = "RAIL_REFUSED";

    /// <summary>The deployment has no payment rail configured at all (oasis-sleek <c>FiatConsumerErrors.PaymentsNotConfiguredCode</c>).</summary>
    public const string PaymentsNotConfigured = "PAYMENTS_NOT_CONFIGURED";

    // -- The BFF transport's own codes: these failures never reached a node ----------------------

    /// <summary>A typed client refused to send: the path is outside its credential's route family. A configuration fault.</summary>
    public const string RouteNotAllowed = "ARDANOVA_ROUTE_NOT_ALLOWED";

    /// <summary>The node answered HTTP 429. The only failure this BFF calls "busy".</summary>
    public const string UpstreamRateLimited = "ARDANOVA_UPSTREAM_RATE_LIMITED";

    /// <summary>No transport to the node is configured on this deployment, so nothing was sent.</summary>
    public const string TransportNotConfigured = "ARDANOVA_TRANSPORT_NOT_CONFIGURED";
}
