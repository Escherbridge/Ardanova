namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

public sealed record CreateUnitSaleQuote(string ProjectTokenConfigId, TokenHolderClass HolderClass, int SourceUnitAmount);
public sealed record AcceptUnitSaleQuote(string RequestKey, string QuoteId);

public sealed record SaleRequestResolution(string Outcome, PayoutRequestDto? Sale = null);

public interface IQuotedSaleService
{
    Task<Result<SaleRequestResolution>> VoidSaleRequestAsync(string userId, AcceptUnitSaleQuote request, CancellationToken ct = default);
    Task<Result<ArdaNova.Application.Services.Interfaces.UnitSaleQuote>> CreateSaleQuoteAsync(string userId, CreateUnitSaleQuote request, CancellationToken ct = default);
    Task<Result<PayoutRequestDto>> AcceptSaleQuoteAsync(string userId, AcceptUnitSaleQuote request, CancellationToken ct = default);
    Task<Result<PayoutRequestDto>> ReadSaleAsync(string userId, string id, CancellationToken ct = default);
}

/// <summary>Transaction-scoped admission and payout row locks; see Implementations/AGENTS.md.</summary>
public interface IPayoutSaleStore
{
    Task<PayoutSaleVoid?> ReadVoidAsync(string userId, string key, CancellationToken ct);
    Task AddVoidAsync(PayoutSaleVoid value, CancellationToken ct);
    Task<bool> TryAdmitSaleAsync(PayoutRequest payout, string quoteId, CancellationToken ct);
    Task LockAdmissionAsync(string userId, CancellationToken ct);
    Task<ArdaNova.Domain.Models.Entities.PayoutSaleQuote?> ReadQuoteAsync(string userId, string id, CancellationToken ct);
    Task<bool> HasQuoteExpiredAsync(string id, CancellationToken ct);
    Task AddQuoteAsync(ArdaNova.Domain.Models.Entities.PayoutSaleQuote quote, CancellationToken ct);
    Task<PayoutRequest?> ReadRequestAsync(string userId, string key, CancellationToken ct);
    Task<PayoutRequest?> ReadQuoteSaleAsync(string quoteId, CancellationToken ct);
    Task<PayoutRequest?> LockPayoutAsync(string id, CancellationToken ct);
}
