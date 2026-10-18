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
                case HttpStatusCode.BadRequest:
                    return Result<OgaiTurn>.ValidationError("Nova AI could not use that input. Check the idea and answers, then try again.");
                case HttpStatusCode.Unauthorized:
                    _logger.LogError("Nova upstream rejected the partner key; check Ogai:PartnerApiKey");
                    return Result<OgaiTurn>.Failure(NovaUpstreamErrors.Unavailable);
                case HttpStatusCode.TooManyRequests when await ErrorCodeAsync(response, ct) == "quota":
                    return Result<OgaiTurn>.Conflict(NovaUpstreamErrors.Quota);
                default:
                    _logger.LogWarning("Nova upstream answered {Status}", (int)response.StatusCode);
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

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>Error codes an <see cref="IOgaiClient"/> failure carries.</summary>
public static class NovaUpstreamErrors
{
    public const string Unavailable = "nova_unavailable";
    public const string Quota = "nova_daily_limit";
}
