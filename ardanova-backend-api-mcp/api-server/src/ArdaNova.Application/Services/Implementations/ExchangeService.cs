using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations;

public class ExchangeService : IExchangeService
{
    private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
    private readonly IRepository<PlatformTreasury> _treasuryRepository;
    private readonly IMapper _mapper;

    public ExchangeService(
        IRepository<ProjectTokenConfig> projectTokenConfigRepository,
        IRepository<PlatformTreasury> treasuryRepository,
        IMapper mapper)
    {
        _projectTokenConfigRepository = projectTokenConfigRepository;
        _treasuryRepository = treasuryRepository;
        _mapper = mapper;
    }

    public async Task<Result<double>> GetProjectTokenValueAsync(string projectTokenConfigId, CancellationToken ct = default)
    {
        var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
        {
            return Result<double>.Failure($"Project token configuration not found: {projectTokenConfigId}");
        }

        if (config.totalSupply == 0)
        {
            return Result<double>.Failure("Cannot calculate token value: total supply is zero");
        }

        // Value per token = total funding raised / total supply
        var tokenValue = (double)config.fundingRaised / config.totalSupply;
        return Result<double>.Success(tokenValue);
    }

    public async Task<Result<double>> GetArdaValueAsync(CancellationToken ct = default)
    {
        // Get the single treasury record
        var treasuries = await _treasuryRepository.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();

        if (treasury == null)
        {
            return Result<double>.Failure("Platform treasury not found");
        }

        if (treasury.ardaCirculatingSupply == 0)
        {
            return Result<double>.Failure("Cannot calculate ARDA value: circulating supply is zero");
        }

        // ARDA value = (index fund + liquid reserve + operations) / circulating supply
        var totalBacking = treasury.indexFundBalance + treasury.liquidReserveBalance + treasury.operationsBalance;
        var ardaValue = (double)totalBacking / treasury.ardaCirculatingSupply;

        return Result<double>.Success(ardaValue);
    }

    public async Task<Result<ConversionPreviewDto>> CalculateConversionAsync(
        string projectTokenConfigId,
        int tokenAmount,
        CancellationToken ct = default)
    {
        if (tokenAmount <= 0)
        {
            return Result<ConversionPreviewDto>.Failure("Token amount must be positive");
        }

        // Get project token value
        var projectTokenValueResult = await GetProjectTokenValueAsync(projectTokenConfigId, ct);
        if (!projectTokenValueResult.IsSuccess)
        {
            return Result<ConversionPreviewDto>.Failure(projectTokenValueResult.Error);
        }

        // Get ARDA value
        var ardaValueResult = await GetArdaValueAsync(ct);
        if (!ardaValueResult.IsSuccess)
        {
            return Result<ConversionPreviewDto>.Failure(ardaValueResult.Error);
        }

        var projectTokenValue = projectTokenValueResult.Value;
        var ardaValue = ardaValueResult.Value;

        // Calculate conversion
        var usdValue = tokenAmount * projectTokenValue;
        var ardaAmount = usdValue / ardaValue;

        var preview = new ConversionPreviewDto
        {
            ProjectTokenValueUsd = projectTokenValue,
            ArdaValueUsd = ardaValue,
            SourceTokenAmount = tokenAmount,
            UsdValue = usdValue,
            ArdaAmount = (long)ardaAmount
        };

        return Result<ConversionPreviewDto>.Success(preview);
    }

    public async Task<Result<TreasuryStatusDto>> GetTreasuryStatusAsync(CancellationToken ct = default)
    {
        // Get the single treasury record
        var treasuries = await _treasuryRepository.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();

        if (treasury == null)
        {
            return Result<TreasuryStatusDto>.Failure("Platform treasury not found");
        }

        var dto = _mapper.Map<TreasuryStatusDto>(treasury);
        return Result<TreasuryStatusDto>.Success(dto);
    }
}
