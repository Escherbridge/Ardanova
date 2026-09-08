namespace ArdaNova.Application.Services.Interfaces;

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
    public static string SetupIncomplete(string action)
    {
        var describedAction = string.IsNullOrWhiteSpace(action)
            ? "continue with this action"
            : action;
        return $"Complete your account setup before you can {describedAction}. You can still create projects and collaborate while setup is pending.";
    }
}

/// <summary>
/// Carries a readiness refusal to a caller's own Result WITHOUT flattening it.
/// </summary>
/// <remarks>
/// The gate has three outcomes, not two. Converting every failure to Forbidden turns an
/// honest "we could not confirm, try again" into "you are not allowed" - it blames the
/// person for the platform being down, and points them at a setup flow they have already
/// finished. Conflict must survive to the edge, where it becomes a 409, not a 403.
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
/// Per-person limits are expressed as AZOA uniqueness requirements - one verified person resolves to
/// exactly one avatar, and AZOA enforces that uniqueness - never as a count of ArdaNova rows. A row
/// count answers "how many records exist", which is not the question a per-person limit asks.
/// </remarks>
public interface IAzoaValueEligibilityService
{
    /// <summary>Fails closed unless identity, approved verification, avatar, and managed-account evidence are all ready.</summary>
    /// <returns>
    /// <see cref="ResultType.Success"/> with the authoritative status when every signal is confirmed;
    /// <see cref="ResultType.Forbidden"/> only when the account was actually read and setup is genuinely
    /// incomplete, or when no subject was supplied; <see cref="ResultType.Conflict"/> when readiness could
    /// not be confirmed at all, which is a platform state and must never be reported as unfinished setup.
    /// </returns>
    Task<Result<AzoaCustodialAccountStatusDto>> RequireReadyAsync(
        string userId,
        string action,
        CancellationToken ct = default);
}
