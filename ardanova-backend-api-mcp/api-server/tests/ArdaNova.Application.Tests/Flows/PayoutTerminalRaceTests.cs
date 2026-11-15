namespace ArdaNova.Application.Tests.Flows;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using ArdaNova.Infrastructure.UnitOfWork;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Npgsql;
using Npgsql.NameTranslation;

[Trait("Category", "Database")]
public sealed class PayoutTerminalRaceTests : IAsyncLifetime
{
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _source;
    public async Task InitializeAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
            { Database = "ardanova_payout_" + Guid.NewGuid().ToString("N")[..12] };
        var enums = typeof(PayoutStatus).Assembly.GetTypes().Where(t => t.IsEnum && t.Namespace == "ArdaNova.Domain.Models.Enums").ToList();
        var translator = new NpgsqlNullNameTranslator();
        var source = new NpgsqlDataSourceBuilder(connection.ConnectionString);
        foreach (var type in enums) source.MapEnum(type, type.Name, translator);
        _source = source.Build();
        _options = new DbContextOptionsBuilder<ArdaNovaDbContext>().UseNpgsql(_source, pg =>
        {
            foreach (var type in enums) pg.MapEnum(type, type.Name, "public", translator);
        }).ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)).Options;
        await using var db = NewContext();
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new User { id = "u", email = "payout@example.test", createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow });
        db.PayoutRequests.Add(new PayoutRequest { id = "p", userId = "u", sourceTokenAmount = 1,
            holderClass = TokenHolderClass.CONTRIBUTOR, status = PayoutStatus.PROCESSING,
            requestedAt = DateTime.UtcNow.AddMinutes(-10), exchangeAdmitBefore = DateTime.UtcNow.AddMinutes(-5), ardaMinorUnits = 100 });
        db.Projects.Add(new Project { id = "project", slug = "payout-project", title = "Payout project", createdById = "u",
            createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow });
        db.ProjectTokenConfigs.Add(new ProjectTokenConfig { id = "config", projectId = "project", unitName = "Units",
            createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_options is null) return;
        await using (var db = NewContext()) await db.Database.EnsureDeletedAsync();
        if (_source is not null) await _source.DisposeAsync();
    }

    [PostgresFact]
    public Task TwoReconcilersReleaseUnitsOnce() => RaceAsync(cancelSecond: false);

    [PostgresFact]
    public Task CancellationAndReconciliationReleaseUnitsOnce() => RaceAsync(cancelSecond: true);

    [PostgresFact]
    public async Task AdmissionCopiesFrozenQuoteAndChecksClockAtTheInsertStatement()
    {
        await using var db = NewContext();
        db.PayoutSaleQuotes.Add(new PayoutSaleQuote { id = "quote", userId = "u", projectTokenConfigId = "config",
            sourceUnitAmount = 7, platformMinorUnits = 123, usdReferenceMinorUnits = 101, commandJson = "frozen-command",
            holderClass = TokenHolderClass.INVESTOR, createdAt = DateTime.UtcNow.AddMinutes(-1), expiresAt = DateTime.UtcNow.AddMinutes(5) });
        await db.SaveChangesAsync();
        var store = new PayoutSaleStore(db);
        var stamp = DateTime.UtcNow;
        stamp = new DateTime(stamp.Ticks - stamp.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        var payout = new PayoutRequest { id = "admitted", userId = "u", requestKey = "key", sourceTokenAmount = 999,
            ardaMinorUnits = 999, exchangeCommandJson = "untrusted-command", currencyCode = "USD", currencyDecimals = 6,
            requestedAt = stamp, exchangeAdmitBefore = stamp.AddMinutes(4) };
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL TIME ZONE 'America/Denver'");
            await store.LockAdmissionAsync("u", CancellationToken.None);
            (await store.TryAdmitSaleAsync(payout, "quote", CancellationToken.None)).Should().BeTrue();
            await tx.CommitAsync();
        }
        var stored = await db.PayoutRequests.AsNoTracking().SingleAsync(p => p.id == "admitted");
        stored.sourceTokenAmount.Should().Be(7);
        stored.ardaMinorUnits.Should().Be(123);
        stored.exchangeCommandJson.Should().Be("frozen-command");
        stored.currencyCode.Should().Be("ARDA");
        stored.currencyDecimals.Should().Be(2);
        stored.exchangeAdmitBefore.Should().Be(payout.exchangeAdmitBefore);
        stored.requestedAt.Should().Be(stamp);
        stored.exchangeAdmitBefore!.Value.Kind.Should().Be(DateTimeKind.Utc);

        db.PayoutSaleQuotes.Add(new PayoutSaleQuote { id = "late", userId = "u", projectTokenConfigId = "config",
            sourceUnitAmount = 1, platformMinorUnits = 100, commandJson = "late-command",
            createdAt = DateTime.UtcNow.AddMinutes(-1), expiresAt = DateTime.UtcNow.AddMinutes(5) });
        await db.SaveChangesAsync();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL TIME ZONE 'America/Denver'");
            await store.LockAdmissionAsync("u", CancellationToken.None);
            await db.Database.ExecuteSqlRawAsync("UPDATE \"PayoutSaleQuote\" SET \"expiresAt\" = (clock_timestamp() AT TIME ZONE 'UTC') + interval '50 milliseconds' WHERE \"id\" = 'late'");
            await db.Database.ExecuteSqlRawAsync("SELECT pg_sleep(0.1)");
            payout.id = "not-admitted";
            payout.requestKey = "late-key";
            (await store.TryAdmitSaleAsync(payout, "late", CancellationToken.None)).Should().BeFalse();
            await tx.RollbackAsync();
        }
        (await db.PayoutRequests.AnyAsync(p => p.id == "not-admitted")).Should().BeFalse();
    }

    private async Task RaceAsync(bool cancelSecond)
    {
        var readers = 0;
        var released = 0;
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var node = new Mock<IAzoaExchangeNode>();
        node.Setup(n => n.GetReceiptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(async () =>
        {
            if (Interlocked.Increment(ref readers) == 2) bothRead.TrySetResult();
            await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(15));
            return Result<ArdaNovaExchangeReceipt>.NotFound("absent");
        });
        var balance = new Mock<ITokenBalanceService>();
        balance.Setup(b => b.UnlockAsync("u", "", 1, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
            .Returns(() => { Interlocked.Increment(ref released); return Task.FromResult(Result<TokenBalanceDto>.Success(new TokenBalanceDto())); });
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns((PayoutRequest row) => new PayoutRequestDto { Id = row.id, Status = row.status });
        await using var a = NewContext();
        await using var b = NewContext();
        PayoutService Service(ArdaNovaDbContext db) => new(new Repository<PayoutRequest>(db),
            new Repository<ProjectTokenConfig>(db), new UnitOfWork(db), mapper.Object, balance.Object,
            Mock.Of<IExchangeService>(), Mock.Of<IAzoaValueEligibilityService>(), node.Object,
            saleStore: new PayoutSaleStore(db), admission: new() { NodeEnforcesAdmitBefore = true });
        var first = Service(a).ProcessPayoutAsync("p");
        var secondService = Service(b);
        var second = cancelSecond ? secondService.CancelPayoutAsync("p") : secondService.ProcessPayoutAsync("p");
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(30));
        released.Should().Be(1);
        await using var check = NewContext();
        var row = await check.PayoutRequests.SingleAsync(p => p.id == "p");
        row.status.Should().BeOneOf(PayoutStatus.FAILED, PayoutStatus.CANCELLED);
    }

    private ArdaNovaDbContext NewContext() => new(_options!);
}
