namespace ArdaNova.Application.Services.Implementations;

using System;
using System.Globalization;
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
    private readonly IStripePaymentIntentGateway _paymentIntentGateway;
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
        IStripePaymentIntentGateway paymentIntentGateway,
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
        _paymentIntentGateway = paymentIntentGateway;
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

            var fundingMetadata = new Dictionary<string, string>
            {
                { "projectTokenConfigId", projectTokenConfigId },
                { "userId", userId },
                { "usdAmount", usdAmount.ToString("F2", CultureInfo.InvariantCulture) }
            };

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
                Metadata = fundingMetadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>(fundingMetadata)
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
        var priorInvestment = await _investmentRepo.FindOneAsync(
            investment => investment.stripePaymentIntentId == paymentIntentId,
            ct);
        if (priorInvestment is not null)
            return Result<ProjectInvestmentDto>.Success(_mapper.Map<ProjectInvestmentDto>(priorInvestment));

        var paymentIntent = await _paymentIntentGateway.GetAsync(paymentIntentId, ct);
        if (paymentIntent == null)
            return Result<ProjectInvestmentDto>.Failure("PaymentIntent not found");

        var metadata = paymentIntent.Metadata;
        if (metadata is null
            || !metadata.ContainsKey("projectTokenConfigId")
            || !metadata.ContainsKey("userId")
            || !metadata.ContainsKey("usdAmount"))
        {
            _logger.LogWarning("PaymentIntent {PaymentIntentId} missing required metadata", paymentIntentId);
            return Result<ProjectInvestmentDto>.Failure("PaymentIntent missing required metadata");
        }

        if (!double.TryParse(
                metadata["usdAmount"],
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var usdAmount)
            || usdAmount <= 0)
        {
            return Result<ProjectInvestmentDto>.Failure("PaymentIntent has an invalid funding amount");
        }

        var projectTokenConfigId = metadata["projectTokenConfigId"];
        var userId = metadata["userId"];
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<ProjectInvestmentDto>.Failure("ProjectTokenConfig not found");

        var valuationBase = config.fundingRaised > 0 ? config.fundingRaised : config.fundingGoal;
        if (valuationBase <= 0)
            return Result<ProjectInvestmentDto>.Failure("ProjectTokenConfig has an invalid funding valuation");

        var tokenAmount = (int)((usdAmount / valuationBase) * config.totalSupply);
        if (tokenAmount <= 0)
            return Result<ProjectInvestmentDto>.Failure("Funding amount is below the minimum token allocation");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var allocationDto = new CreateInvestorAllocationDto
            {
                UserId = userId,
                UsdAmount = usdAmount,
                TokenAmount = tokenAmount
            };
            var allocationResult = await _projectTokenService.AllocateToInvestorAsync(
                projectTokenConfigId,
                allocationDto,
                ct,
                stripePaymentIntentId: paymentIntentId);
            if (!allocationResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return Result<ProjectInvestmentDto>.Failure(allocationResult.Error ?? "Investor allocation failed");
            }

            var creditResult = await _tokenBalanceService.CreditAsync(
                userId,
                projectTokenConfigId,
                tokenAmount,
                TokenHolderClass.INVESTOR,
                ct);
            if (!creditResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return Result<ProjectInvestmentDto>.Failure(creditResult.Error ?? "Investor balance credit failed");
            }

            var treasuryResult = await _treasuryService.ProcessFundingInflowAsync(
                usdAmount,
                config.projectId,
                ct);
            if (!treasuryResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return Result<ProjectInvestmentDto>.Failure(treasuryResult.Error ?? "Treasury funding inflow failed");
            }

            var gateResult = await _projectGateService.EvaluateGate1Async(projectTokenConfigId, ct);
            if (gateResult.IsSuccess && gateResult.Value?.Transitioned == true)
            {
                _logger.LogInformation(
                    "Gate 1 cleared for project {ProjectId} after investment",
                    config.projectId);
            }

            var investment = await _investmentRepo.FindOneAsync(
                item => item.stripePaymentIntentId == paymentIntentId,
                ct)
                ?? throw new InvalidOperationException(
                    $"Investor allocation did not persist Stripe payment intent '{paymentIntentId}'.");

            await _unitOfWork.CommitTransactionAsync(ct);
            return Result<ProjectInvestmentDto>.Success(_mapper.Map<ProjectInvestmentDto>(investment));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
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
