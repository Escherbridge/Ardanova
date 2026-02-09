namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IExchangeService
{
    Task<Result<double>> GetProjectTokenValueAsync(string projectTokenConfigId, CancellationToken ct = default);
    Task<Result<double>> GetArdaValueAsync(CancellationToken ct = default);
    Task<Result<ConversionPreviewDto>> CalculateConversionAsync(string projectTokenConfigId, int tokenAmount, CancellationToken ct = default);
    Task<Result<TreasuryStatusDto>> GetTreasuryStatusAsync(CancellationToken ct = default);
}
