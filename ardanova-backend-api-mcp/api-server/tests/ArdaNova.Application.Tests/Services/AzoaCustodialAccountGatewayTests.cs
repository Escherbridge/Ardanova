namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class AzoaCustodialAccountGatewayTests
{
    [Fact]
    public async Task GetCapabilitiesAsync_MapsExplicitDevelopmentSimulation()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {
              "isError": false,
              "result": {
                "enabled": true,
                "walletChain": "Algorand",
                "custodyMode": "Development",
                "custodyAvailable": true,
                "blockchainProviderAvailable": true,
                "kycProvider": "Manual",
                "kycAvailable": true,
                "hostedVerification": false,
                "acceptsDocumentReferences": false,
                "developmentSimulation": true,
                "identityReady": true,
                "kycReady": true,
                "walletProvisioningReady": true,
                "ready": true
              }
            }
            """));
        var gateway = CreateGateway(handler);

        var result = await gateway.GetCapabilitiesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.DevelopmentSimulation.Should().BeTrue();
        result.Value.HostedVerification.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureAsync_EncodesSafeExternalSubjectAndCarriesStableIdempotencyKey()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            return Json(HttpStatusCode.OK, """
                {
                  "isError": false,
                  "result": {
                    "tenantId": "11111111-1111-1111-1111-111111111111",
                    "externalSubject": "user:1",
                    "avatarId": "avatar-1",
                    "walletId": "wallet-1",
                    "walletAddress": "address-1",
                    "kycStatus": "Approved",
                    "identityReady": true,
                    "kycReady": true,
                    "walletReady": true,
                    "ready": true
                  }
                }
                """);
        });
        var gateway = CreateGateway(handler);
        var binding = new AzoaCustodialAccountBinding(
            "11111111-1111-1111-1111-111111111111",
            "user:1",
            "ardanova-custodial-account:stable-key",
            "ardanova-kyc-session:stable-key");

        var result = await gateway.EnsureAsync(binding);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ArdaNovaUserId.Should().Be("user:1");
        captured!.Method.Should().Be(HttpMethod.Put);
        captured.RequestUri!.PathAndQuery.Should().Be("/api/tenant/custodial-accounts/user%3A1");
        captured.Headers.GetValues("Idempotency-Key").Should().ContainSingle()
            .Which.Should().Be(binding.IdempotencyKey);
    }

    [Fact]
    public async Task GetStatusAsync_RejectsUnknownKycState()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """
            {
              "isError": false,
              "result": {
                "tenantId": "11111111-1111-1111-1111-111111111111",
                "externalSubject": "user-1",
                "kycStatus": "VendorMystery",
                "walletReady": false,
                "ready": false
              }
            }
            """));
        var gateway = CreateGateway(handler);

        var result = await gateway.GetStatusAsync(new(
            "11111111-1111-1111-1111-111111111111",
            "user-1",
            "unused-ensure-key",
            "unused-kyc-session-key"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("invalid custodial account");
    }

    [Fact]
    public async Task BeginKycAsync_CarriesStableIdempotencyKey()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            return Json(HttpStatusCode.OK, """
                {
                  "isError": false,
                  "result": {
                    "provider": "HostedProvider",
                    "hostedVerification": true,
                    "acceptsDocumentReferences": false,
                    "providerSessionId": "must-not-cross-the-gateway",
                    "verificationUrl": "https://verify.example/session",
                    "expiresAt": "2099-01-01T00:00:00Z"
                  }
                }
                """);
        });
        var gateway = CreateGateway(handler);
        var binding = new AzoaCustodialAccountBinding(
            "11111111-1111-1111-1111-111111111111",
            "user-1",
            "ardanova-custodial-account:stable-key",
            "ardanova-kyc-session:stable-key");

        var result = await gateway.BeginKycAsync(binding);

        result.IsSuccess.Should().BeTrue();
        captured!.Headers.GetValues("Idempotency-Key").Should().ContainSingle()
            .Which.Should().Be(binding.KycSessionIdempotencyKey);
        result.Value!.Provider.Should().Be("HostedProvider");
    }

    [Fact]
    public async Task CustodialClient_RejectsValuePathBeforeNetwork()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaCustodialNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaCustodialNodeClient>.Instance);

        var result = await client.PostAsync<object>(
            "/api/allocation/11111111-1111-1111-1111-111111111111",
            new { amount = "1" },
            "value-boundary-test-key");

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("custody credential");
    }

    [Theory]
    [InlineData("/api/tenant/custodial-accounts/../allocation/11111111-1111-1111-1111-111111111111")]
    [InlineData("/api/tenant/custodial-accounts/%2e%2e/allocation/11111111-1111-1111-1111-111111111111")]
    [InlineData("/api/tenant/custodial-accounts/user%2Fallocation")]
    [InlineData("/api/tenant/custodial-accounts/user%252Fallocation")]
    public async Task CustodialClient_RejectsRouteConfusionBeforeNetwork(string path)
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaCustodialNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaCustodialNodeClient>.Instance);

        var result = await client.PostAsync<object>(
            path,
            body: null,
            "custody-boundary-test-key");

        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Fact]
    public async Task ValueClient_RejectsCustodialPathBeforeNetwork()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<object>(
            "/api/tenant/custodial-accounts/capabilities",
            body: null);

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("value credential");
    }

    [Fact]
    public async Task ValueClient_RejectsAbsoluteDestinationBeforeNetwork()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<object>(
            "https://untrusted.example/api/value",
            body: null);

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("allocation and fungible-mint");
    }

    [Fact]
    public async Task ValueClient_RejectsQuestPathBeforeNetwork()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<object>("/api/quest", new { name = "unsafe" });

        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Fact]
    public async Task QuestClient_RejectsValuePathBeforeNetwork()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaQuestNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaQuestNodeClient>.Instance);

        var result = await client.PostAsync<object>(
            "/api/allocation/11111111-1111-1111-1111-111111111111",
            new { amount = "1" });

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("quest credential");
    }

    [Theory]
    [InlineData("/api/quest/../allocation/11111111-1111-1111-1111-111111111111")]
    [InlineData("/api/quest/%2e%2e/allocation/11111111-1111-1111-1111-111111111111")]
    [InlineData("/api/quest/%252e%252e/allocation/11111111-1111-1111-1111-111111111111")]
    [InlineData("/api/quest/run%2Fsignal")]
    public async Task QuestClient_RejectsRouteConfusionBeforeNetwork(string path)
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaQuestNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaQuestNodeClient>.Instance);

        var result = await client.PostAsync<object>(path, body: null);

        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Theory]
    [InlineData("/api/tenant/custodial-accounts%2Fcapabilities")]
    [InlineData("/api/tenant/custodial-accounts-ambiguous")]
    public async Task ValueClient_RejectsAmbiguousCustodyNamespaceBeforeNetwork(string path)
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Network must not be called."));
        var client = new AzoaNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaNodeClient>.Instance);

        var result = await client.PostAsync<object>(path, body: null);

        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Fact]
    public async Task PublicRegistrationClient_SendsNoApiKey()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            return Json(HttpStatusCode.OK, """
                {
                  "isError": false,
                  "result": {
                    "id": "11111111-1111-1111-1111-111111111111",
                    "username": "user-1",
                    "email": "user-1@example.test"
                  }
                }
                """);
        });
        var client = new AzoaPublicNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaPublicNodeClient>.Instance);

        var result = await client.RegisterAvatarAsync(new AzoaAvatarRegisterRequest
        {
            Username = "user-1",
            Email = "user-1@example.test",
            Password = "development-test-password",
        });

        result.IsSuccess.Should().BeTrue();
        captured!.Headers.Contains("X-Api-Key").Should().BeFalse();
    }

    private static AzoaCustodialAccountGateway CreateGateway(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") };
        var node = new AzoaCustodialNodeClient(
            http,
            NullLogger<AzoaCustodialNodeClient>.Instance);
        return new AzoaCustodialAccountGateway(node);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(_respond(request));
    }
}
