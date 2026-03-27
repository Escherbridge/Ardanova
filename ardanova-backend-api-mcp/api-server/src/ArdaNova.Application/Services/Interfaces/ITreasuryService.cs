namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITreasuryService
{
    /// <summary>Splits incoming USD across three buckets (55% index, 30% liquid, 15% ops) and logs transactions.</summary>
    Task<Result<bool>> ProcessFundingInflowAsync(double usdAmount, string? projectId, CancellationToken ct = default);

    /// <summary>Applies monthly index fund return and deducts platform profit share.</summary>
    Task<Result<bool>> ApplyIndexFundReturnAsync(CancellationToken ct = default);

    /// <summary>Transfers from index fund to liquid reserve if liquid is insufficient. Returns amount transferred.</summary>
    Task<Result<double>> RebalanceIfNeededAsync(double requiredLiquid, CancellationToken ct = default);

    /// <summary>Verifies that token balances × values ≤ total treasury. Detects discrepancies.</summary>
    Task<Result<bool>> ReconcileAsync(CancellationToken ct = default);

    Task<Result<TreasuryStatusDto>> GetStatusAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<PlatformTreasuryTransactionDto>>> GetTransactionHistoryAsync(int limit = 50, CancellationToken ct = default);
}
