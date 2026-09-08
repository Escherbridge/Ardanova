namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>Confirms the complete provider-authoritative account state required for a value-bearing action.</summary>
public interface IAzoaValueEligibilityService
{
    /// <summary>Fails closed unless identity, approved verification, avatar, and managed-account evidence are all ready.</summary>
    Task<Result<AzoaCustodialAccountStatusDto>> RequireReadyAsync(
        string userId,
        string action,
        CancellationToken ct = default);
}
