namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using ArdaNova.Application.Common.Results;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class AzoaNodeClientSecurityTests
{
    private const string ProviderException =
        "System.InvalidOperationException: database password=super-secret at Internal.Handler";

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ResultType.BadRequest, "AZOA node rejected the request.")]
    [InlineData(HttpStatusCode.Unauthorized, ResultType.Unauthorized, "AZOA node authentication failed.")]
    [InlineData(HttpStatusCode.Forbidden, ResultType.Forbidden, "AZOA node denied the request.")]
    [InlineData(HttpStatusCode.NotFound, ResultType.NotFound, "AZOA resource was not found.")]
    [InlineData(HttpStatusCode.Conflict, ResultType.Conflict, "AZOA request conflicted with existing state.")]
    [InlineData(HttpStatusCode.TooManyRequests, ResultType.Failure, "AZOA node rate limit exceeded.")]
    [InlineData(HttpStatusCode.InternalServerError, ResultType.Failure, "AZOA node request failed.")]
    public async Task ErrorEnvelope_DoesNotExposeProviderMessage(
        HttpStatusCode status,
        ResultType expectedType,
        string expectedMessage)
    {
        var client = CreateClient(status, ErrorEnvelope(ProviderException));

        var result = await client.PostAsync<object>("/api/nft/fungible-mint", new { amount = 1 });

        result.Type.Should().Be(expectedType);
        result.Error.Should().Be(expectedMessage);
        result.Error.Should().NotContain("super-secret").And.NotContain("Internal.Handler");
    }

    [Fact]
    public async Task KycForbiddenError_PreservesOnlySanitizedContractSignal()
    {
        var client = CreateClient(
            HttpStatusCode.Forbidden,
            ErrorEnvelope($"KYC_FORBIDDEN: {ProviderException}"));

        var result = await client.PostAsync<object>("/api/nft/fungible-mint", new { amount = 1 });

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Be("KYC_FORBIDDEN: AZOA approval is required.");
    }

    [Fact]
    public async Task SuccessEnvelopeWithoutResult_DoesNotExposeProviderMessage()
    {
        var client = CreateClient(
            HttpStatusCode.OK,
            $$"""{"isError":false,"message":"{{ProviderException}}","result":null}""");

        var result = await client.PostAsync<object>("/api/nft/fungible-mint", new { amount = 1 });

        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Be("AZOA node returned no result payload.");
    }

    private static AzoaNodeClient CreateClient(HttpStatusCode status, string json)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return new AzoaNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaNodeClient>.Instance);
    }

    private static string ErrorEnvelope(string message)
        => $$"""{"isError":true,"message":"{{message}}","result":null}""";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
