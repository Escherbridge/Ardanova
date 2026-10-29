namespace ArdaNova.API.Tests.Flows;

using System.Net;
using System.Security.Claims;
using System.Text;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Azoa;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Outbox;
using ArdaNova.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.NameTranslation;

/// <summary>
/// H-1 / L-1 / M-5: a sponsorship request key is discarded only through the void route, and the
/// void serialises with an in-flight create. Driven through <see cref="FundingIntentsController"/>
/// with the real service, repositories, outbox store and settlement gateway on a throwaway Postgres
/// database; only the node's HTTP edge and the account-readiness read are faked.
/// </summary>
/// <remarks>
/// Needs <c>ARDANOVA_TEST_POSTGRES</c> (a server connection string without a database name); without
/// it every test reports SKIPPED via <see cref="PostgresFactAttribute"/>, the same gate
/// <c>SettlementNoEffectReopenFlowTests</c> uses.
/// Interleavings are forced at the seams a concurrent request really crosses: the readiness read
/// (before the create commits), the release compare-and-set, the node call, the hold compare-and-set.
/// </remarks>
[Trait("Category", "Database")]
public sealed class SponsorshipVoidFlowTests : IAsyncLifetime
{
    private const string SponsorId = "sponsor-void";
    private const string OwnerId = "owner-void";
    private const string ProjectId = "project-void";
    private const string ConfigId = "config-void";
    private const string Disclosure = "funding-disclosure-v1";

    private static readonly Guid AvatarId = Guid.Parse("4b3e1f0a-8c2d-4e6f-9a1b-2c3d4e5f6a7b");
    private static readonly string ExchangePath = "/api/exchange/" + AvatarId.ToString("D");

    private readonly FakeNode _node = new();
    private readonly Dictionary<(string User, string Action), Result<AzoaCustodialAccountStatusDto>> _readiness = new();
    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    /// <summary>Runs once inside the sponsor's readiness read - after the create's key checks, before its commit.</summary>
    private Func<Task>? _duringReadiness;

    public async Task InitializeAsync()
    {
        // Only reached when PostgresFact did not skip, so the server is configured.
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_void_" + Guid.NewGuid().ToString("N")[..12],
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

        await using (var context = NewContext())
            await context.Database.EnsureCreatedAsync();
        await SeedFundableProjectAsync();
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

    // -- H-1: the only way out of a refused-before-admission key ----------------------------------

    [PostgresFact]
    public async Task RefusedBeforeAdmissionThenCancelled_TombstonesTheKeyAndALaterCreateIsRefusedAsVoided()
    {
        var key = NewKey();
        SponsorIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("fund a project")));

        var refused = await CreateAsync(key);
        refused.Status.Should().Be(403);
        refused.Code.Should().Be(MoneyRefusalCodes.SetupRequired, "a pre-admission refusal keeps the key");

        var voided = await VoidAsync(key);
        voided.Status.Should().Be(200);
        voided.Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Voided);
        voided.Body.IntentId.Should().BeNull();
        (await TombstonesAsync(key)).Should().Be(1);

        // Setup finished since: a fresh key would be admitted, but THIS key is dead.
        SponsorIs(Ready());
        var late = await CreateAsync(key);

        late.Status.Should().Be(409);
        late.Code.Should().Be(MoneyRefusalCodes.Definite);
        late.Reason.Should().Be(MoneyRefusalReasons.Voided);
        (await IntentsAsync()).Should().BeEmpty();
        _node.ExchangeCalls.Should().Be(0);
    }

    // -- L-1: the void racing an in-flight create ------------------------------------------------

    [PostgresFact]
    public async Task VoidLandsWhileTheCreateIsBetweenItsKeyCheckAndItsCommit_TheCreateIsRefusedAndNothingIsSent()
    {
        var key = NewKey();
        VoidAnswer? voidSeen = null;
        // The create already passed its tombstone check; the void finds no intent and tombstones.
        _duringReadiness = async () => voidSeen = (await VoidAsync(key)).Body;

        var create = await CreateAsync(key);

        voidSeen!.Outcome.Should().Be(FundingIntentVoidOutcomes.Voided);
        create.Status.Should().Be(409);
        create.Code.Should().Be(MoneyRefusalCodes.Definite);
        _node.ExchangeCalls.Should().Be(0, "the create re-read the tombstone after committing, before releasing");
        var (intent, settlement, outbox) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.CANCELLED);
        settlement.status.Should().Be(EconomicSettlementStatus.CANCELLED);
        settlement.failureCode.Should().Be(FundingIntentService.VoidedBySponsorCode);
        outbox.status.Should().Be(EconomicOutboxStatus.CANCELLED);
    }

    [PostgresFact]
    public async Task VoidLandsAfterTheCreateCommittedButBeforeItReleased_CancelsItAndTheCreateNeverDispatches()
    {
        var key = NewKey();
        VoidAnswer? voidSeen = null;
        var leases = new HookedLeaseStore { BeforeRelease = async () => voidSeen = (await VoidAsync(key)).Body };

        var create = await CreateAsync(key, leases);

        voidSeen!.Outcome.Should().Be(FundingIntentVoidOutcomes.Voided);
        voidSeen.IntentId.Should().NotBeNull("an admitted, never-released sponsorship is cancelled, not tombstoned");
        create.Status.Should().Be(409);
        create.Code.Should().Be(MoneyRefusalCodes.Definite, "the release compare-and-set lost to the void");
        _node.ExchangeCalls.Should().Be(0);
        (await RowsAsync(key)).Intent.status.Should().Be(FundingIntentStatus.CANCELLED);

        // And the key stays dead: a replay of it is the same final answer, still with no node call.
        var replay = await CreateAsync(key);
        replay.Code.Should().Be(MoneyRefusalCodes.Definite);
        _node.ExchangeCalls.Should().Be(0);
    }

    [PostgresFact]
    public async Task VoidLandsWhileTheCreatesNodeCallIsInFlight_RevealsItAndCancelsNothing()
    {
        var key = NewKey();
        VoidAnswer? voidSeen = null;
        _node.OnExchange(async () =>
        {
            // The node is moving the funds right now; the sponsor presses "Cancel that request".
            voidSeen = (await VoidAsync(key)).Body;
            return (HttpStatusCode.OK, AcceptedReceipt());
        });

        var create = await CreateAsync(key);

        voidSeen!.Outcome.Should().Be(FundingIntentVoidOutcomes.Revealed);
        voidSeen.Intent!.Status.Should().Be(nameof(FundingIntentStatus.DRAFT));
        create.Status.Should().Be(200);
        var (intent, settlement, _) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.SETTLED);
        settlement.status.Should().Be(EconomicSettlementStatus.CONFIRMED);
        _node.ExchangeCalls.Should().Be(1);

        var after = await VoidAsync(key);
        after.Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Revealed);
        after.Body.Intent!.Status.Should().Be(nameof(FundingIntentStatus.SETTLED));
    }

    // -- M-5: nothing cancels an outbox a worker holds --------------------------------------------

    [PostgresFact]
    public async Task ALapseWhileAWorkerHoldsTheOutboxLease_IsNeitherHeldNorCancelled_AndAVoidOnlyRevealsIt()
    {
        var key = NewKey();
        BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("receive settled project value")));
        // A worker claims the outbox (as after the handover delay) just before the request's hold lands.
        var leases = new HookedLeaseStore
        {
            BeforeCancelUnleased = async () =>
            {
                await using var worker = NewContext();
                var lease = await new EconomicOutboxLeaseStore(worker).TryClaimAsync(
                    EconomicOutboxClaimKind.Dispatch, DateTime.UtcNow.AddMinutes(5), TimeSpan.FromMinutes(5));
                lease.Should().NotBeNull();
            },
        };

        var create = await CreateAsync(key, leases);

        create.Status.Should().Be(200, "the sponsorship follows the worker instead of claiming nothing was taken");
        var (intent, settlement, outbox) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.SETTLEMENT_PENDING);
        intent.heldAt.Should().BeNull();
        settlement.status.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH);
        outbox.status.Should().Be(EconomicOutboxStatus.LEASED);

        var voided = await VoidAsync(key);
        voided.Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Revealed);
        (await RowsAsync(key)).Outbox.status.Should().Be(EconomicOutboxStatus.LEASED);
    }

    [PostgresFact]
    public async Task ALapseOnAnUnleasedOutbox_HoldsAndCancelsBoth_AndTheVoidThenReleasesTheKey()
    {
        var key = NewKey();
        BeneficiaryIs(Result<AzoaCustodialAccountStatusDto>.Forbidden(ValueEligibilityMessages.SetupIncomplete("receive settled project value")));

        var create = await CreateAsync(key);

        create.Status.Should().Be(409);
        create.Code.Should().Be(MoneyRefusalCodes.Finished, "a terminal state alone never releases a key");
        var (intent, settlement, outbox) = await RowsAsync(key);
        intent.status.Should().Be(FundingIntentStatus.HOLDING);
        settlement.status.Should().Be(EconomicSettlementStatus.CANCELLED);
        outbox.status.Should().Be(EconomicOutboxStatus.CANCELLED);

        // Held with nothing dispatchable left: the void can prove nothing was or will be taken.
        var voided = await VoidAsync(key);
        voided.Body!.Outcome.Should().Be(FundingIntentVoidOutcomes.Voided);
        voided.Body.IntentId.Should().Be(intent.id);
    }

    // -- Harness -----------------------------------------------------------------------------

    private sealed record CreateAnswer(int Status, string? Code, string? Reason);

    private sealed record VoidAnswer(string Outcome, string? IntentId, FundingIntentStatusDto? Intent);

    private ArdaNovaDbContext NewContext() => new(_options!);

    private static string NewKey() => Guid.NewGuid().ToString("D");

    private void SponsorIs(Result<AzoaCustodialAccountStatusDto> answer) => _readiness[(SponsorId, "fund a project")] = answer;

    private void BeneficiaryIs(Result<AzoaCustodialAccountStatusDto> answer)
        => _readiness[(SponsorId, "receive settled project value")] = answer;

    /// <summary>One request: its own context, unit of work, repositories and store, exactly as DI scopes them.</summary>
    private async Task<CreateAnswer> CreateAsync(string key, HookedLeaseStore? leases = null)
    {
        await using var context = NewContext();
        var controller = NewController(context, leases);
        controller.Request.Headers["X-Idempotency-Key"] = key;

        var result = await controller.CreateCheckout(
            new CreateFundingIntentDto { ProjectTokenConfigId = ConfigId, Amount = "9.00", DisclosureVersion = Disclosure },
            CancellationToken.None);

        if (result is OkObjectResult)
            return new CreateAnswer(200, null, null);

        var refusal = result.Should().BeAssignableTo<ObjectResult>().Subject;
        var body = refusal.Value!;
        return new CreateAnswer(
            refusal.StatusCode ?? 0,
            (string?)body.GetType().GetProperty("code")?.GetValue(body),
            (string?)body.GetType().GetProperty("reason")?.GetValue(body));
    }

    private async Task<(int Status, VoidAnswer? Body)> VoidAsync(string key)
    {
        await using var context = NewContext();
        var result = await NewController(context, leases: null).VoidRequest(key, CancellationToken.None);
        if (result is OkObjectResult { Value: FundingIntentVoidDto dto })
            return (200, new VoidAnswer(dto.Outcome, dto.IntentId, dto.Intent));

        return (result.Should().BeAssignableTo<ObjectResult>().Subject.StatusCode ?? 0, null);
    }

    private FundingIntentsController NewController(ArdaNovaDbContext context, HookedLeaseStore? leases)
    {
        var readiness = new Mock<IAzoaValueEligibilityService>();
        readiness
            .Setup(service => service.RequireReadyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string user, string action, CancellationToken _) =>
            {
                if (user == SponsorId && action == "fund a project" && _duringReadiness is { } hook)
                {
                    _duringReadiness = null;
                    await hook();
                }

                return _readiness.TryGetValue((user, action), out var answer) ? answer : Ready();
            });

        var store = new EconomicOutboxLeaseStore(context);
        IEconomicOutboxLeaseStore outboxLeases = leases is null ? store : leases.Wrapping(store);

        var http = new HttpClient(_node) { BaseAddress = new Uri("https://node.test") };
        var gateway = new AzoaSettlementGateway(
            new AzoaExchangeClient(http, NullLogger<AzoaExchangeClient>.Instance, Options.Create(new AzoaSettings())),
            readiness.Object,
            new AzoaSettlementGatewayCapability("node-void", true, DateTimeOffset.UtcNow.AddDays(1)),
            TimeProvider.System,
            NullLogger<AzoaSettlementGateway>.Instance);

        var rate = new Mock<IProjectUnitRateSource>();
        rate.Setup(source => source.GetDeclaredRateAsync(ConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeclaredProjectUnitRate>.Success(new DeclaredProjectUnitRate(
                ProjectTokenConfigId: ConfigId,
                ArdaPerWholeProjectUnit: "3.00",
                UsdMajorUnitsPerWholeProjectUnit: "0.03",
                ProjectUnitDecimals: 2,
                Source: "test:rate",
                DeclaredAt: DateTimeOffset.UtcNow,
                Version: null)));
        var gate = new Mock<IProjectGateService>();
        gate.Setup(service => service.EvaluateGate1Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GateTransitionResultDto>.Success(new GateTransitionResultDto()));
        var settlementReadiness = new Mock<IFundingSettlementReadiness>();
        settlementReadiness.SetupGet(item => item.IsReady).Returns(true);

        var service = new FundingIntentService(
            new Repository<User>(context),
            new Repository<Project>(context),
            new Repository<ProjectTokenConfig>(context),
            new Repository<FundingIntent>(context),
            new Repository<EconomicSettlement>(context),
            new Repository<EconomicOutbox>(context),
            new Repository<ProjectInvestment>(context),
            new Repository<PlatformTreasuryTransaction>(context),
            new Repository<PlatformTreasury>(context),
            gateway,
            rate.Object,
            gate.Object,
            settlementReadiness.Object,
            readiness.Object,
            new ArdaNova.Infrastructure.UnitOfWork.UnitOfWork(context),
            new Repository<FundingIntentVoid>(context),
            outboxLeases);

        return new FundingIntentsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, SponsorId)], "test")),
                },
            },
        };
    }

    private static Result<AzoaCustodialAccountStatusDto> Ready() => Result<AzoaCustodialAccountStatusDto>.Success(new()
    {
        AvatarId = AvatarId.ToString("D"),
        WalletId = "wallet-void",
        WalletAddress = "managed-account-address",
        IdentityReady = true,
        KycReady = true,
        KycStatus = AzoaKycStatus.Approved,
        WalletReady = true,
        Ready = true,
    });

    private async Task SeedFundableProjectAsync()
    {
        var now = DateTime.UtcNow;
        await using var context = NewContext();
        context.Users.AddRange(
            new User { id = SponsorId, email = "sponsor@void.test", createdAt = now, updatedAt = now },
            new User { id = OwnerId, email = "owner@void.test", createdAt = now, updatedAt = now });
        context.Projects.Add(new Project
        {
            id = ProjectId,
            title = "A project people back",
            slug = "a-project-people-back",
            status = ProjectStatus.PUBLISHED,
            commerceEnabled = true,
            createdById = OwnerId,
            createdAt = now,
            updatedAt = now,
        });
        context.ProjectTokenConfigs.Add(new ProjectTokenConfig
        {
            id = ConfigId,
            projectId = ProjectId,
            assetId = "asset-void",
            assetName = "Project Units",
            unitName = "PROJ",
            assetScale = 2,
            status = ProjectTokenStatus.ACTIVE,
            gateStatus = ProjectGateStatus.FUNDING,
            totalSupply = 1_000,
            investorPartitionSupply = 1_000,
            fundingGoal = 1_000d,
            assetChain = "Algorand",
            treasuryChain = "Algorand",
            treasuryReference = ProjectId,
            treasuryAddress = "TREASURY-ADDRESS",
            treasuryResolvedAt = now,
            partitionsDeclaredAt = now,
            createdAt = now,
            updatedAt = now,
        });
        await context.SaveChangesAsync();
    }

    private async Task<List<FundingIntent>> IntentsAsync()
    {
        await using var context = NewContext();
        return await context.FundingIntents.AsNoTracking().ToListAsync();
    }

    private async Task<int> TombstonesAsync(string key)
    {
        await using var context = NewContext();
        return await context.FundingIntentVoids.CountAsync(row => row.funderUserId == SponsorId && row.idempotencyKey == key);
    }

    private async Task<(FundingIntent Intent, EconomicSettlement Settlement, EconomicOutbox Outbox)> RowsAsync(string key)
    {
        await using var context = NewContext();
        var intent = await context.FundingIntents.AsNoTracking()
            .SingleAsync(row => row.funderUserId == SponsorId && row.idempotencyKey == key);
        var settlement = await context.EconomicSettlements.AsNoTracking().SingleAsync(row => row.id == intent.settlementId);
        var outbox = await context.EconomicOutboxes.AsNoTracking().SingleAsync(row => row.settlementId == settlement.id);
        return (intent, settlement, outbox);
    }

    private static string AcceptedReceipt()
        => $$$"""
           {"isError":false,"message":"Exchange completed.","result":{"receiptReference":"rcpt-void",
            "operationId":"{{{Guid.NewGuid():D}}}","state":"Confirmed","isTerminal":true,"requiresReconciliation":false,
            "replayed":false,"amountSpent":"900","amountReceived":"300"}}
           """;

    /// <summary>The node's HTTP edge: the exchange route only, answered by a script that may act mid-call.</summary>
    private sealed class FakeNode : HttpMessageHandler
    {
        private Func<Task<(HttpStatusCode, string)>> _exchange =
            () => Task.FromResult((HttpStatusCode.OK, AcceptedReceipt()));

        public int ExchangeCalls { get; private set; }

        public void OnExchange(Func<Task<(HttpStatusCode, string)>> answer) => _exchange = answer;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            request.RequestUri!.AbsolutePath.Should().Be(ExchangePath);
            ExchangeCalls++;
            var (status, body) = await _exchange();
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    /// <summary>
    /// The real outbox store with a concurrent request injected at the exact compare-and-set it races.
    /// It fakes nothing: every call still lands on <see cref="EconomicOutboxLeaseStore"/>.
    /// </summary>
    private sealed class HookedLeaseStore
    {
        public Func<Task>? BeforeRelease { get; init; }

        public Func<Task>? BeforeCancelUnleased { get; init; }

        public IEconomicOutboxLeaseStore Wrapping(IEconomicOutboxLeaseStore inner) => new Hooked(inner, this);

        private sealed class Hooked(IEconomicOutboxLeaseStore inner, HookedLeaseStore hooks) : IEconomicOutboxLeaseStore
        {
            public Task<EconomicOutboxLease?> TryClaimAsync(EconomicOutboxClaimKind kind, DateTime now, TimeSpan leaseDuration, CancellationToken ct = default)
                => inner.TryClaimAsync(kind, now, leaseDuration, ct);

            public Task<EconomicOutboxLease?> TryRenewAsync(EconomicOutboxLease lease, DateTime now, TimeSpan leaseDuration, CancellationToken ct = default)
                => inner.TryRenewAsync(lease, now, leaseDuration, ct);

            public Task<bool> FinalizeAsync(EconomicOutboxLease lease, EconomicOutboxFinalization finalization, DateTime now, CancellationToken ct = default)
                => inner.FinalizeAsync(lease, finalization, now, ct);

            public async Task<bool> TryReleaseForDispatchAsync(string settlementId, DateTime now, CancellationToken ct = default)
            {
                if (hooks.BeforeRelease is { } hook)
                    await hook();
                return await inner.TryReleaseForDispatchAsync(settlementId, now, ct);
            }

            public Task<EconomicSettlementStatus?> ReadSettlementStatusAsync(string settlementId, CancellationToken ct = default)
                => inner.ReadSettlementStatusAsync(settlementId, ct);

            public Task<bool> TryCancelUndispatchedAsync(string settlementId, string failureCode, string? failureDetail, DateTime now, CancellationToken ct = default)
                => inner.TryCancelUndispatchedAsync(settlementId, failureCode, failureDetail, now, ct);

            public async Task<bool> TryCancelUnleasedAsync(
                string settlementId, int expectedSettlementVersion, string? failureCode, string? failureDetail, DateTime now, CancellationToken ct = default)
            {
                if (hooks.BeforeCancelUnleased is { } hook)
                    await hook();
                return await inner.TryCancelUnleasedAsync(settlementId, expectedSettlementVersion, failureCode, failureDetail, now, ct);
            }
        }
    }
}
