namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class CashOutVoidGatewayTests
{
    [Fact]
    public async Task VoidCarriesOnlyTheServerResolvedActorAndOriginalKey()
    {
        var actor = Guid.NewGuid();
        var handler = new Handler(HttpStatusCode.OK, "{\"isError\":false,\"result\":{\"outcome\":\"voided\"}}");
        var client = new AzoaCashOutClient(new HttpClient(handler) { BaseAddress = new Uri("https://node.example") }, NullLogger<AzoaCashOutClient>.Instance);
        var result = await client.VoidRequestAsync(actor, "original-key");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Outcome.Should().Be("voided");
        handler.Path.Should().Be($"/api/fiat/payouts/{actor:D}/requests/original-key/void");
        handler.Method.Should().Be(HttpMethod.Post);
    }

    [Theory]
    [InlineData(200, "{\"isError\":false,\"result\":{\"outcome\":\"mystery\"}}")]
    [InlineData(404, "{\"isError\":true,\"code\":\"NOT_FOUND\",\"message\":\"absent\"}")]
    [InlineData(403, "{\"isError\":true,\"code\":\"FORBIDDEN\",\"message\":\"consent revoked\"}")]
    public async Task RefusalOrUnknownEnvelopeNeverInventsVoidProof(int status, string body)
    {
        var handler = new Handler((HttpStatusCode)status, body);
        var client = new AzoaCashOutClient(new HttpClient(handler) { BaseAddress = new Uri("https://node.example") }, NullLogger<AzoaCashOutClient>.Instance);
        var result = await client.VoidRequestAsync(Guid.NewGuid(), "original-key");
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Path;
        public HttpMethod? Method;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Path = request.RequestUri!.AbsolutePath;
            Method = request.Method;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
