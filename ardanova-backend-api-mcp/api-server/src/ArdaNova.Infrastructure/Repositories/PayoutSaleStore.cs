namespace ArdaNova.Infrastructure.Repositories;

using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

public sealed class PayoutSaleStore(ArdaNovaDbContext context) : IPayoutSaleStore
{
    public Task<PayoutSaleVoid?> ReadVoidAsync(string userId, string key, CancellationToken ct)
        => context.Set<PayoutSaleVoid>().SingleOrDefaultAsync(v => v.userId == userId && v.requestKey == key, ct);

    public async Task AddVoidAsync(PayoutSaleVoid value, CancellationToken ct)
        => await context.Set<PayoutSaleVoid>().AddAsync(value, ct);

    public async Task<bool> TryAdmitSaleAsync(PayoutRequest payout, string quoteId, CancellationToken ct)
    {
        if (payout.exchangeAdmitBefore is not { } deadlineUtc) return false;
        var requested = new NpgsqlParameter("sale_requested_at", NpgsqlDbType.Timestamp)
            { Value = DateTime.SpecifyKind(payout.requestedAt, DateTimeKind.Unspecified) };
        var deadline = new NpgsqlParameter("sale_admit_before", NpgsqlDbType.Timestamp)
            { Value = DateTime.SpecifyKind(deadlineUtc, DateTimeKind.Unspecified) };
        var written = await context.Database.ExecuteSqlInterpolatedAsync($"""
            WITH input AS (SELECT {requested}::timestamp AS requested_at, {deadline}::timestamp AS admit_before)
            INSERT INTO "PayoutRequest" ("id", "currencyCode", "currencyDecimals", "userId", "sourceProjectTokenConfigId",
                "sourceTokenAmount", "ardaTokenAmount", "usdAmount", "ardaMinorUnits", "usdReferenceMinorUnits", "status",
                "holderClass", "gateStatusAtRequest", "requestedAt", "requestKey", "quoteId", "exchangeAdmitBefore", "exchangeCommandJson")
            SELECT {payout.id}, quote."currencyCode", quote."currencyDecimals", quote."userId", quote."projectTokenConfigId",
                quote."sourceUnitAmount", {payout.ardaTokenAmount}, {payout.usdAmount}, quote."platformMinorUnits",
                quote."usdReferenceMinorUnits", 'PENDING'::"PayoutStatus", quote."holderClass", {payout.gateStatusAtRequest},
                input.requested_at, {payout.requestKey}, quote."id", input.admit_before, quote."commandJson"
            FROM "PayoutSaleQuote" AS quote CROSS JOIN input
            WHERE quote."id" = {quoteId} AND quote."userId" = {payout.userId}
                AND quote."expiresAt" > (clock_timestamp() AT TIME ZONE 'UTC')
                AND input.admit_before > (clock_timestamp() AT TIME ZONE 'UTC')
                AND NOT EXISTS (SELECT 1 FROM "PayoutSaleVoid" AS withdrawn
                    WHERE withdrawn."userId" = {payout.userId} AND withdrawn."requestKey" = {payout.requestKey})
            """, ct);
        return written == 1;
    }

    public Task LockAdmissionAsync(string userId, CancellationToken ct)
        => context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"payout-sale:" + userId}, 0))", ct);

    public Task<ArdaNova.Domain.Models.Entities.PayoutSaleQuote?> ReadQuoteAsync(string userId, string id, CancellationToken ct)
        => context.Set<ArdaNova.Domain.Models.Entities.PayoutSaleQuote>().SingleOrDefaultAsync(q => q.id == id && q.userId == userId, ct);

    public Task<bool> HasQuoteExpiredAsync(string id, CancellationToken ct)
        => context.Set<ArdaNova.Domain.Models.Entities.PayoutSaleQuote>()
            .FromSqlInterpolated($"SELECT * FROM \"PayoutSaleQuote\" WHERE \"id\" = {id} AND \"expiresAt\" <= (clock_timestamp() AT TIME ZONE 'UTC')")
            .AnyAsync(ct);

    public async Task AddQuoteAsync(ArdaNova.Domain.Models.Entities.PayoutSaleQuote quote, CancellationToken ct)
        => await context.Set<ArdaNova.Domain.Models.Entities.PayoutSaleQuote>().AddAsync(quote, ct);

    public Task<PayoutRequest?> ReadRequestAsync(string userId, string key, CancellationToken ct)
        => context.PayoutRequests.SingleOrDefaultAsync(p => p.userId == userId && p.requestKey == key, ct);

    public Task<PayoutRequest?> ReadQuoteSaleAsync(string quoteId, CancellationToken ct)
        => context.PayoutRequests.SingleOrDefaultAsync(p => p.quoteId == quoteId, ct);

    public async Task<PayoutRequest?> LockPayoutAsync(string id, CancellationToken ct)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"PayoutRequest\" WHERE \"id\" = {id} FOR UPDATE", ct);
        foreach (var tracked in context.ChangeTracker.Entries<PayoutRequest>().Where(e => e.Entity.id == id).ToArray())
            tracked.State = EntityState.Detached;
        return await context.PayoutRequests.SingleOrDefaultAsync(p => p.id == id, ct);
    }
}
