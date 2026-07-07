namespace ArdaNova.Application.Tests.E2E;

using System.Net.Http;
using System.Net.Http.Headers;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Cross-process, live golden-path e2e for the ArdaNova ⇄ AZOA integration.
///
/// This talks to a REAL AZOA node process (oasis-sleek, <c>AZOA.WebAPI</c>) over
/// HTTP at <c>http://localhost:5000</c> using the REAL ArdaNova transport
/// (<see cref="AzoaNodeClient"/>). It is NOT a unit test and NOT part of the
/// normal <c>dotnet test</c> signal — it is GATED behind environment variables so
/// it skips cleanly (passes trivially) unless a developer has explicitly booted
/// AZOA locally and opted in.
///
/// GATE (all three must hold, else the test early-returns and logs a skip):
///   1. <c>AZOA_E2E == "1"</c>              — explicit opt-in.
///   2. <c>http://localhost:5000</c> reachable — the AZOA node is up.
///   3. <c>AZOA_E2E_API_KEY</c> is set        — an nft:mint-scoped X-Api-Key.
///
/// We deliberately do NOT take a NuGet dependency (e.g. Xunit.SkippableFact) to
/// achieve runtime skipping. Plain xUnit has no runtime-skip primitive, so the
/// gate is implemented as an early-return helper that logs
/// "[E2E SKIPPED: reason]" via <see cref="ITestOutputHelper"/> and lets the test
/// pass. The consequence: when gated off, these tests report as PASSED (green),
/// never failed/errored, so the baseline signal is unaffected. They are also
/// tagged <c>[Trait("Category","E2E")]</c> so they can be filtered in/out
/// (<c>--filter Category=E2E</c> to run only them; <c>--filter Category!=E2E</c>
/// to exclude them).
///
/// WHAT IT PROVES (contract §4/§6/§7, §11.2):
///   • Avatar self-register     — an anonymous end user becomes a self-sovereign
///                                AZOA avatar via <c>POST /api/avatar/register</c>.
///   • Exactly-once allocation  — a redelivered economic trigger (same avatar,
///                                same Idempotency-Key) REPLAYS the first op
///                                (replayed==true, SAME operationId) and moves the
///                                asset exactly once. This is the must-have
///                                assertion.
///
/// See <c>tests/E2E/README.md</c> for how to run it and how to mint the dev key.
/// </summary>
[Trait("Category", "E2E")]
public sealed class AzoaGoldenPathE2ETests
{
    /// <summary>Base address of the locally-booted AZOA node (see oasis-sleek launchSettings).</summary>
    private const string AzoaBaseUrl = "http://localhost:5000";

    private const string OptInEnv = "AZOA_E2E";
    private const string ApiKeyEnv = "AZOA_E2E_API_KEY";

    private readonly ITestOutputHelper _output;

    public AzoaGoldenPathE2ETests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GoldenPath_SelfRegister_Then_ExactlyOnce_Allocation()
    {
        // ── GATE ──────────────────────────────────────────────────────────────
        if (!TryEnterGate(out var apiKey))
            return; // skipped-and-logged; passes trivially so the baseline is unaffected.

        using var client = BuildRealNodeClient(apiKey, out var azoaClient);

        // ── (a) Avatar self-register (anonymous; §4/§11.2) ─────────────────────
        var unique = Guid.NewGuid().ToString("N")[..12];
        var registerRequest = new AzoaAvatarRegisterRequest
        {
            Username = $"e2e_{unique}",
            Email = $"e2e_{unique}@ardanova-e2e.test",
            // Meets the node's dev password policy; kept simple + deterministic-per-run.
            Password = $"E2e!{unique}Pw9",
            FirstName = "Ardanova",
            LastName = "E2E",
        };

        var registerResult = await azoaClient.RegisterAvatarAsync(registerRequest);

        registerResult.IsSuccess.Should().BeTrue(
            $"self-register must succeed against the live node, got: {registerResult.Error}");
        registerResult.Value.Should().NotBeNull();
        var avatarId = registerResult.Value!.Id;
        avatarId.Should().NotBe(Guid.Empty, "the node must return a concrete avatar id");
        _output.WriteLine($"[E2E] Registered avatar {avatarId} (username={registerRequest.Username}).");

        // ── (b) First allocation — the real value move (§6) ────────────────────
        // Stable per-economic-event key: the same reward event redelivered MUST
        // dedupe to the same operation. In Simulated mode the node auto-approves
        // KYC (no 403 is observable here) and returns deterministic sim: ids — the
        // KYC fail-closed path (§6/§11.4) is exercised by AZOA's own suite, not
        // here; the exactly-once guarantee is what this e2e uniquely proves.
        var idempotencyKey = $"e2e-reward:{Guid.NewGuid()}";
        var allocationRequest = new AzoaAllocationRequest
        {
            Kind = AzoaAllocationKind.Mint,
            ChainType = "Algorand",
            Amount = "1",
            Name = $"E2E Reward {unique}",
            Description = "ArdaNova ⇄ AZOA golden-path e2e allocation.",
            Metadata = new Dictionary<string, string> { ["source"] = "ardanova-e2e" },
        };

        var firstAlloc = await azoaClient.AllocateAsync(avatarId, allocationRequest, idempotencyKey);

        firstAlloc.IsSuccess.Should().BeTrue(
            $"the first allocation must succeed, got: {firstAlloc.Error}");
        firstAlloc.Value.Should().NotBeNull();
        firstAlloc.Value!.Replayed.Should().BeFalse(
            "the first delivery of a fresh Idempotency-Key must NOT be a replay");
        var firstOperationId = firstAlloc.Value.OperationId;
        firstOperationId.Should().NotBe(Guid.Empty, "the node must return the operation id it created");
        _output.WriteLine(
            $"[E2E] First allocation ok: operationId={firstOperationId}, " +
            $"walletProvisioned={firstAlloc.Value.WalletProvisioned}, replayed=false.");

        // ── (c) Redelivery — the exactly-once proof (§6/§7) ────────────────────
        // SAME avatar + SAME Idempotency-Key ⇒ the node must REPLAY the existing
        // op: replayed==true and the SAME operationId, with NO second value move.
        var replayAlloc = await azoaClient.AllocateAsync(avatarId, allocationRequest, idempotencyKey);

        replayAlloc.IsSuccess.Should().BeTrue(
            $"a redelivered allocation must still succeed (idempotent), got: {replayAlloc.Error}");
        replayAlloc.Value.Should().NotBeNull();
        replayAlloc.Value!.Replayed.Should().BeTrue(
            "redelivering the SAME Idempotency-Key MUST replay — this is the exactly-once guarantee");
        replayAlloc.Value.OperationId.Should().Be(
            firstOperationId,
            "the replay MUST resolve to the SAME operation — no second value move occurred");
        _output.WriteLine(
            $"[E2E] Replay ok: operationId={replayAlloc.Value.OperationId} (== first), replayed=true. " +
            "Exactly-once proven.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gate + construction helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates the three-part gate. Returns <c>true</c> only when the developer
    /// has opted in, the node is reachable, and an API key is present. On any
    /// miss it logs a clear "[E2E SKIPPED: reason]" and returns <c>false</c> so
    /// the caller early-returns and the test passes trivially.
    /// </summary>
    private bool TryEnterGate(out string apiKey)
    {
        apiKey = string.Empty;

        var optIn = Environment.GetEnvironmentVariable(OptInEnv);
        if (!string.Equals(optIn, "1", StringComparison.Ordinal))
        {
            Skip($"{OptInEnv} != \"1\" (set {OptInEnv}=1 and boot AZOA to run this).");
            return false;
        }

        var key = Environment.GetEnvironmentVariable(ApiKeyEnv);
        if (string.IsNullOrWhiteSpace(key))
        {
            Skip($"{ApiKeyEnv} is not set (an nft:mint-scoped X-Api-Key is required — see tests/E2E/README.md).");
            return false;
        }

        if (!IsNodeReachable())
        {
            Skip($"AZOA node at {AzoaBaseUrl} is not reachable (boot oasis-sleek + SurrealDB first).");
            return false;
        }

        apiKey = key.Trim();
        return true;
    }

    /// <summary>
    /// Cheap TCP/HTTP reachability probe against the node root. Any HTTP response
    /// (even 404/401) proves the process is up; only a transport failure or
    /// timeout counts as unreachable.
    /// </summary>
    private static bool IsNodeReachable()
    {
        try
        {
            using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            // A HEAD to the root: we only care that the socket answers, not the status.
            using var request = new HttpRequestMessage(HttpMethod.Head, AzoaBaseUrl + "/");
            _ = probe.Send(request);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false; // timeout
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the REAL <see cref="AzoaNodeClient"/> over a real <see cref="HttpClient"/>
    /// pointed at the local node, with the <c>X-Api-Key</c> default header and
    /// <c>Simulated</c> settings — exactly how Infrastructure wires it in prod,
    /// minus DI. Returns the owning <see cref="HttpClient"/> so the caller disposes it.
    /// </summary>
    private static HttpClient BuildRealNodeClient(string apiKey, out AzoaNodeClient azoaClient)
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri(AzoaBaseUrl),
            Timeout = TimeSpan.FromSeconds(30),
        };
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var settings = Options.Create(new AzoaSettings
        {
            BaseUrl = AzoaBaseUrl,
            TenantApiKey = apiKey,
            Mode = "Simulated",
            ChainType = "Algorand",
            TimeoutSeconds = 30,
        });

        azoaClient = new AzoaNodeClient(http, settings, NullLogger<AzoaNodeClient>.Instance);
        return http;
    }

    /// <summary>Logs a clear, greppable skip line. Does NOT fail the test.</summary>
    private void Skip(string reason) => _output.WriteLine($"[E2E SKIPPED: {reason}]");
}
