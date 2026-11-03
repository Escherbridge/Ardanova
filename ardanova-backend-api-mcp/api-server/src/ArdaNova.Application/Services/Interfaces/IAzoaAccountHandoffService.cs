namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;

public sealed record AccountHandoff(string RequestId, string TargetUserId, string Ownership, string Status,
    DateTimeOffset ExpiresAt, string Delivery);

public interface IAzoaAccountHandoffService
{
    Task<Result<AccountHandoff>> BeginAsync(string userId, string? requestId, CancellationToken ct = default);
    Task<Result<AccountHandoff>> ReadAsync(string userId, string requestId, CancellationToken ct = default);
}

public interface IAzoaAccountHandoffGateway
{
    Task<Result<AccountHandoff>> BeginHandoffAsync(string externalSubject, string email, string returnUrl, string? requestId, CancellationToken ct);
    Task<Result<AccountHandoff>> ReadHandoffAsync(string externalSubject, string requestId, CancellationToken ct);
}
