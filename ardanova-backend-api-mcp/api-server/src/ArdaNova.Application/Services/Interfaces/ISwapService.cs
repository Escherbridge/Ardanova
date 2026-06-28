namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ISwapService
{
    Task<Result<SwapPreviewDto>> GetSwapPreviewAsync(
        string userId,
        string sourceConfigId,
        string targetConfigId,
        int sourceTokenAmount,
        CancellationToken ct = default);

    Task<Result<SwapResultDto>> ExecuteSwapAsync(
        string userId,
        SwapRequestDto request,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<SwapHistoryDto>>> GetSwapHistoryAsync(
        string userId,
        CancellationToken ct = default);
}
