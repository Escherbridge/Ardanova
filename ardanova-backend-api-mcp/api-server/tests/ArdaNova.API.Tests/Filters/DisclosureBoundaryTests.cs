using System.Text.Json;
using ArdaNova.API.Filters;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArdaNova.API.Tests.Filters;

/// <summary>What leaves the API when something fails or a storage URL is minted: no exception text, no script-capable uploads, bounded links.</summary>
public sealed class DisclosureBoundaryTests
{
    [Theory]
    [InlineData(500, false)]
    [InlineData(502, false)]
    [InlineData(504, false)]
    [InlineData(501, true)]
    [InlineData(503, true)]
    public void ServerFaultBodies_KeepOnlyAuthoredUnavailabilityMessages(int status, bool keepsMessage)
    {
        const string authored = "Npgsql.PostgresException: password authentication failed for user \"ardanova\"";
        var normalized = ErrorEnvelopeResultFilter.Normalize(
            new DefaultHttpContext(),
            new ObjectResult(new { error = authored }) { StatusCode = status });

        var body = JsonSerializer.Serialize(((ObjectResult)normalized).Value);
        body.Contains("Npgsql").Should().Be(keepsMessage);
        if (!keepsMessage)
            body.Should().Contain(ExceptionHandlingMiddleware.ServerErrorMessage);
    }

    [Theory]
    [InlineData("image/png", true)]
    [InlineData("application/pdf; charset=binary", true)]
    [InlineData("text/html", false)]
    [InlineData("image/svg+xml", false)]
    [InlineData("Application/JavaScript", false)]
    [InlineData("not a type", false)]
    [InlineData("", false)]
    public void UploadContentTypes_ThatABrowserWouldExecute_AreRefused(string contentType, bool allowed)
        => UploadContentPolicy.IsAllowedContentType(contentType).Should().Be(allowed);

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(30, 30)]
    [InlineData(525_600, 60)]
    public void DownloadLinkLifetimes_AreBounded(int requested, int granted)
        => UploadContentPolicy.ClampDownloadMinutes(requested).Should().Be(granted);

    [Fact]
    public void AttachmentNames_CannotBreakOutOfTheContentDispositionQuote()
        => UploadContentPolicy.SafeAttachmentName("evil\".html\r\nSet-Cookie: x=1")
            .Should().Be("evil.htmlSet-Cookie: x=1");
}
