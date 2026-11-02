namespace ArdaNova.Application.Services.Implementations;

using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

public partial class PayoutService
{
    public async Task<Result<UnitSaleQuote>> CreateSaleQuoteAsync(string userId, CreateUnitSaleQuote request, CancellationToken ct = default)
    {
        if (_saleStore is null)
            return Result<UnitSaleQuote>.Conflict("Sale quotes are unavailable on this deployment.");
        if (request.SourceUnitAmount <= 0 || !Enum.IsDefined(request.HolderClass))
            return Result<UnitSaleQuote>.ValidationError("Choose a valid quantity and holding to sell.");
        var assessed = await AssessSaleAsync(userId, request.ProjectTokenConfigId, request.HolderClass, request.SourceUnitAmount, ct);
        if (assessed.IsFailure || assessed.Value is null)
            return Carry<SaleAssessment, UnitSaleQuote>(assessed);
        var ready = assessed.Value;
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(5);
        var command = BuildSaleCommand(ready.Config, ready.Rate, ready.Priced);
        var quote = new PayoutSaleQuote
        {
            id = Guid.NewGuid().ToString("N"), userId = userId,
            currencyCode = "ARDA", currencyDecimals = Money.MinorUnitDigits,
            projectTokenConfigId = request.ProjectTokenConfigId, holderClass = request.HolderClass,
            sourceUnitAmount = request.SourceUnitAmount, platformMinorUnits = ready.Priced.PlatformMinorUnits,
            usdReferenceMinorUnits = ready.Priced.UsdReferenceMinorUnits,
            commandJson = JsonSerializer.Serialize(command), createdAt = now.UtcDateTime, expiresAt = expires.UtcDateTime,
        };
        await _saleStore.AddQuoteAsync(quote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UnitSaleQuote>.Success(new(request.ProjectTokenConfigId, ready.Config.unitName,
            request.SourceUnitAmount, quote.platformMinorUnits, quote.usdReferenceMinorUnits,
            ready.SellableUnitCount, ready.Restriction.RestrictedUntil, true, "Ready to sell.", quote.id, expires, quote.currencyCode, quote.currencyDecimals));
    }

    public async Task<Result<PayoutRequestDto>> AcceptSaleQuoteAsync(string userId, AcceptUnitSaleQuote request, CancellationToken ct = default)
    {
        if (_saleStore is null || _exchangeNode is null)
            return Result<PayoutRequestDto>.Conflict(NoExchangeConfigured);
        if (string.IsNullOrWhiteSpace(userId) || !IsSaleKey(request.RequestKey)
            || !Guid.TryParseExact(request.QuoteId, "N", out _))
            return Result<PayoutRequestDto>.ValidationError("Use a valid sale quote and request key.");

        PayoutRequest payout;
        Guid avatar;
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _saleStore.LockAdmissionAsync(userId, ct);
            var existing = await _saleStore.ReadRequestAsync(userId, request.RequestKey, ct);
            if (existing is not null)
            {
                await _unitOfWork.CommitTransactionAsync(ct);
                if (existing.quoteId != request.QuoteId)
                    return Result<PayoutRequestDto>.Conflict("This key already names another sale. Keep its original request.").WithCode("REQUEST_KEY_IN_USE");
                return await ProcessPayoutAsync(existing.id, ct);
            }
            var withdrawn = await _saleStore.ReadVoidAsync(userId, request.RequestKey, ct);
            if (withdrawn is not null)
            {
                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<PayoutRequestDto>.Conflict("This request key was withdrawn. Nothing can start under it.")
                    .WithCode(withdrawn.quoteId == request.QuoteId ? "REQUEST_VOIDED" : "REQUEST_KEY_IN_USE");
            }
            var quote = await _saleStore.ReadQuoteAsync(userId, request.QuoteId, ct);
            if (quote is null)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<PayoutRequestDto>.Conflict("This quote is no longer available. Request a fresh quote.");
            }
            if (await _saleStore.ReadQuoteSaleAsync(quote.id, ct) is not null)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<PayoutRequestDto>.Conflict("This quote already names a sale. Resume its original request.").WithCode("REQUEST_KEY_IN_USE");
            }
            if (quote.expiresAt <= _timeProvider.GetUtcNow().UtcDateTime)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<PayoutRequestDto>.Conflict("This quote expired before admission. Resolve this request before starting another.");
            }
            var assessed = await AssessSaleAsync(userId, quote.projectTokenConfigId, quote.holderClass, quote.sourceUnitAmount, ct);
            if (assessed.IsFailure || assessed.Value is null)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Carry<SaleAssessment, PayoutRequestDto>(assessed);
            }
            var ready = assessed.Value;
            if (quote.expiresAt <= _timeProvider.GetUtcNow().UtcDateTime || await _saleStore.HasQuoteExpiredAsync(quote.id, ct))
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<PayoutRequestDto>.Conflict("The quote expired during account checks. Resume this request to resolve it.");
            }
            var locked = await _tokenBalanceService.LockAsync(userId, quote.projectTokenConfigId, quote.sourceUnitAmount, quote.holderClass, ct);
            if (locked.IsFailure || locked.Value is null
                || locked.Value.Balance - locked.Value.LockedBalance < ready.Restriction.RestrictedUnits)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _unitOfWork.ClearTrackedChanges();
                return Result<PayoutRequestDto>.Conflict("Those units are already committed or held. Nothing was started.");
            }
            if (await _saleStore.HasQuoteExpiredAsync(quote.id, ct))
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _unitOfWork.ClearTrackedChanges();
                return Result<PayoutRequestDto>.Conflict("The quote expired before admission. Resume this request to resolve it.");
            }
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            payout = new PayoutRequest
            {
                id = Guid.NewGuid().ToString(), userId = userId, currencyCode = quote.currencyCode, currencyDecimals = quote.currencyDecimals, sourceProjectTokenConfigId = quote.projectTokenConfigId,
                sourceTokenAmount = quote.sourceUnitAmount, holderClass = quote.holderClass,
                gateStatusAtRequest = ready.Config.gateStatus, status = PayoutStatus.PENDING, requestedAt = now,
                ardaMinorUnits = quote.platformMinorUnits, usdReferenceMinorUnits = quote.usdReferenceMinorUnits,
                ardaTokenAmount = ToWholeUnitsForDisplay(quote.platformMinorUnits),
                usdAmount = Money.ToDisplayDouble(Money.FromMinorUnits(quote.usdReferenceMinorUnits)),
                requestKey = request.RequestKey, quoteId = quote.id,
                exchangeCommandJson = quote.commandJson, exchangeAdmitBefore = _admission.AdmitBefore(now).UtcDateTime,
            };
            if (!await _saleStore.TryAdmitSaleAsync(payout, quote.id, ct))
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _unitOfWork.ClearTrackedChanges();
                return Result<PayoutRequestDto>.Conflict("The quote expired before admission. Resolve this saved request before starting another.");
            }
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            avatar = ready.AvatarId;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
        var command = JsonSerializer.Deserialize<ArdaNovaExchangeCommand>(payout.exchangeCommandJson!)!;
        var result = await _exchangeNode.ExchangeAsync(avatar,
            command with { AdmitBefore = new DateTimeOffset(DateTime.SpecifyKind(payout.exchangeAdmitBefore!.Value, DateTimeKind.Utc)) },
            payout.ExchangeIdempotencyKey, ct);
        return await ApplyExchangeOutcomeAsync(payout, result, ct);
    }

    public async Task<Result<SaleRequestResolution>> VoidSaleRequestAsync(string userId, AcceptUnitSaleQuote request, CancellationToken ct = default)
    {
        if (_saleStore is null || string.IsNullOrWhiteSpace(userId) || !IsSaleKey(request.RequestKey)
            || !Guid.TryParseExact(request.QuoteId, "N", out _))
            return Result<SaleRequestResolution>.ValidationError("A valid saved sale request is required.");
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _saleStore.LockAdmissionAsync(userId, ct);
            var sale = await _saleStore.ReadRequestAsync(userId, request.RequestKey, ct);
            if (sale is not null)
            {
                await _unitOfWork.CommitTransactionAsync(ct);
                if (sale.quoteId != request.QuoteId)
                    return Result<SaleRequestResolution>.Conflict("This key names another sale.").WithCode("REQUEST_KEY_IN_USE");
                return Result<SaleRequestResolution>.Success(new("revealed", _mapper.Map<PayoutRequestDto>(sale)));
            }
            var withdrawn = await _saleStore.ReadVoidAsync(userId, request.RequestKey, ct);
            if (withdrawn is not null && withdrawn.quoteId != request.QuoteId)
            {
                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<SaleRequestResolution>.Conflict("This key names another request.").WithCode("REQUEST_KEY_IN_USE");
            }
            if (withdrawn is null)
            {
                await _saleStore.AddVoidAsync(new PayoutSaleVoid
                {
                    id = Guid.NewGuid().ToString("N"), userId = userId, requestKey = request.RequestKey,
                    quoteId = request.QuoteId, createdAt = _timeProvider.GetUtcNow().UtcDateTime,
                }, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            await _unitOfWork.CommitTransactionAsync(ct);
            return Result<SaleRequestResolution>.Success(new("voided"));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }

    public async Task<Result<PayoutRequestDto>> ReadSaleAsync(string userId, string id, CancellationToken ct = default)
    {
        var payout = await _payoutRepository.GetByIdAsync(id, ct);
        if (payout is null || payout.userId != userId)
            return Result<PayoutRequestDto>.NotFound("That sale could not be found.");
        return await ProcessPayoutAsync(id, ct);
    }

    private static bool IsSaleKey(string? key)
        => key is { Length: > 0 and <= MaximumRequestKeyLength }
            && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or ':' or '-');

    private static ArdaNovaExchangeCommand BuildSaleCommand(ProjectTokenConfig config, DeclaredProjectUnitRate rate, PricedSale priced)
        => new(config.assetChain!, false, config.treasuryReference!, config.assetId!, priced.PlatformMinorUnits,
            priced.ProjectUnitBaseUnits, priced.ReservePartition, rate, Purpose: ExchangePurpose);

    private bool SaleAbsenceIsProof(PayoutRequest payout, DateTime readStartedAt)
        => !payout.IsNodeSettled && string.IsNullOrWhiteSpace(payout.SellOperationId)
            && _admission.NodeEnforcesAdmitBefore && payout.exchangeAdmitBefore is { } deadline
            && readStartedAt >= deadline.Add(_admission.MaximumClockSkew).Add(ExchangeAdmissionPolicy.MinimumMargin);
}
