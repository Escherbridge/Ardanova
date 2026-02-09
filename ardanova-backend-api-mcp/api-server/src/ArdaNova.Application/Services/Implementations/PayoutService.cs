using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations
{
    public class PayoutService : IPayoutService
    {
        private readonly IRepository<PayoutRequest> _payoutRepository;
        private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITokenBalanceService _tokenBalanceService;
        private readonly IExchangeService _exchangeService;
        private readonly ITreasuryService _treasuryService;

        public PayoutService(
            IRepository<PayoutRequest> payoutRepository,
            IRepository<ProjectTokenConfig> projectTokenConfigRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITokenBalanceService tokenBalanceService,
            IExchangeService exchangeService,
            ITreasuryService treasuryService)
        {
            _payoutRepository = payoutRepository;
            _projectTokenConfigRepository = projectTokenConfigRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tokenBalanceService = tokenBalanceService;
            _exchangeService = exchangeService;
            _treasuryService = treasuryService;
        }

        public async Task<Result<PayoutRequestDto>> RequestPayoutAsync(string userId, CreatePayoutRequestDto dto, CancellationToken ct = default)
        {
            // 1. Get ProjectTokenConfig
            var config = await _projectTokenConfigRepository.GetByIdAsync(dto.SourceProjectTokenConfigId, ct);
            if (config == null)
            {
                return Result<PayoutRequestDto>.Failure("ProjectTokenConfig not found.");
            }

            // 2. Check if balance is liquid based on holder class and gate status
            var isLiquidResult = await _tokenBalanceService.IsBalanceLiquidAsync(
                userId,
                dto.SourceProjectTokenConfigId,
                dto.HolderClass,
                ct);

            if (!isLiquidResult.IsSuccess || !isLiquidResult.Value)
            {
                // Provide gate-specific rejection messages
                var gateStatus = config.gateStatus;
                var holderClass = dto.HolderClass;

                string message = (holderClass, gateStatus) switch
                {
                    (TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.FUNDING) =>
                        "Project hasn't reached funding goal yet.",
                    (TokenHolderClass.INVESTOR, ProjectGateStatus.ACTIVE) =>
                        "Project hasn't reached success milestone. Your tokens unlock when the project delivers.",
                    (TokenHolderClass.FOUNDER, ProjectGateStatus.ACTIVE) =>
                        "Founder tokens unlock after project success verification.",
                    (TokenHolderClass.FOUNDER, ProjectGateStatus.FAILED) =>
                        "Founder tokens were burned when the project failed.",
                    _ => "Tokens are not liquid and cannot be cashed out at this time."
                };

                return Result<PayoutRequestDto>.Failure(message);
            }

            // 3. Lock tokens
            var lockResult = await _tokenBalanceService.LockAsync(
                userId,
                dto.SourceProjectTokenConfigId,
                dto.SourceTokenAmount,
                dto.HolderClass,
                ct);

            if (!lockResult.IsSuccess)
            {
                return Result<PayoutRequestDto>.Failure(lockResult.Error);
            }

            // 4. Calculate conversion
            var conversionResult = await _exchangeService.CalculateConversionAsync(
                dto.SourceProjectTokenConfigId,
                dto.SourceTokenAmount,
                ct);

            if (!conversionResult.IsSuccess)
            {
                // Unlock tokens on failure
                await _tokenBalanceService.UnlockAsync(
                    userId,
                    dto.SourceProjectTokenConfigId,
                    dto.SourceTokenAmount,
                    dto.HolderClass,
                    ct);

                return Result<PayoutRequestDto>.Failure(conversionResult.Error);
            }

            var preview = conversionResult.Value;
            if (preview == null)
            {
                return Result<PayoutRequestDto>.Failure("Conversion preview is null");
            }

            // 5. Create PayoutRequest entity
            var payoutRequest = new PayoutRequest
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                sourceProjectTokenConfigId = dto.SourceProjectTokenConfigId,
                sourceTokenAmount = dto.SourceTokenAmount,
                ardaTokenAmount = (int)preview.ArdaAmount,
                usdAmount = preview.UsdValue,
                holderClass = dto.HolderClass,
                gateStatusAtRequest = config.gateStatus,
                status = PayoutStatus.PENDING,
                requestedAt = DateTime.UtcNow
            };

            // 6. Save and return DTO
            await _payoutRepository.AddAsync(payoutRequest, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var resultDto = _mapper.Map<PayoutRequestDto>(payoutRequest);
            return Result<PayoutRequestDto>.Success(resultDto);
        }

        public async Task<Result<PayoutRequestDto>> ProcessPayoutAsync(string payoutRequestId, CancellationToken ct = default)
        {
            // 1. Get payout request and validate status
            var payoutRequest = await _payoutRepository.GetByIdAsync(payoutRequestId, ct);
            if (payoutRequest == null)
            {
                return Result<PayoutRequestDto>.Failure("Payout request not found.");
            }

            if (payoutRequest.status != PayoutStatus.PENDING)
            {
                return Result<PayoutRequestDto>.Failure($"Cannot process payout with status {payoutRequest.status}. Only PENDING payouts can be processed.");
            }

            // 2. Set status to PROCESSING
            payoutRequest.status = PayoutStatus.PROCESSING;
            await _payoutRepository.UpdateAsync(payoutRequest, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 3. Check if treasury has enough liquid balance
            var rebalanceResult = await _treasuryService.RebalanceIfNeededAsync(
                payoutRequest.usdAmount ?? 0,
                ct);

            if (!rebalanceResult.IsSuccess)
            {
                payoutRequest.status = PayoutStatus.PENDING;
                await _payoutRepository.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return Result<PayoutRequestDto>.Failure($"Treasury rebalancing failed: {rebalanceResult.Error}");
            }

            // 4. Debit tokens
            var debitResult = await _tokenBalanceService.DebitAsync(
                payoutRequest.userId,
                payoutRequest.sourceProjectTokenConfigId ?? string.Empty,
                payoutRequest.sourceTokenAmount,
                payoutRequest.holderClass,
                ct);

            if (!debitResult.IsSuccess)
            {
                payoutRequest.status = PayoutStatus.PENDING;
                await _payoutRepository.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return Result<PayoutRequestDto>.Failure($"Token debit failed: {debitResult.Error}");
            }

            // 5. Set status to COMPLETED
            payoutRequest.status = PayoutStatus.COMPLETED;
            payoutRequest.processedAt = DateTime.UtcNow;
            payoutRequest.completedAt = DateTime.UtcNow;

            // 6. Save and return
            await _payoutRepository.UpdateAsync(payoutRequest, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var resultDto = _mapper.Map<PayoutRequestDto>(payoutRequest);
            return Result<PayoutRequestDto>.Success(resultDto);
        }

        public async Task<Result<PayoutRequestDto>> CancelPayoutAsync(string payoutRequestId, CancellationToken ct = default)
        {
            var payoutRequest = await _payoutRepository.GetByIdAsync(payoutRequestId, ct);
            if (payoutRequest == null)
            {
                return Result<PayoutRequestDto>.Failure("Payout request not found.");
            }

            if (payoutRequest.status != PayoutStatus.PENDING)
            {
                return Result<PayoutRequestDto>.Failure($"Cannot cancel payout with status {payoutRequest.status}. Only PENDING payouts can be cancelled.");
            }

            // Unlock tokens
            var unlockResult = await _tokenBalanceService.UnlockAsync(
                payoutRequest.userId,
                payoutRequest.sourceProjectTokenConfigId ?? string.Empty,
                payoutRequest.sourceTokenAmount,
                payoutRequest.holderClass,
                ct);

            if (!unlockResult.IsSuccess)
            {
                return Result<PayoutRequestDto>.Failure($"Failed to unlock tokens: {unlockResult.Error}");
            }

            // Set status to CANCELLED
            payoutRequest.status = PayoutStatus.CANCELLED;

            await _payoutRepository.UpdateAsync(payoutRequest, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var resultDto = _mapper.Map<PayoutRequestDto>(payoutRequest);
            return Result<PayoutRequestDto>.Success(resultDto);
        }

        public async Task<Result<IReadOnlyList<PayoutRequestDto>>> GetPayoutsByUserAsync(string userId, CancellationToken ct = default)
        {
            Expression<Func<PayoutRequest, bool>> predicate = pr => pr.userId == userId;
            var payouts = await _payoutRepository.FindAsync(predicate, ct);

            var dtos = _mapper.Map<List<PayoutRequestDto>>(payouts);
            return Result<IReadOnlyList<PayoutRequestDto>>.Success(dtos);
        }

        public async Task<Result<IReadOnlyList<PayoutRequestDto>>> GetPendingPayoutsAsync(CancellationToken ct = default)
        {
            Expression<Func<PayoutRequest, bool>> predicate = pr => pr.status == PayoutStatus.PENDING;
            var payouts = await _payoutRepository.FindAsync(predicate, ct);

            var dtos = _mapper.Map<List<PayoutRequestDto>>(payouts);
            return Result<IReadOnlyList<PayoutRequestDto>>.Success(dtos);
        }
    }
}
