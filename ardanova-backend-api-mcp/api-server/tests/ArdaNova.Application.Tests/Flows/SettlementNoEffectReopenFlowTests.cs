namespace ArdaNova.Application.Tests.Flows;

using System.Net;
using System.Text;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Npgsql.NameTranslation;
using Xunit.Abstractions;

/// <summary>
/// Owner decision F29: a node receipt 404 is proof of no effect, so the settlement redispatches
/// under the same key. Real outbox store on a throwaway Postgres database, real gateway and exchange
/// client; only the node's HTTP edge and its account-readiness read are faked.
/// </summary>
/// <remarks>
/// Needs <c>ARDANOVA_TEST_POSTGRES</c> (a connection string without a database name); without it each
/// test logs a skip and returns, the same gate <c>AzoaGoldenPathE2ETests</c> uses.
/// </remarks>
[Trait("Category", "Database")]
public sealed class SettlementNoEffectReopenFlowTests : IAsyncLifetime
{
    private const string ConnectionEnv = "ARDANOVA_TEST_POSTGRES";
    private const string SettlementId = "settlement-f29";
    private const string OutboxId = "outbox-f29";
    private const string IdempotencyKey = "avatar-f29:funding-intent:intent-f29";
    private const string ReceiptPath = "/api/exchange/receipt/" + IdempotencyKey;

    private static readonly Guid AvatarId = Guid.Parse("6f1d7c1e-2c55-4c2a-9a5e-0f6f2b1d3a11");
    private static readonly string ExchangePath = "/api/exchange/" + AvatarId.ToString("D");

    private readonly ITestOutputHelper _output;
    private readonly FakeNode _node = new();
    private readonly RecordingAlerts _alerts = new();
    private readonly string? _serverConnection = Environment.GetEnvironmentVariable(ConnectionEnv);
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    public SettlementNoEffectReopenFlowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_serverConnection))
            return;

        var connection = new NpgsqlConnectionStringBuilder(_serverConnection)
        {
            Database = "ardanova_f29_" + Guid.NewGuid().ToString("N")[..12],
        };
        var translator = new NpgsqlNullNameTranslator();
        var enumTypes = typeof(EconomicOutboxStatus).Assembly.GetTypes()
            .Where(type => type.IsEnum && type.Namespace == "ArdaNova.Domain.Models.Enums")
            .ToList();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connection.ConnectionString);
        foreach (var enumType in enumTypes)
            dataSourceBuilder.MapEnum(enumType, enumType.Name, translator);
        _dataSource = dataSourceBuilder.Build();
        _options = new DbContextOptionsBuilder<ArdaNovaDbContext>()
            .UseNpgsql(_dataSource, npgsql =>
            {
                foreach (var enumType in enumTypes)
                    npgsql.MapEnum(enumType, enumType.Name, "public", translator);
            })
            .Options;

        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_options is null)
            return;

        await using (var context = NewContext())
            await context.Database.EnsureDeletedAsync();
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
    }

    [Fact]
    public async Task DeadLeasedEntryWithNodeReceipt404_RedispatchesTheSameIntentUnderTheSameKeyAndSettles()
    {
        if (Skipped())
            return;

        await SeedPendingSettlementAsync();
        _node.OnExchange(HttpStatusCode.ServiceUnavailable, NodeFailure("RAIL_UNAVAILABLE"));
        (await DispatchOnceAsync()).Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);

        // A reconciliation worker claims the row and dies mid-read; its lease outlives nobody.
        await using (var crashed = NewContext())
        {
            var abandoned = await new EconomicOutboxLeaseStore(crashed).TryClaimAsync(
                EconomicOutboxClaimKind.Reconciliation, DateTime.UtcNow, TimeSpan.FromMinutes(5));
            abandoned.Should().NotBeNull();
        }
        await AgePastQuietWindowAsync(expireLease: true);
        var attemptsBeforeReopen = (await OutboxAsync()).attemptCount;

        _node.OnReceipt(HttpStatusCode.NotFound, NodeFailure("NOT_FOUND"));
        (await ReconcileOnceAsync()).Outcome.Should().Be(EconomicOutboxRecordedOutcome.Retry);

        var reopened = await OutboxAsync();
        reopened.status.Should().Be(EconomicOutboxStatus.PENDING);
        reopened.reconciliationRequiredAt.Should().BeNull();
        reopened.failureCode.Should().Be(EconomicSettlementFailureCodes.ExchangeNeverLanded);
        reopened.failureDetail.Should().EndWith("[noEffectReopens=1]");
        reopened.attemptCount.Should().Be(attemptsBeforeReopen, "a reopen is bookkeeping, not a dispatch");
        (await SettlementAsync()).status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH);

        var operationId = Guid.NewGuid();
        _node.OnExchange(HttpStatusCode.OK, AcceptedReceipt(operationId));
        (await DispatchOnceAsync()).Outcome.Should().Be(EconomicOutboxRecordedOutcome.Accepted);

        var settled = await SettlementAsync();
        settled.status.Should().Be(EconomicSettlementStatus.SUBMITTED);
        settled.azoaOperationId.Should().Be(operationId.ToString("D"));
        (await OutboxAsync()).status.Should().Be(EconomicOutboxStatus.SUBMITTED);

        var dispatches = _node.Requests.Where(request => request.Path == ExchangePath).ToList();
        dispatches.Should().HaveCount(2);
        dispatches.Select(request => request.IdempotencyKey).Should().AllBe(IdempotencyKey);
        dispatches[1].Body.Should().Be(dispatches[0].Body, "a redispatch replays the stored intent byte for byte");
    }

    [Fact]
    public async Task NodeReceipt404WhileTheDispatchLeaseCouldStillBeAlive_IsReadAgainLaterAndNotReopened()
    {
        if (Skipped())
            return;

        await SeedPendingSettlementAsync();
        _node.OnExchange(HttpStatusCode.GatewayTimeout, NodeFailure("TIMEOUT"));
        await DispatchOnceAsync();
        var parked = await OutboxAsync();

        _node.OnReceipt(HttpStatusCode.NotFound, NodeFailure("NOT_FOUND"));
        await ReconcileOnceAsync();

        var deferred = await OutboxAsync();
        deferred.status.Should().Be(EconomicOutboxStatus.AWAITING_RECONCILIATION);
        deferred.failureCode.Should().Be(EconomicSettlementFailureCodes.ExchangeAbsenceUnsettled);
        deferred.attemptCount.Should().Be(parked.attemptCount);
        deferred.reconciliationRequiredAt.Should().Be(parked.reconciliationRequiredAt);
        deferred.availableAt.Should().BeOnOrAfter(
            parked.reconciliationRequiredAt!.Value.Add(EconomicOutboxDispatchService.NoEffectQuietWindow));
        (await SettlementAsync()).status.Should().Be(EconomicSettlementStatus.AWAITING_RECONCILIATION);
    }

    [Fact]
    public async Task SettlementThatKeepsReading404_ParksWithAttentionOnceItsReopensAreSpent()
    {
        if (Skipped())
            return;

        await SeedPendingSettlementAsync();
        _node.OnExchange(HttpStatusCode.ServiceUnavailable, NodeFailure("RAIL_UNAVAILABLE"));
        _node.OnReceipt(HttpStatusCode.NotFound, NodeFailure("NOT_FOUND"));

        for (var cycle = 1; cycle <= EconomicOutboxDispatchService.MaximumNoEffectReopens; cycle++)
        {
            await DispatchOnceAsync();
            await AgePastQuietWindowAsync(expireLease: false);
            await ReconcileOnceAsync();
            (await OutboxAsync()).status.Should().Be(EconomicOutboxStatus.PENDING, $"reopen {cycle} is still allowed");
        }

        await DispatchOnceAsync();
        await AgePastQuietWindowAsync(expireLease: false);
        await ReconcileOnceAsync();

        var outbox = await OutboxAsync();
        outbox.status.Should().Be(EconomicOutboxStatus.AWAITING_RECONCILIATION);
        outbox.failureCode.Should().Be(EconomicSettlementFailureCodes.ReconciliationAttentionRequired);
        outbox.availableAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(30));
        outbox.attemptCount.Should().Be(EconomicOutboxDispatchService.MaximumNoEffectReopens + 2,
            "six real dispatches plus the parking read; no reopen spent an attempt");
        var settlement = await SettlementAsync();
        settlement.status.Should().Be(EconomicSettlementStatus.AWAITING_RECONCILIATION, "parked, never failed");
        _alerts.Raised.Should().ContainSingle(alert =>
            alert.AlertCode == EconomicOperatorAlertCodes.ReconciliationAttentionRequired
            && alert.SettlementId == SettlementId);
    }

    public static TheoryData<HttpStatusCode, string> AmbiguousReceiptAnswers() => new()
    {
        { HttpStatusCode.ServiceUnavailable, NodeFailure("DEPENDENCY_UNAVAILABLE") },
        { HttpStatusCode.NotFound, "<html><body>Application not found</body></html>" },
        { HttpStatusCode.NotFound, string.Empty },
    };

    [Theory]
    [MemberData(nameof(AmbiguousReceiptAnswers))]
    public async Task AmbiguousReceiptAnswer_StaysInReconciliationAndSpendsTheAttempt(HttpStatusCode status, string body)
    {
        if (Skipped())
            return;

        await SeedPendingSettlementAsync();
        _node.OnExchange(HttpStatusCode.ServiceUnavailable, NodeFailure("RAIL_UNAVAILABLE"));
        await DispatchOnceAsync();
        await AgePastQuietWindowAsync(expireLease: false);
        var before = await OutboxAsync();

        _node.OnReceipt(status, body);
        (await ReconcileOnceAsync()).Outcome.Should().Be(EconomicOutboxRecordedOutcome.Unknown);

        var outbox = await OutboxAsync();
        outbox.status.Should().Be(EconomicOutboxStatus.AWAITING_RECONCILIATION);
        outbox.failureCode.Should().Be("AZOA_TRANSPORT_UNCERTAIN");
        outbox.attemptCount.Should().Be(before.attemptCount + 1);
        outbox.reconciliationRequiredAt.Should().Be(before.reconciliationRequiredAt);
        (await SettlementAsync()).status.Should().Be(EconomicSettlementStatus.AWAITING_RECONCILIATION);
        _node.Requests.Count(request => request.Path == ExchangePath).Should().Be(1, "nothing was redispatched");
    }

    // --- Harness -----------------------------------------------------------------------------

    private bool Skipped()
    {
        if (_options is not null)
            return false;

        _output.WriteLine($"[DATABASE SKIPPED: set {ConnectionEnv} to a Postgres server connection string]");
        return true;
    }

    private ArdaNovaDbContext NewContext() => new(_options!);

    private async Task<EconomicOutboxDispatchRun> DispatchOnceAsync()
    {
        await using var context = NewContext();
        return await NewDispatcher(context).DispatchOneAsync();
    }

    private async Task<EconomicOutboxDispatchRun> ReconcileOnceAsync()
    {
        await using var context = NewContext();
        return await NewDispatcher(context).ReconcileOneAsync();
    }

    private EconomicOutboxDispatchService NewDispatcher(ArdaNovaDbContext context)
    {
        var readiness = new Mock<IAzoaValueEligibilityService>();
        readiness.Setup(service => service.RequireReadyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(new AzoaCustodialAccountStatusDto
            {
                AvatarId = AvatarId.ToString("D"),
                WalletId = "wallet-f29",
                WalletAddress = "managed-account-address",
                IdentityReady = true,
                KycReady = true,
                KycStatus = AzoaKycStatus.Approved,
                WalletReady = true,
                Ready = true,
            }));
        var http = new HttpClient(_node) { BaseAddress = new Uri("https://node.test") };
        var exchange = new AzoaExchangeClient(
            http, NullLogger<AzoaExchangeClient>.Instance, Options.Create(new AzoaSettings()));
        var gateway = new AzoaSettlementGateway(
            exchange,
            readiness.Object,
            new AzoaSettlementGatewayCapability("node-f29", true, DateTimeOffset.UtcNow.AddDays(1)),
            TimeProvider.System,
            NullLogger<AzoaSettlementGateway>.Instance);
        return new EconomicOutboxDispatchService(
            new EconomicOutboxLeaseStore(context),
            gateway,
            readiness.Object,
            operatorAlerts: _alerts);
    }

    private async Task SeedPendingSettlementAsync()
    {
        var now = DateTime.UtcNow;
        await using var context = NewContext();
        context.Users.Add(new User
        {
            id = "user-f29",
            email = "f29@example.test",
            createdAt = now,
            updatedAt = now,
        });
        context.EconomicSettlements.Add(new EconomicSettlement
        {
            id = SettlementId,
            kind = EconomicSettlementKind.TASK_REWARD,
            status = EconomicSettlementStatus.PENDING_DISPATCH,
            idempotencyKey = IdempotencyKey,
            beneficiaryUserId = "user-f29",
            assetCode = "PROJ",
            amount = 12.34m,
            scale = 2,
            termsSnapshot = TermsSnapshot,
            version = 1,
            createdAt = now,
            updatedAt = now,
        });
        context.EconomicOutboxes.Add(new EconomicOutbox
        {
            id = OutboxId,
            settlementId = SettlementId,
            status = EconomicOutboxStatus.PENDING,
            payloadVersion = 1,
            attemptCount = 0,
            availableAt = now.AddSeconds(-1),
            createdAt = now,
            updatedAt = now,
        });
        await context.SaveChangesAsync();
    }

    /// <summary>Moves the row's clock back past the quiet window, as if the worker came round later.</summary>
    private async Task AgePastQuietWindowAsync(bool expireLease)
    {
        var past = DateTime.UtcNow - EconomicOutboxDispatchService.NoEffectQuietWindow - TimeSpan.FromMinutes(1);
        await using var context = NewContext();
        await context.EconomicOutboxes
            .Where(item => item.id == OutboxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.reconciliationRequiredAt, past)
                .SetProperty(item => item.availableAt, past)
                .SetProperty(item => item.leaseExpiresAt, item => expireLease ? past : item.leaseExpiresAt));
    }

    private async Task<EconomicOutbox> OutboxAsync()
    {
        await using var context = NewContext();
        return await context.EconomicOutboxes.AsNoTracking().SingleAsync(item => item.id == OutboxId);
    }

    private async Task<EconomicSettlement> SettlementAsync()
    {
        await using var context = NewContext();
        return await context.EconomicSettlements.AsNoTracking().SingleAsync(item => item.id == SettlementId);
    }

    private const string TermsSnapshot = """
        {"settlementLeg":"projectUnit","chainType":"Simulated","isBuy":true,"projectReference":"project-f29",
         "projectAssetId":"asset-f29","reservePartition":"sponsor","platformAmountBaseUnits":"5000",
         "projectUnitAmountBaseUnits":"1234","purpose":"sponsorship",
         "releaseNotBefore":"2026-11-25T10:15:30.1234567+00:00",
         "declaredRate":{"projectTokenConfigId":"config-f29","ardaPerWholeProjectUnit":"4.0518638573743922",
           "usdMajorUnitsPerWholeProjectUnit":"0.0405","projectUnitDecimals":2,"source":"tenant-declared",
           "declaredAt":"2026-10-25T08:00:00.9876543+00:00","version":"3"}}
        """;

    private static string NodeFailure(string code)
        => $$$"""{"isError":true,"message":"node refusal","result":null,"code":"{{{code}}}"}""";

    private static string AcceptedReceipt(Guid operationId)
        => $$$"""
           {"isError":false,"message":"Exchange completed.","result":{"receiptReference":"rcpt-f29",
            "operationId":"{{{operationId:D}}}","state":"Confirmed","isTerminal":true,"requiresReconciliation":false,
            "replayed":false,"amountSpent":"5000","amountReceived":"1234"}}
           """;

    /// <summary>The node's HTTP edge: one scripted answer per route, every request recorded.</summary>
    private sealed class FakeNode : HttpMessageHandler
    {
        private (HttpStatusCode Status, string Body) _exchange = (HttpStatusCode.ServiceUnavailable, string.Empty);
        private (HttpStatusCode Status, string Body) _receipt = (HttpStatusCode.ServiceUnavailable, string.Empty);

        public List<RecordedRequest> Requests { get; } = new();

        public void OnExchange(HttpStatusCode status, string body) => _exchange = (status, body);

        public void OnReceipt(HttpStatusCode status, string body) => _receipt = (status, body);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            var key = request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.Single() : null;
            Requests.Add(new RecordedRequest(path, key, body));

            var (status, responseBody) = path.StartsWith("/api/exchange/receipt/", StringComparison.Ordinal)
                ? _receipt
                : _exchange;
            path.Should().BeOneOf(ExchangePath, ReceiptPath);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed record RecordedRequest(string Path, string? IdempotencyKey, string Body);

    private sealed class RecordingAlerts : IOperatorAlertService
    {
        public List<EconomicOperatorAlert> Raised { get; } = new();

        public Task RaiseAsync(EconomicOperatorAlert alert, CancellationToken ct = default)
        {
            Raised.Add(alert);
            return Task.CompletedTask;
        }
    }
}
