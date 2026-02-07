namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Domain.Models.Enums;

public interface IKycGateService
{
    Task<Result<bool>> RequireProAsync(string userId, CancellationToken ct = default);
    Task<Result<bool>> RequireVerifiedAsync(string userId, CancellationToken ct = default);
    Task<Result<VerificationLevel>> GetVerificationLevelAsync(string userId, CancellationToken ct = default);
}
