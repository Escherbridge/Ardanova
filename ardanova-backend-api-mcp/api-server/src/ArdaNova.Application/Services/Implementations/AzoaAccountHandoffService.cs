namespace ArdaNova.Application.Services.Implementations;

using System.Net.Mail;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using Microsoft.Extensions.Configuration;

public sealed class AzoaAccountHandoffService(IRepository<User> users, IAzoaCustodialAccountGateway custody,
    IConfiguration configuration) : IAzoaAccountHandoffService
{
    public async Task<Result<AccountHandoff>> BeginAsync(string userId, string? requestId, CancellationToken ct = default)
    {
        if (!Enabled || custody is not IAzoaAccountHandoffGateway gateway)
            return Result<AccountHandoff>.Conflict("Account handoff is not available on this deployment.");
        if (requestId is not null && !Guid.TryParse(requestId, out _))
            return Result<AccountHandoff>.ValidationError("Use the account request that was sent to your email.");
        var user = await users.GetByIdAsync(userId, ct);
        if (user?.emailVerified is null || !MailAddress.TryCreate(user.email, out var email)
            || email.Address != user.email || string.IsNullOrWhiteSpace(user.azoaAvatarId))
            return Result<AccountHandoff>.Forbidden("Sign in with your verified account before continuing.");
        if (!Uri.TryCreate(configuration["NEXTJS_URL"], UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttps && !(origin.Scheme == Uri.UriSchemeHttp && origin.IsLoopback
                && string.Equals(configuration["Azoa:AllowInsecureDevVerificationUrl"], "true", StringComparison.OrdinalIgnoreCase)))
            || !string.IsNullOrEmpty(origin.UserInfo)
            || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
            return Result<AccountHandoff>.Conflict("The secure account return address is not configured.");
        var result = await gateway.BeginHandoffAsync(userId, email.Address,
            new Uri(origin, "/portfolio/withdraw").AbsoluteUri, requestId, ct);
        return CheckTarget(result, user.azoaAvatarId);
    }

    public async Task<Result<AccountHandoff>> ReadAsync(string userId, string requestId, CancellationToken ct = default)
    {
        if (!Enabled || custody is not IAzoaAccountHandoffGateway gateway)
            return Result<AccountHandoff>.Conflict("Account handoff is not available on this deployment.");
        if (!Guid.TryParse(requestId, out _))
            return Result<AccountHandoff>.ValidationError("Use a valid account request.");
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.azoaAvatarId))
            return Result<AccountHandoff>.NotFound("That account could not be found.");
        return CheckTarget(await gateway.ReadHandoffAsync(userId, requestId, ct), user.azoaAvatarId);
    }

    private bool Enabled => string.Equals(configuration["Azoa:ClaimInvites:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

    private static Result<AccountHandoff> CheckTarget(Result<AccountHandoff> result, string target)
        => result.IsSuccess && (!Guid.TryParse(result.Value?.TargetUserId, out var returned)
            || !Guid.TryParse(target, out var expected) || returned != expected)
            ? Result<AccountHandoff>.Conflict("The account request did not match this account.") : result;
}
