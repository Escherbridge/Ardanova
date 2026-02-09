namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IPayoutService
{
    Task<Result<PayoutRequestDto>> RequestPayoutAsync(string userId, CreatePayoutRequestDto dto, CancellationToken ct = default);
    Task<Result<PayoutRequestDto>> ProcessPayoutAsync(string payoutRequestId, CancellationToken ct = default);
    Task<Result<PayoutRequestDto>> CancelPayoutAsync(string payoutRequestId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PayoutRequestDto>>> GetPayoutsByUserAsync(string userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PayoutRequestDto>>> GetPendingPayoutsAsync(CancellationToken ct = default);
}
