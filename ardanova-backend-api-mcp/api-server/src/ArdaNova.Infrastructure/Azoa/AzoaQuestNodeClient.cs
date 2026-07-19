namespace ArdaNova.Infrastructure.Azoa;

using System.Net.Http.Json;
using ArdaNova.Application.Common.Results;
using Microsoft.Extensions.Logging;

/// <summary>Typed transport constrained to authenticated quest routes.</summary>
public sealed class AzoaQuestNodeClient : IAzoaQuestNodeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AzoaQuestNodeClient> _logger;

    public AzoaQuestNodeClient(
        HttpClient http,
        ILogger<AzoaQuestNodeClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
        => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), ct);

    public Task<Result<T>> PostAsync<T>(
        string path,
        object? body,
        CancellationToken ct = default)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        return SendAsync<T>(message, ct);
    }

    private Task<Result<T>> SendAsync<T>(HttpRequestMessage message, CancellationToken ct)
    {
        if (!AzoaNodePathPolicy.IsQuestPath(message.RequestUri?.OriginalString))
        {
            message.Dispose();
            return Task.FromResult(Result<T>.Forbidden(
                "The AZOA quest credential is restricted to quest endpoints."));
        }

        return AzoaNodeClient.SendAsync<T>(_http, _logger, message, ct);
    }
}
