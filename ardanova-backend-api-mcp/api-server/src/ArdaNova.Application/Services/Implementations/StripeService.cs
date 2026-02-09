namespace ArdaNova.Application.Services.Implementations;

using System;
using System.Threading;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeCheckoutSessionService = Stripe.Checkout.SessionService;
using StripeAccountService = Stripe.AccountService;
using StripeAccountLinkService = Stripe.AccountLinkService;
using StripeTransferService = Stripe.TransferService;
using StripePaymentIntentService = Stripe.PaymentIntentService;

/// <summary>
/// Stripe SDK wrapper for crowdfunding inflow (Checkout) and payout outflow (Connect).
/// Handles payment webhooks and creates ProjectInvestments, allocates tokens, processes treasury inflows.
/// </summary>
public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly IRepository<ProjectTokenConfig> _configRepo;
    private readonly IRepository<ProjectInvestment> _investmentRepo;
    private readonly IRepository<PayoutRequest> _payoutRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProjectTokenService _projectTokenService;
    private readonly ITokenBalanceService _tokenBalanceService;
    private readonly ITreasuryService _treasuryService;
    private readonly IProjectGateService _projectGateService;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IConfiguration configuration,
        IRepository<ProjectTokenConfig> configRepo,
        IRepository<ProjectInvestment> investmentRepo,
        IRepository<PayoutRequest> payoutRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProjectTokenService projectTokenService,
        ITokenBalanceService tokenBalanceService,
        ITreasuryService treasuryService,
        IProjectGateService projectGateService,
        ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _configRepo = configRepo;
        _investmentRepo = investmentRepo;
        _payoutRepo = payoutRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _projectTokenService = projectTokenService;
        _tokenBalanceService = tokenBalanceService;
        _treasuryService = treasuryService;
        _projectGateService = projectGateService;
        _logger = logger;

        // Set Stripe API key
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    // --- Crowdfunding (Inflow) ---

    public async Task<Result<StripeCheckoutSessionDto>> CreateCheckoutSessionAsync(
        string projectTokenConfigId,
        string userId,
        double usdAmount,
        CancellationToken ct = default)
    {
        try
        {
            // Validate inputs
            if (usdAmount <= 0)
                return Result<StripeCheckoutSessionDto>.ValidationError("USD amount must be positive");

            var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
                return Result<StripeCheckoutSessionDto>.Failure("ProjectTokenConfig not found");

            // Create Stripe Checkout Session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Fund Project: {config.assetName}",
                                Description = $"Investment in {config.assetName}"
                            },
                            UnitAmount = (long)(usdAmount * 100) // Stripe uses cents
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "https://ardanova.com/success",
                CancelUrl = _configuration["Stripe:CancelUrl"] ?? "https://ardanova.com/cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "projectTokenConfigId", projectTokenConfigId },
                    { "userId", userId },
                    { "usdAmount", usdAmount.ToString("F2") }
                }
            };

            var service = new StripeCheckoutSessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            var dto = new StripeCheckoutSessionDto
            {
                SessionId = session.Id,
                SessionUrl = session.Url,
                ProjectTokenConfigId = projectTokenConfigId,
                UsdAmount = usdAmount
            };

            return Result<StripeCheckoutSessionDto>.Success(dto);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session");
            return Result<StripeCheckoutSessionDto>.Failure($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return Result<StripeCheckoutSessionDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<ProjectInvestmentDto>> HandlePaymentSucceededAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        try
        {
            // Retrieve PaymentIntent from Stripe to get metadata
            var paymentIntentService = new StripePaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId, cancellationToken: ct);

            if (paymentIntent == null)
                return Result<ProjectInvestmentDto>.Failure("PaymentIntent not found");

            // Extract metadata
            var metadata = paymentIntent.Metadata;
            if (!metadata.ContainsKey("projectTokenConfigId") ||
                !metadata.ContainsKey("userId") ||
                !metadata.ContainsKey("usdAmount"))
            {
                _logger.LogWarning("PaymentIntent {PaymentIntentId} missing required metadata", paymentIntentId);
                return Result<ProjectInvestmentDto>.Failure("PaymentIntent missing required metadata");
            }

            var projectTokenConfigId = metadata["projectTokenConfigId"];
            var userId = metadata["userId"];
            var usdAmount = double.Parse(metadata["usdAmount"]);

            // Get ProjectTokenConfig
            var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
                return Result<ProjectInvestmentDto>.Failure("ProjectTokenConfig not found");

            // Calculate token allocation based on current project valuation
            // Token value = fundingRaised / totalSupply
            // Token amount = usdAmount / tokenValue = (usdAmount * totalSupply) / fundingRaised
            // But for early investors (fundingRaised is small), we use the fundingGoal as the valuation base
            var valuationBase = config.fundingRaised > 0 ? config.fundingRaised : config.fundingGoal;
            var tokenAmount = (int)((usdAmount / valuationBase) * config.totalSupply);

            // Allocate tokens to investor
            var allocationDto = new CreateInvestorAllocationDto
            {
                UserId = userId,
                UsdAmount = usdAmount,
                TokenAmount = tokenAmount
            };

            var allocationResult = await _projectTokenService.AllocateToInvestorAsync(
                projectTokenConfigId,
                allocationDto,
                ct);

            if (!allocationResult.IsSuccess)
            {
                _logger.LogError("Failed to allocate tokens for investment: {Error}", allocationResult.Error);
                return Result<ProjectInvestmentDto>.Failure(allocationResult.Error);
            }

            // Credit TokenBalance (INVESTOR, not locked initially, gate-enforced liquidity)
            var creditResult = await _tokenBalanceService.CreditAsync(
                userId,
                projectTokenConfigId,
                tokenAmount,
                TokenHolderClass.INVESTOR,
                ct);

            if (!creditResult.IsSuccess)
            {
                _logger.LogError("Failed to credit token balance for investment: {Error}", creditResult.Error);
                return Result<ProjectInvestmentDto>.Failure(creditResult.Error);
            }

            // Process treasury inflow (55/30/15 split)
            var treasuryResult = await _treasuryService.ProcessFundingInflowAsync(
                usdAmount,
                config.projectId,
                ct);

            if (!treasuryResult.IsSuccess)
            {
                _logger.LogError("Failed to process treasury inflow: {Error}", treasuryResult.Error);
                return Result<ProjectInvestmentDto>.Failure(treasuryResult.Error);
            }

            // Update fundingRaised
            config.fundingRaised += usdAmount;
            config.updatedAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config, ct);

            // Create ProjectInvestment record
            var investment = new ProjectInvestment
            {
                id = Guid.NewGuid().ToString(),
                projectTokenConfigId = projectTokenConfigId,
                userId = userId,
                usdAmount = usdAmount,
                tokenAmount = tokenAmount,
                stripePaymentIntentId = paymentIntentId,
                investedAt = DateTime.UtcNow,
                protectionEligible = true,
                protectionPaidOut = false
            };

            await _investmentRepo.AddAsync(investment, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Evaluate Gate 1 (funding goal met?)
            var gateResult = await _projectGateService.EvaluateGate1Async(projectTokenConfigId, ct);
            if (gateResult.IsSuccess && gateResult.Value?.Transitioned == true)
            {
                _logger.LogInformation(
                    "Gate 1 cleared for project {ProjectId} after investment",
                    config.projectId);
            }

            var investmentDto = _mapper.Map<ProjectInvestmentDto>(investment);
            return Result<ProjectInvestmentDto>.Success(investmentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment succeeded for PaymentIntent {PaymentIntentId}", paymentIntentId);
            return Result<ProjectInvestmentDto>.Failure($"Error: {ex.Message}");
        }
    }

    public Task<Result<bool>> HandlePaymentFailedAsync(
        string paymentIntentId,
        string failureReason,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogWarning(
                "Payment failed for PaymentIntent {PaymentIntentId}: {Reason}",
                paymentIntentId,
                failureReason);

            // No side effects on payment failure — just log
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failed for PaymentIntent {PaymentIntentId}", paymentIntentId);
            return Task.FromResult(Result<bool>.Failure($"Error: {ex.Message}"));
        }
    }

    // --- Stripe Connect (Outflow) ---

    public async Task<Result<StripeConnectedAccountDto>> CreateConnectedAccountAsync(
        string userId,
        string email,
        CancellationToken ct = default)
    {
        try
        {
            var options = new AccountCreateOptions
            {
                Type = "express",
                Email = email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId }
                }
            };

            var service = new StripeAccountService();
            var account = await service.CreateAsync(options, cancellationToken: ct);

            // Create account link for onboarding
            var linkOptions = new AccountLinkCreateOptions
            {
                Account = account.Id,
                RefreshUrl = _configuration["Stripe:RefreshUrl"] ?? "https://ardanova.com/connect/refresh",
                ReturnUrl = _configuration["Stripe:ReturnUrl"] ?? "https://ardanova.com/connect/return",
                Type = "account_onboarding"
            };

            var linkService = new StripeAccountLinkService();
            var accountLink = await linkService.CreateAsync(linkOptions, cancellationToken: ct);

            var dto = new StripeConnectedAccountDto
            {
                AccountId = account.Id,
                UserId = userId,
                OnboardingUrl = accountLink.Url,
                Status = account.ChargesEnabled ? "active" : "pending"
            };

            return Result<StripeConnectedAccountDto>.Success(dto);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating connected account for user {UserId}", userId);
            return Result<StripeConnectedAccountDto>.Failure($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connected account for user {UserId}", userId);
            return Result<StripeConnectedAccountDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<StripeTransferDto>> CreatePayoutTransferAsync(
        string payoutRequestId,
        string connectedAccountId,
        double usdAmount,
        CancellationToken ct = default)
    {
        try
        {
            if (usdAmount <= 0)
                return Result<StripeTransferDto>.ValidationError("USD amount must be positive");

            var options = new TransferCreateOptions
            {
                Amount = (long)(usdAmount * 100), // Stripe uses cents
                Currency = "usd",
                Destination = connectedAccountId,
                Metadata = new Dictionary<string, string>
                {
                    { "payoutRequestId", payoutRequestId }
                }
            };

            var service = new StripeTransferService();
            var transfer = await service.CreateAsync(options, cancellationToken: ct);

            // Update PayoutRequest with stripe transfer ID
            var payoutRequest = await _payoutRepo.GetByIdAsync(payoutRequestId, ct);
            if (payoutRequest != null)
            {
                payoutRequest.stripePayoutId = transfer.Id;
                payoutRequest.status = PayoutStatus.PROCESSING;
                payoutRequest.processedAt = DateTime.UtcNow;
                await _payoutRepo.UpdateAsync(payoutRequest, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            var dto = new StripeTransferDto
            {
                TransferId = transfer.Id,
                PayoutRequestId = payoutRequestId,
                UsdAmount = usdAmount,
                Status = transfer.Reversed ? "reversed" : "paid"
            };

            return Result<StripeTransferDto>.Success(dto);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payout transfer for request {PayoutRequestId}", payoutRequestId);
            return Result<StripeTransferDto>.Failure($"Stripe error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payout transfer for request {PayoutRequestId}", payoutRequestId);
            return Result<StripeTransferDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<PayoutRequestDto>> HandlePayoutSucceededAsync(
        string transferId,
        CancellationToken ct = default)
    {
        try
        {
            // Find PayoutRequest by stripePayoutId
            var payoutRequests = await _payoutRepo.FindAsync(
                pr => pr.stripePayoutId == transferId,
                ct);

            var payoutRequest = payoutRequests.FirstOrDefault();
            if (payoutRequest == null)
            {
                _logger.LogWarning("PayoutRequest not found for transfer {TransferId}", transferId);
                return Result<PayoutRequestDto>.Failure("PayoutRequest not found");
            }

            // Update status to COMPLETED
            payoutRequest.status = PayoutStatus.COMPLETED;
            payoutRequest.completedAt = DateTime.UtcNow;
            await _payoutRepo.UpdateAsync(payoutRequest, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var dto = _mapper.Map<PayoutRequestDto>(payoutRequest);
            return Result<PayoutRequestDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payout succeeded for transfer {TransferId}", transferId);
            return Result<PayoutRequestDto>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<PayoutRequestDto>> HandlePayoutFailedAsync(
        string transferId,
        string failureReason,
        CancellationToken ct = default)
    {
        try
        {
            // Find PayoutRequest by stripePayoutId
            var payoutRequests = await _payoutRepo.FindAsync(
                pr => pr.stripePayoutId == transferId,
                ct);

            var payoutRequest = payoutRequests.FirstOrDefault();
            if (payoutRequest == null)
            {
                _logger.LogWarning("PayoutRequest not found for transfer {TransferId}", transferId);
                return Result<PayoutRequestDto>.Failure("PayoutRequest not found");
            }

            // Update status to FAILED and unlock tokens
            payoutRequest.status = PayoutStatus.FAILED;
            payoutRequest.failureReason = failureReason;
            payoutRequest.completedAt = DateTime.UtcNow;
            await _payoutRepo.UpdateAsync(payoutRequest, ct);

            // Unlock tokens
            if (payoutRequest.sourceProjectTokenConfigId != null)
            {
                await _tokenBalanceService.UnlockAsync(
                    payoutRequest.userId,
                    payoutRequest.sourceProjectTokenConfigId,
                    payoutRequest.sourceTokenAmount,
                    payoutRequest.holderClass,
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            var dto = _mapper.Map<PayoutRequestDto>(payoutRequest);
            return Result<PayoutRequestDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payout failed for transfer {TransferId}", transferId);
            return Result<PayoutRequestDto>.Failure($"Error: {ex.Message}");
        }
    }
}
