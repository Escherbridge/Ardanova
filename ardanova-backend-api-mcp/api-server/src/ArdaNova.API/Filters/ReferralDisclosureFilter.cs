using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArdaNova.API.Filters;

/// <summary>What a referral code lookup tells someone who is not part of the referral.</summary>
public sealed record ReferralCodeView(string? ReferralCode, ReferralStatus Status);

/// <summary>Projects every ReferralDto response to <see cref="ReferralCodeView"/> unless the viewer is the referrer, the referred user or an administrator. See Filters/AGENTS.md §Referral disclosure.</summary>
public sealed class ReferralDisclosureFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: { } value } result)
            return;
        if (ViewerAuthority.IsAdministrator(context.HttpContext.User))
            return;

        var actorId = ViewerAuthority.ActorId(context.HttpContext.User);
        result.Value = value switch
        {
            ReferralDto referral => Project(referral, actorId),
            IEnumerable<ReferralDto> referrals => referrals.Select(referral => Project(referral, actorId)).ToList(),
            _ => value,
        };
        if (!ReferenceEquals(result.Value, value))
            result.DeclaredType = null;
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static object Project(ReferralDto referral, string? actorId)
        => actorId is not null
            && (string.Equals(referral.ReferrerId, actorId, StringComparison.Ordinal)
                || string.Equals(referral.ReferredId, actorId, StringComparison.Ordinal))
            ? referral
            : new ReferralCodeView(referral.ReferralCode, referral.Status);
}
