namespace ArdaNova.Application.Services.Interfaces;

using System.Globalization;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>Person-facing refusal text for the value-action gate, shared so gate and callers cannot drift.</summary>
public static class ValueEligibilityMessages
{
    /// <summary>No account could be identified for the action; a missing subject is a refusal, never a skipped check.</summary>
    public const string SubjectUnknown =
        "We could not identify the account this action is for, so nothing was moved.";

    /// <summary>Readiness could not be confirmed at all; says nothing about whether this person finished setup.</summary>
    public const string ServiceUnavailable =
        "We could not confirm account setup just now, so nothing was moved. This is not a problem with your account - please try again shortly.";

    /// <summary>The account was read successfully and this person still has setup steps left.</summary>
    /// <remarks>
    /// This is also the text a per-person uniqueness denial must carry. D23 requires such a
    /// denial to be indistinguishable from ordinary ineligibility, so it never gets copy of
    /// its own - distinct copy would tell one tenant that its person exists on another.
    /// </remarks>
    public static string SetupIncomplete(string action)
    {
        var describedAction = string.IsNullOrWhiteSpace(action)
            ? "continue with this action"
            : action;
        return $"Complete your account setup before you can {describedAction}. You can still create projects and collaborate while setup is pending.";
    }

    /// <summary>Setup is finished and a reviewer still owes this account a decision.</summary>
    /// <remarks>
    /// Deliberately says nothing about WHY a review is open - not the check that opened it,
    /// not a score, not a name match. It says the two things the person can act on: there is
    /// nothing left for them to redo, and a human can be reached.
    /// </remarks>
    public static string UnderReview(DateTimeOffset? reviewDueAt)
    {
        var decisionExpected = reviewDueAt is null
            ? "shortly"
            : "by " + reviewDueAt.Value.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        return $"Your identity check is complete and our review team is finishing one last check on your account, so nothing was moved. A decision is expected {decisionExpected} - there is nothing for you to redo. Contact support if you need it sooner.";
    }
}

/// <summary>
/// Carries a readiness refusal to a caller's own Result WITHOUT flattening it.
/// </summary>
/// <remarks>
/// The gate has four outcomes, not two: ready, setup genuinely unfinished, held for a
/// human decision, and could-not-confirm. Converting every failure to Forbidden turns an
/// honest "we could not confirm, try again" into "you are not allowed" - it blames the
/// person for the platform being down, and points them at a setup flow they have already
/// finished. Conflict must survive to the edge, where it becomes a 409, not a 403.
///
/// <paramref name="forbiddenOverride"/> REPLACES the gate's own words, so a caller that
/// supplies one erases the difference between "finish your setup" and "a reviewer owes you
/// a decision". Pass it only when the caller genuinely knows more than the gate does.
/// </remarks>
public static class ValueEligibilityFailure
{
    public static Result<TOut> Propagate<TIn, TOut>(Result<TIn> readiness, string? forbiddenOverride = null)
    {
        var error = readiness.Error ?? ValueEligibilityMessages.ServiceUnavailable;
        return readiness.Type switch
        {
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            _ => Result<TOut>.Forbidden(forbiddenOverride ?? error),
        };
    }

    /// <summary>HTTP status for a readiness refusal: 409 when the platform could not answer, else 403.</summary>
    public static int StatusCode<TIn>(Result<TIn> readiness)
        => readiness.Type == ResultType.Conflict ? 409 : 403;
}

/// <summary>Confirms the complete provider-authoritative account state required for a value-bearing action.</summary>
/// <remarks>
/// CHECKLIST NOTE - per-person limits. A limit of the form "one X per person" is expressed as an AZOA
/// uniqueness requirement - one verified person resolves to exactly one avatar, and AZOA enforces that
/// uniqueness at the identity - never as a count of ArdaNova rows. A row count answers "how many records
/// exist", which is not the question a per-person limit asks: rows are per-account, and one person can
/// hold several accounts until an identity says otherwise. Two consequences bind any code added here:
/// <list type="number">
///   <item>Never add a repository count to this gate. If a limit needs enforcing, it needs an AZOA
///   uniqueness requirement, and this service reads its outcome.</item>
///   <item>A uniqueness denial must reuse <see cref="ValueEligibilityMessages.SetupIncomplete"/> verbatim.
///   D23 requires it to be indistinguishable from ordinary ineligibility, because distinct copy would
///   tell one tenant that its person already exists on another.</item>
/// </list>
/// </remarks>
public interface IAzoaValueEligibilityService
{
    /// <summary>Fails closed unless identity, approved verification, avatar, and managed-account evidence are all ready.</summary>
    /// <returns>
    /// <see cref="ResultType.Success"/> with the authoritative status when every signal is confirmed;
    /// <see cref="ResultType.Forbidden"/> only when the account was actually read - carrying
    /// <see cref="ValueEligibilityMessages.SetupIncomplete"/> when setup is genuinely unfinished,
    /// <see cref="ValueEligibilityMessages.UnderReview"/> when it is finished and a reviewer owes a
    /// decision, or <see cref="ValueEligibilityMessages.SubjectUnknown"/> when no subject was supplied;
    /// <see cref="ResultType.Conflict"/> when readiness could not be confirmed at all, which is a
    /// platform state and must never be reported as unfinished setup.
    /// </returns>
    Task<Result<AzoaCustodialAccountStatusDto>> RequireReadyAsync(
        string userId,
        string action,
        CancellationToken ct = default);
}
