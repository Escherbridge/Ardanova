namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// The withdrawal chain below the controller: a person takes back the standing permission
/// ArdaNova was given when their account was provisioned. Every assertion is made at the counter -
/// the request that actually reached the node, or the proof that none did.
/// </summary>
public sealed class ConsentRevokeChainTests
{
    private const string NodeBaseAddress = "https://azoa.example";
    private const string ConsentRoute = "/api/tenant/consent/provisioned";
    private const string IdentityQueryPrefix = "?externalUserId=";

    [Fact]
    public async Task RevokeProvisionedConsentAsync_SendsOneDeleteForTheNamedPersonAndReportsTheWithdrawal()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, """
            { "isError": false, "result": 2 }
            """));
        var gateway = CreateGateway(handler);

        var result = await gateway.RevokeProvisionedConsentAsync("user-42");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revoked.Should().BeTrue();
        result.Value.PermissionsWithdrawn.Should().Be(2);
        result.Value.RevokedAt.Should().NotBeNull("this call is what ended the permission");

        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Method.Should().Be(HttpMethod.Delete);
        handler.Calls[0].Path.Should().Be(ConsentRoute);
        IdentityIn(handler.Calls[0].Query).Should().Be("user-42");
    }

    [Fact]
    public async Task RevokeProvisionedConsentAsync_ReportsAnAlreadyWithdrawnPermissionAsWithdrawn()
    {
        // Zero live grants is the person's desired end state, not a failure: saying "we could not
        // withdraw it" here would be a false statement about a permission that is already gone.
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, """
            { "isError": false, "result": 0 }
            """));
        var gateway = CreateGateway(handler);

        var result = await gateway.RevokeProvisionedConsentAsync("user-42");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Revoked.Should().BeTrue();
        result.Value.PermissionsWithdrawn.Should().Be(0);
        result.Value.RevokedAt.Should().BeNull("nothing was ended by this request");
    }

    [Theory]
    [InlineData("acme/user/42")]
    [InlineData("../admin")]
    [InlineData("user 42")]
    [InlineData("user&participationRef=someone-else")]
    public async Task RevokeProvisionedConsentAsync_KeepsTheIdentityInTheQueryAndOutOfTheRoute(
        string ardaNovaUserId)
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, """
            { "isError": false, "result": 1 }
            """));
        var gateway = CreateGateway(handler);

        var result = await gateway.RevokeProvisionedConsentAsync(ardaNovaUserId);

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().ContainSingle();
        handler.Calls[0].Path.Should()
            .Be(ConsentRoute, "an identity must never become part of the route");
        IdentityIn(handler.Calls[0].Query).Should()
            .Be(ardaNovaUserId, "the node must receive exactly the person it was asked about");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RevokeProvisionedConsentAsync_RefusesABlankIdentityBeforeAnythingIsWithdrawn(
        string ardaNovaUserId)
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException(
            "A blank identity must never reach the node."));
        var gateway = CreateGateway(handler);

        var result = await gateway.RevokeProvisionedConsentAsync(ardaNovaUserId);

        result.Type.Should().Be(ResultType.ValidationError);
        handler.Calls.Should().BeEmpty("no permission may be withdrawn without naming whose it is");
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, ResultType.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized, ResultType.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ResultType.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError, ResultType.Failure)]
    public async Task RevokeProvisionedConsentAsync_NeverClaimsAWithdrawalTheNodeRefused(
        HttpStatusCode status,
        ResultType expected)
    {
        var handler = new RecordingHandler(_ => Json(status, """
            { "isError": true, "message": "no" }
            """));
        var gateway = CreateGateway(handler);

        var result = await gateway.RevokeProvisionedConsentAsync("user-42");

        result.Type.Should().Be(expected);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task RevokeProvisionedConsentAsync_ResolvesTheCustodyTransportByItsImplementationNameToo()
    {
        // IHttpClientFactory names a typed-client registration after the SERVICE type; the gateway
        // accepts the implementation name as well, so neither convention silently yields an
        // unconfigured client that would post a withdrawal into the void.
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, """
            { "isError": false, "result": 1 }
            """));
        var gateway = new AzoaCustodialAccountGateway(
            UnusableNodeClient(),
            new StubHttpClientFactory(
                (nameof(AzoaCustodialNodeClient), ConfiguredClient(handler))),
            NullLogger<AzoaCustodialAccountGateway>.Instance);

        var result = await gateway.RevokeProvisionedConsentAsync("user-42");

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task RevokeProvisionedConsentAsync_FailsClosedWhenNoCustodyTransportIsConfigured()
    {
        var gateway = new AzoaCustodialAccountGateway(
            UnusableNodeClient(),
            new StubHttpClientFactory(),
            NullLogger<AzoaCustodialAccountGateway>.Instance);

        var result = await gateway.RevokeProvisionedConsentAsync("user-42");

        result.Type.Should().Be(ResultType.Failure);
        result.Value.Should().BeNull("an unreachable node must never read as a withdrawal");
    }

    [Fact]
    public async Task RevokeProvisionedConsentAsync_FailsClosedWhenTheGatewayWasBuiltWithoutATransport()
    {
        var gateway = new AzoaCustodialAccountGateway(UnusableNodeClient());

        var result = await gateway.RevokeProvisionedConsentAsync("user-42");

        result.Type.Should().Be(ResultType.Failure);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void OnlyTheEnabledCustodyGatewayCarriesTheWithdrawalPort()
    {
        // The controller branches on this port. With custody switched off the disabled gateway
        // must NOT carry it, so the answer is a stated refusal rather than a reported withdrawal
        // that never reached the node and left a live grant standing.
        IAzoaCustodialAccountGateway enabled = new AzoaCustodialAccountGateway(UnusableNodeClient());
        IAzoaCustodialAccountGateway disabled = new DisabledAzoaCustodialAccountGateway();

        enabled.Should().BeAssignableTo<IAzoaConsentRevocationGateway>();
        disabled.Should().NotBeAssignableTo<IAzoaConsentRevocationGateway>();
    }

    private static AzoaCustodialAccountGateway CreateGateway(RecordingHandler handler)
        => new(
            UnusableNodeClient(),
            new StubHttpClientFactory(
                (nameof(IAzoaCustodialNodeClient), ConfiguredClient(handler))),
            NullLogger<AzoaCustodialAccountGateway>.Instance);

    private static HttpClient ConfiguredClient(HttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri(NodeBaseAddress) };

    /// <summary>Withdrawal must not travel over the read/provision verbs; this fake proves it does not.</summary>
    private static IAzoaCustodialNodeClient UnusableNodeClient() => new ThrowingCustodialNodeClient();

    /// <summary>The identity the node actually received, read back out of the query string.</summary>
    private static string IdentityIn(string query)
    {
        query.Should().StartWith(IdentityQueryPrefix);
        return Uri.UnescapeDataString(query[IdentityQueryPrefix.Length..]);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private sealed record RecordedCall(HttpMethod Method, string Path, string Query);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public List<RecordedCall> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Recorded before the response is produced, so a throwing responder still proves
            // the call reached the wire.
            Calls.Add(new RecordedCall(
                request.Method,
                request.RequestUri!.AbsolutePath,
                request.RequestUri.Query));
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> _clients;

        public StubHttpClientFactory(params (string Name, HttpClient Client)[] clients)
        {
            _clients = clients.ToDictionary(
                entry => entry.Name,
                entry => entry.Client,
                StringComparer.Ordinal);
        }

        // An unknown name yields a client with no base address - exactly what a real factory
        // returns for a registration that does not exist.
        public HttpClient CreateClient(string name)
            => _clients.TryGetValue(name, out var client) ? client : new HttpClient();
    }

    private sealed class ThrowingCustodialNodeClient : IAzoaCustodialNodeClient
    {
        public Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
            => throw new InvalidOperationException("Withdrawal must not use the custodial read verb.");

        public Task<Result<T>> PostAsync<T>(
            string path,
            object? body,
            string idempotencyKey,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Withdrawal must not use the custodial POST verb.");

        public Task<Result<T>> PutAsync<T>(
            string path,
            object? body,
            string idempotencyKey,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Withdrawal must not use the custodial PUT verb.");
    }
}
