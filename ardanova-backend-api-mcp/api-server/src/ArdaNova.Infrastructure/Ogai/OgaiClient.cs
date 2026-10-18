namespace ArdaNova.Infrastructure.Ogai;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs.Nova;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>Typed HttpClient for POST /api/partner/v1/validate; base address, bearer key and timeout are set at registration.</summary>
public sealed class OgaiClient : IOgaiClient
{
    public const string ValidatePath = "api/partner/v1/validate";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<OgaiClient> _logger;

    public OgaiClient(HttpClient http, ILogger<OgaiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Availability => NovaAvailability.Live;

    public async Task<Result<OgaiTurn>> ValidateAsync(
        string idea,
        IReadOnlyList<NovaTurnDto> turns,
        string? state,
        string? sessionId,
        CancellationToken ct = default)
    {
        var body = new
        {
            idea,
            turns = turns.Select(turn => new { question = turn.Question, answer = turn.Answer }),
            state,
            sessionId,
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(ValidatePath, body, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // Logs the failure kind only: never the idea, answers or key.
            _logger.LogWarning("Nova upstream unreachable: {Kind}", ex.GetType().Name);
            return Result<OgaiTurn>.Failure(NovaUpstreamErrors.Unavailable);
        }

        using (response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var turn = await TryReadAsync(response, ct);
                    return turn is null
                        ? Result<OgaiTurn>.Failure(NovaUpstreamErrors.Unavailable)
                        : Result<OgaiTurn>.Success(turn);
                case HttpStatusCode.Unauthorized:
                    _logger.LogError("Nova upstream rejected the partner key; check Ogai:PartnerApiKey");
                    return Result<OgaiTurn>.Failure(NovaUpstreamErrors.Unavailable);
            }

            var (code, reason) = await ErrorCodeAsync(response, ct);
            switch (response.StatusCode, code, reason)
            {
                case (HttpStatusCode.BadRequest, "session", _):
                    return Result<OgaiTurn>.NotFound(NovaErrors.SessionNotFound);
                case (HttpStatusCode.BadRequest, _, _):
                    return Result<OgaiTurn>.ValidationError("Nova AI could not use that input. Check the idea and answers, then try again.");
                // The partner's whole day (sessions or spend), shared by every member: capacity, not this member's limit.
                case (HttpStatusCode.TooManyRequests, "quota", _):
                case (HttpStatusCode.ServiceUnavailable, _, "spend_closed"):
                    _logger.LogWarning("Nova upstream at partner capacity: {Status} {Reason}", (int)response.StatusCode, reason ?? code);
                    return Result<OgaiTurn>.Failure(NovaErrors.Capacity);
                default:
                    _logger.LogWarning("Nova upstream answered {Status} {Reason}", (int)response.StatusCode, reason);
                    return Result<OgaiTurn>.Failure(NovaUpstreamErrors.Unavailable);
            }
        }
    }

    private async Task<OgaiTurn?> TryReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<OgaiTurn>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Nova upstream returned malformed JSON");
            return null;
        }
    }

    /// <summary>The partner API's machine-readable error and reason codes, when the body carries them.</summary>
    private static async Task<(string? Code, string? Reason)> ErrorCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, null);
            return (StringProperty(root, "error"), StringProperty(root, "reason"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string? StringProperty(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

/// <summary>Error codes an <see cref="IOgaiClient"/> failure carries.</summary>
public static class NovaUpstreamErrors
{
    public const string Unavailable = NovaErrors.Unavailable;
    public const string Capacity = NovaErrors.Capacity;
}
