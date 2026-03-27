namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IKycService
{
    Task<Result<KycSubmissionDto>> SubmitAsync(SubmitKycDto dto, CancellationToken ct = default);
    Task<Result<KycSubmissionDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<KycSubmissionDto>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<List<KycSubmissionDto>>> GetPendingAsync(CancellationToken ct = default);
    Task<Result<KycSubmissionDto>> ApproveAsync(string id, string reviewerId, string? notes, CancellationToken ct = default);
    Task<Result<KycSubmissionDto>> RejectAsync(string id, string reviewerId, string? notes, string? rejectionReason, CancellationToken ct = default);
}
