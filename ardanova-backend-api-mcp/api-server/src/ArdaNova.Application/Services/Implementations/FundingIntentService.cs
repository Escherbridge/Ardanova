namespace ArdaNova.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Stripe;

/// <inheritdoc/>
public sealed class FundingIntentService : IFundingIntentService
{
    private const string Usd = "usd";
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTokenConfig> _tokenConfigRepository;
    private readonly IRepository<FundingIntent> _intentRepository;
    private readonly IRepository<EconomicSettlement> _settlementRepository;
    private readonly IRepository<EconomicOutbox> _outboxRepository;
    private readonly IStripeCheckoutGateway _checkoutGateway;
    private readonly IFundingSettlementReadiness _settlementReadiness;
    private readonly IUnitOfWork _unitOfWork;

    public FundingIntentService(
        IRepository<User> userRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectTokenConfig> tokenConfigRepository,
        IRepository<FundingIntent> intentRepository,
        IRepository<EconomicSettlement> settlementRepository,
        IRepository<EconomicOutbox> outboxRepository,
        IStripeCheckoutGateway checkoutGateway,
        IFundingSettlementReadiness settlementReadiness,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _tokenConfigRepository = tokenConfigRepository;
        _intentRepository = intentRepository;
        _settlementRepository = settlementRepository;
        _outboxRepository = outboxRepository;
        _checkoutGateway = checkoutGateway;
        _settlementReadiness = settlementReadiness;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<Result<FundingCheckoutDto>> CreateCheckoutAsync(
        CreateFundingIntentDto request,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            return Result<FundingCheckoutDto>.Unauthorized("A funding actor is required");
        if (!Guid.TryParse(idempotencyKey, out _))
            return Result<FundingCheckoutDto>.ValidationError("A canonical idempotency key is required");
        if (!UsdMoney.TryParseInvariant(request.Amount, out var amount) || !amount.IsPositive)
            return Result<FundingCheckoutDto>.ValidationError("Funding amount must be a positive USD value with at most two decimal places");
        if (string.IsNullOrWhiteSpace(request.DisclosureVersion) || request.DisclosureVersion.Length > 100)
            return Result<FundingCheckoutDto>.ValidationError("A disclosure version is required");
        if (!_settlementReadiness.IsReady)
            return Result<FundingCheckoutDto>.Conflict(_settlementReadiness.UnavailableReason);

        var actor = await _userRepository.GetByIdAsync(actorId, ct);
        if (actor is null)
            return Result<FundingCheckoutDto>.NotFound("Funding actor not found");

        var config = await _tokenConfigRepository.GetByIdAsync(request.ProjectTokenConfigId, ct);
        if (config is null)
            return Result<FundingCheckoutDto>.NotFound("Project token configuration not found");
        if (config.assetScale is not int assetScale || !FixedScaleAmount.IsSupportedScale(assetScale))
            return Result<FundingCheckoutDto>.ValidationError("Project token configuration has an unsupported asset scale");
        if (string.IsNullOrWhiteSpace(config.assetId))
            return Result<FundingCheckoutDto>.ValidationError("Project token configuration requires a verified asset id before funding checkout");
        if (config.gateStatus != ProjectGateStatus.FUNDING)
            return Result<FundingCheckoutDto>.ValidationError("Funding is not open for this project token configuration");

        var project = await _projectRepository.GetByIdAsync(config.projectId, ct);
        if (project is null)
            return Result<FundingCheckoutDto>.NotFound("Project not found");
        if (!project.commerceEnabled)
            return Result<FundingCheckoutDto>.ValidationError("Commerce is not enabled for this project");
        if (project.status is ProjectStatus.DRAFT or ProjectStatus.CANCELLED)
            return Result<FundingCheckoutDto>.ValidationError("This project is not eligible for funding");

        var existing = await _intentRepository.FindOneAsync(
            intent => intent.funderUserId == actorId && intent.idempotencyKey == idempotencyKey,
            ct);
        if (existing is not null)
            return await ReplayCheckoutAsync(existing, request, amount, actorId, ct);

        var now = DateTime.UtcNow;
        var terms = CreateTermsSnapshot(config, project, amount, request.DisclosureVersion, assetScale);
        var intent = new FundingIntent
        {
            id = Guid.NewGuid().ToString("D"),
            semanticKey = $"funding:{actorId}:{idempotencyKey}",
            idempotencyKey = idempotencyKey,
            status = FundingIntentStatus.DRAFT,
            funderUserId = actorId,
            projectId = project.id,
            projectTokenConfigId = config.id,
            currencyCode = Usd,
            amount = amount.ToDecimal(),
            scale = 2,
            disclosureVersion = request.DisclosureVersion,
            eligibilitySnapshot = CreateEligibilitySnapshot(project, config),
            termsSnapshot = terms,
            termsHash = Hash(terms),
            paymentProvider = "stripe",
            createdAt = now,
            updatedAt = now,
        };

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            await _intentRepository.AddAsync(intent, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            var concurrent = await _intentRepository.FindOneAsync(
                item => item.funderUserId == actorId && item.idempotencyKey == idempotencyKey,
                ct);
            return concurrent is null
                ? throw new InvalidOperationException("Funding intent uniqueness conflict did not expose a durable intent.")
                : await ReplayCheckoutAsync(concurrent, request, amount, actorId, ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return await CreateOrReplayProviderCheckoutAsync(intent, config, ct);
    }

    /// <inheritdoc/>
    public async Task<Result<FundingIntentStatusDto>> GetStatusAsync(
        string intentId,
        string actorId,
        CancellationToken ct = default)
    {
        var intent = await _intentRepository.GetByIdAsync(intentId, ct);
        if (intent is null)
            return Result<FundingIntentStatusDto>.NotFound("Funding intent not found");
        if (!string.Equals(intent.funderUserId, actorId, StringComparison.Ordinal))
            return Result<FundingIntentStatusDto>.Forbidden("You cannot view this funding intent");

        return Result<FundingIntentStatusDto>.Success(ToStatus(intent));
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> RecordPaymentSucceededAsync(
        string providerEventId,
        PaymentIntent paymentIntent,
        CancellationToken ct = default)
    {
        if (!TryGetFundingIntentId(paymentIntent, out var intentId))
            return Result<bool>.Success(false);

        var intent = await _intentRepository.GetByIdAsync(intentId, ct);
        if (intent is null)
            return Result<bool>.Success(false);
        if (!PaymentMatchesIntent(paymentIntent, intent))
            return Result<bool>.ValidationError("Provider payment does not match the immutable funding intent");
        if (intent.verifiedProviderEventId is not null)
            return string.Equals(intent.verifiedProviderEventId, providerEventId, StringComparison.Ordinal)
                && string.Equals(intent.providerPaymentIntentId, paymentIntent.Id, StringComparison.Ordinal)
                ? await ValidateExistingSettlementAsync(intent, providerEventId, ct)
                : Result<bool>.Conflict("Funding intent is already verified by different provider data");

        var now = DateTime.UtcNow;
        var settlement = CreateFundingSettlement(intent, providerEventId, now);
        var outbox = CreateFundingOutbox(settlement.id, now);

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            intent.providerPaymentIntentId = paymentIntent.Id;
            intent.verifiedProviderEventId = providerEventId;
            intent.settlementId = settlement.id;
            intent.status = FundingIntentStatus.SETTLEMENT_PENDING;
            intent.paymentVerifiedAt = now;
            intent.updatedAt = now;
            await _intentRepository.UpdateAsync(intent, ct);
            await _settlementRepository.AddAsync(settlement, ct);
            await _outboxRepository.AddAsync(outbox, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            return await RecoverConcurrentSettlementAsync(intent.id, providerEventId, paymentIntent, ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> RecordPaymentFailedAsync(PaymentIntent paymentIntent, CancellationToken ct = default)
    {
        if (!TryGetFundingIntentId(paymentIntent, out var intentId))
            return Result<bool>.Success(false);

        var intent = await _intentRepository.GetByIdAsync(intentId, ct);
        if (intent is null || intent.status is FundingIntentStatus.PAYMENT_VERIFIED or FundingIntentStatus.SETTLEMENT_PENDING or FundingIntentStatus.SETTLED)
            return Result<bool>.Success(false);
        if (!PaymentMatchesIntent(paymentIntent, intent))
            return Result<bool>.ValidationError("Provider payment does not match the immutable funding intent");

        intent.providerPaymentIntentId = paymentIntent.Id;
        intent.status = FundingIntentStatus.FAILED;
        intent.updatedAt = DateTime.UtcNow;
        await _intentRepository.UpdateAsync(intent, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task<Result<FundingCheckoutDto>> ReplayCheckoutAsync(
        FundingIntent intent,
        CreateFundingIntentDto request,
        UsdMoney amount,
        string actorId,
        CancellationToken ct)
    {
        if (!string.Equals(intent.funderUserId, actorId, StringComparison.Ordinal)
            || !string.Equals(intent.projectTokenConfigId, request.ProjectTokenConfigId, StringComparison.Ordinal)
            || intent.amount != amount.ToDecimal()
            || !string.Equals(intent.disclosureVersion, request.DisclosureVersion, StringComparison.Ordinal))
        {
            return Result<FundingCheckoutDto>.Conflict("Idempotency key was already used for different funding terms");
        }
        if (intent.status is FundingIntentStatus.PAYMENT_VERIFIED or FundingIntentStatus.SETTLEMENT_PENDING or FundingIntentStatus.SETTLED)
            return Result<FundingCheckoutDto>.Conflict("This funding intent already has a verified payment");
        if (intent.status is FundingIntentStatus.CANCELLED or FundingIntentStatus.REJECTED)
            return Result<FundingCheckoutDto>.Conflict("This funding intent is not eligible for checkout replay");

        var config = await _tokenConfigRepository.GetByIdAsync(intent.projectTokenConfigId, ct)
            ?? throw new InvalidOperationException("A durable funding intent lost its project token configuration.");
        if (!FundingTermsMatchConfig(intent.termsSnapshot, config))
            return Result<FundingCheckoutDto>.Conflict("The immutable funding asset terms no longer match the project token configuration");

        if (!string.IsNullOrWhiteSpace(intent.providerCheckoutSessionId))
        {
            var url = await _checkoutGateway.GetUrlAsync(intent.providerCheckoutSessionId, ct);
            return Result<FundingCheckoutDto>.Success(new FundingCheckoutDto { IntentId = intent.id, CheckoutUrl = url });
        }

        return await CreateOrReplayProviderCheckoutAsync(intent, config, ct);
    }

    private async Task<Result<FundingCheckoutDto>> CreateOrReplayProviderCheckoutAsync(
        FundingIntent intent,
        ProjectTokenConfig config,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.assetId)
            || !FixedScaleAmount.IsSupportedScale(config.assetScale))
        {
            return Result<FundingCheckoutDto>.ValidationError(
                "Funding checkout requires a verified asset id and supported asset scale");
        }

        var checkout = await _checkoutGateway.CreateAsync(
            new StripeCheckoutRequest(
                intent.id,
                config.id,
                config.assetName,
                checked((long)(intent.amount * UsdMoney.MinorUnitsPerMajorUnit)),
                intent.currencyCode,
                intent.semanticKey),
            ct);

        intent.providerCheckoutSessionId = checkout.Id;
        intent.status = FundingIntentStatus.AWAITING_PAYMENT;
        intent.updatedAt = DateTime.UtcNow;
        await _intentRepository.UpdateAsync(intent, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<FundingCheckoutDto>.Success(new FundingCheckoutDto
        {
            IntentId = intent.id,
            CheckoutUrl = checkout.Url,
        });
    }

    private static bool TryGetFundingIntentId(PaymentIntent paymentIntent, out string intentId)
    {
        intentId = string.Empty;
        if (paymentIntent.Metadata is null
            || !paymentIntent.Metadata.TryGetValue("fundingIntentId", out var value)
            || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        intentId = value;
        return true;
    }

    private static bool PaymentMatchesIntent(PaymentIntent paymentIntent, FundingIntent intent)
        => !string.IsNullOrWhiteSpace(paymentIntent.Id)
            && string.Equals(paymentIntent.Currency, intent.currencyCode, StringComparison.OrdinalIgnoreCase)
            && paymentIntent.Amount == checked((long)(intent.amount * UsdMoney.MinorUnitsPerMajorUnit));

    private async Task<Result<bool>> RecoverConcurrentSettlementAsync(
        string intentId,
        string providerEventId,
        PaymentIntent paymentIntent,
        CancellationToken ct)
    {
        var persistedIntent = await _intentRepository.GetByIdAsync(intentId, ct);
        if (persistedIntent is null || !PaymentMatchesIntent(paymentIntent, persistedIntent))
            return Result<bool>.Conflict("Funding settlement persistence conflicted with immutable payment terms");
        if (!string.Equals(persistedIntent.verifiedProviderEventId, providerEventId, StringComparison.Ordinal)
            || !string.Equals(persistedIntent.providerPaymentIntentId, paymentIntent.Id, StringComparison.Ordinal))
        {
            return Result<bool>.Conflict("Funding intent is already verified by different provider data");
        }

        return await ValidateExistingSettlementAsync(persistedIntent, providerEventId, ct);
    }

    private async Task<Result<bool>> ValidateExistingSettlementAsync(
        FundingIntent intent,
        string providerEventId,
        CancellationToken ct)
    {
        var idempotencyKey = FundingSettlementKey(intent.id);
        var settlement = await _settlementRepository.FindOneAsync(
            item => item.idempotencyKey == idempotencyKey,
            ct);
        if (settlement is null || !SettlementMatchesIntent(settlement, intent, providerEventId))
            return Result<bool>.Conflict("Funding verification conflicts with the durable settlement decision");
        if (!string.Equals(intent.settlementId, settlement.id, StringComparison.Ordinal))
            return Result<bool>.Conflict("Funding intent is not linked to its durable settlement decision");

        var outbox = await _outboxRepository.FindOneAsync(
            item => item.settlementId == settlement.id,
            ct);
        return outbox is null
            ? Result<bool>.Conflict("Funding settlement is missing its durable outbox record")
            : Result<bool>.Success(true);
    }

    private static EconomicSettlement CreateFundingSettlement(
        FundingIntent intent,
        string providerEventId,
        DateTime now)
        => new()
        {
            id = Guid.NewGuid().ToString("D"),
            kind = EconomicSettlementKind.FUNDING_ALLOCATION,
            status = EconomicSettlementStatus.PENDING_DISPATCH,
            idempotencyKey = FundingSettlementKey(intent.id),
            externalEventId = providerEventId,
            beneficiaryUserId = intent.funderUserId,
            projectId = intent.projectId,
            assetCode = intent.currencyCode.ToUpperInvariant(),
            amount = intent.amount,
            scale = intent.scale,
            termsSnapshot = intent.termsSnapshot,
            version = 1,
            createdAt = now,
            updatedAt = now,
        };

    private static EconomicOutbox CreateFundingOutbox(string settlementId, DateTime now)
        => new()
        {
            id = Guid.NewGuid().ToString("D"),
            settlementId = settlementId,
            status = EconomicOutboxStatus.PENDING,
            payloadVersion = 1,
            attemptCount = 0,
            availableAt = now,
            createdAt = now,
            updatedAt = now,
        };

    private static bool SettlementMatchesIntent(
        EconomicSettlement settlement,
        FundingIntent intent,
        string providerEventId)
        => settlement.kind == EconomicSettlementKind.FUNDING_ALLOCATION
            && string.Equals(settlement.idempotencyKey, FundingSettlementKey(intent.id), StringComparison.Ordinal)
            && string.Equals(settlement.externalEventId, providerEventId, StringComparison.Ordinal)
            && string.Equals(settlement.beneficiaryUserId, intent.funderUserId, StringComparison.Ordinal)
            && string.Equals(settlement.projectId, intent.projectId, StringComparison.Ordinal)
            && string.Equals(settlement.assetCode, intent.currencyCode, StringComparison.OrdinalIgnoreCase)
            && settlement.amount == intent.amount
            && settlement.scale == intent.scale
            && string.Equals(settlement.termsSnapshot, intent.termsSnapshot, StringComparison.Ordinal);

    private static string FundingSettlementKey(string intentId)
        => $"funding-intent:{intentId}";

    private static FundingIntentStatusDto ToStatus(FundingIntent intent)
        => new()
        {
            IntentId = intent.id,
            ProjectTokenConfigId = intent.projectTokenConfigId,
            CurrencyCode = intent.currencyCode.ToUpperInvariant(),
            Amount = intent.amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            Status = intent.status.ToString(),
            PaymentVerifiedAt = intent.paymentVerifiedAt,
        };

    private static string CreateEligibilitySnapshot(Project project, ProjectTokenConfig config)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            projectId = project.id,
            projectStatus = project.status.ToString(),
            commerceEnabled = project.commerceEnabled,
            projectTokenConfigId = config.id,
            gateStatus = config.gateStatus.ToString(),
        });

    private static string CreateTermsSnapshot(
        ProjectTokenConfig config,
        Project project,
        UsdMoney amount,
        string disclosureVersion,
        int assetScale)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            currencyCode = Usd.ToUpperInvariant(),
            amount = amount.ToString(),
            amountScale = 2,
            disclosureVersion,
            projectId = project.id,
            projectTokenConfigId = config.id,
            tokenSymbol = config.unitName,
            assetId = config.assetId,
            tokenScale = assetScale,
        });

    private static bool FundingTermsMatchConfig(string termsSnapshot, ProjectTokenConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.assetId)
            || !FixedScaleAmount.IsSupportedScale(config.assetScale)
            || string.IsNullOrWhiteSpace(termsSnapshot))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(termsSnapshot);
            var root = document.RootElement;
            return root.TryGetProperty("projectTokenConfigId", out var configId)
                && string.Equals(configId.GetString(), config.id, StringComparison.Ordinal)
                && root.TryGetProperty("assetId", out var assetId)
                && string.Equals(assetId.GetString(), config.assetId, StringComparison.Ordinal)
                && root.TryGetProperty("tokenScale", out var tokenScale)
                && tokenScale.TryGetInt32(out var capturedScale)
                && capturedScale == config.assetScale;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
