namespace ArdaNova.Infrastructure.Azoa;

using System.Net.Http.Json;
using ArdaNova.Application.Common.Results;
using Microsoft.Extensions.Logging;

/// <summary>Credential-free client for anonymous AZOA registration.</summary>
public sealed class AzoaPublicNodeClient : IAzoaPublicNodeClient
{
    private const string RegistrationPath = "/api/avatar/register";

    private readonly HttpClient _http;
    private readonly ILogger<AzoaPublicNodeClient> _logger;

    public AzoaPublicNodeClient(
        HttpClient http,
        ILogger<AzoaPublicNodeClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<Result<AzoaAvatar>> RegisterAvatarAsync(
        AzoaAvatarRegisterRequest request,
        CancellationToken ct = default)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, RegistrationPath)
        {
            Content = JsonContent.Create(request),
        };
        return AzoaNodeClient.SendAsync<AzoaAvatar>(_http, _logger, message, ct);
    }
}
