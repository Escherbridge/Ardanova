namespace ArdaNova.Application.Services.Implementations;

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>Operator channel that posts each economic alert to a configured webhook endpoint.</summary>
/// <remarks>
/// <para>
/// A webhook is the one delivery every environment here can actually make: the API already speaks
/// outbound HTTP to the AZOA node, and every pager, chat room and on-call product this deployment
/// might use accepts an inbound JSON POST. Email would need an SMTP relay this codebase does not
/// have, so it would be a stub wearing an implementation's name.
/// </para>
/// <para>
/// The webhook is ADDITIVE, never a replacement. Every alert is written to the structured log
/// FIRST, through <see cref="LoggingOperatorAlertService"/>, and only then offered to the endpoint.
/// So a webhook that is unconfigured, refusing, or timing out cannot erase the record of parked
/// value - it can only fail to wake somebody faster, and it says so at Critical level when it does.
/// </para>
/// <para>
/// Blank resolves to UNCONFIGURED, explicitly. Configuration binds an environment variable set to
/// the empty string over any default, and an empty or whitespace endpoint - or one that is not an
/// absolute http/https URI - is the shape a half-finished deployment produces most easily. Treating
/// it as "no channel, say so loudly" is the only reading that does not silently drop alerts about
/// money nobody can move.
/// </para>
/// <para>
/// The JSON property names below are the alert contract, exactly as the log property names are: an
/// operator rule matches on <c>alertCode</c>, so renaming one breaks somebody's pager.
/// </para>
/// </remarks>
public sealed class WebhookOperatorAlertService : IOperatorAlertService
{
    /// <summary>Configuration key naming the endpoint that receives alerts. Blank means no channel.</summary>
    public const string WebhookUrlKey = "OperatorAlerts:WebhookUrl";

    /// <summary>Optional key whose value is sent verbatim as the request's Authorization header.</summary>
    public const string AuthorizationHeaderKey = "OperatorAlerts:AuthorizationHeader";

    /// <summary>Optional key bounding one delivery attempt, so a hung pager cannot stall dispatch.</summary>
    public const string TimeoutSecondsKey = "OperatorAlerts:TimeoutSeconds";

    /// <summary>The header this channel sets; named here so the assembly needs no ASP.NET reference.</summary>
    private const string AuthorizationHeaderName = "Authorization";

    private static readonly TimeSpan DefaultDeliveryTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaximumDeliveryTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// One connection pool for the process. This assembly does not reference
    /// <c>Microsoft.Extensions.Http</c>, and a per-alert client exhausts sockets, so the shared
    /// instance is the right shape here; a host holding a factory passes its own client in.
    /// </summary>
    private static readonly HttpClient SharedHttpClient = new();

    private readonly LoggingOperatorAlertService _logChannel;
    private readonly ILogger<WebhookOperatorAlertService>? _logger;
    private readonly HttpClient _httpClient;
    private readonly Uri? _endpoint;
    private readonly string? _configuredEndpointText;
    private readonly string? _authorizationHeader;
    private readonly TimeSpan _deliveryTimeout;

    public WebhookOperatorAlertService(
        IConfiguration configuration,
        ILogger<WebhookOperatorAlertService>? logger = null,
        HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _logger = logger;
        _logChannel = LoggingOperatorAlertService.For(logger);
        _httpClient = httpClient ?? SharedHttpClient;

        _configuredEndpointText = configuration[WebhookUrlKey];
        _endpoint = ParseEndpoint(_configuredEndpointText);

        var authorization = configuration[AuthorizationHeaderKey];
        _authorizationHeader = string.IsNullOrWhiteSpace(authorization) ? null : authorization.Trim();

        _deliveryTimeout = ParseDeliveryTimeout(configuration[TimeoutSecondsKey]);
    }

    /// <inheritdoc/>
    public async Task RaiseAsync(EconomicOperatorAlert alert, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        // The record first, always. Delivery is best-effort; the log line is not.
        await _logChannel.RaiseAsync(alert, ct).ConfigureAwait(false);

        if (_endpoint is null)
        {
            _logger?.LogCritical(
                "OPERATOR ALERT {AlertCode} HAS NO DELIVERY CHANNEL: settlement {SettlementId} needs a "
                + "person and this deployment can only write it to the log. The configured value for "
                + "'{WebhookUrlKey}' was {ConfiguredEndpoint}; set it to an absolute http or https URL "
                + "that reaches your on-call channel.",
                alert.AlertCode,
                alert.SettlementId,
                WebhookUrlKey,
                DescribeUnusableEndpoint(_configuredEndpointText));
            return;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(_deliveryTimeout);

            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(SerializePayload(alert), Encoding.UTF8, "application/json"),
            };
            if (_authorizationHeader is not null)
                request.Headers.TryAddWithoutValidation(AuthorizationHeaderName, _authorizationHeader);

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogCritical(
                    "OPERATOR ALERT {AlertCode} WAS REFUSED BY ITS CHANNEL: {Endpoint} answered "
                    + "{StatusCode} for settlement {SettlementId}. The alert is in the log above; "
                    + "nobody has been paged.",
                    alert.AlertCode,
                    _endpoint,
                    (int)response.StatusCode,
                    alert.SettlementId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Never throw into the economic path: this runs inside outbox dispatch, and a pager
            // that is down must not turn a settlement that needs a person into a failed lease.
            _logger?.LogCritical(
                ex,
                "OPERATOR ALERT {AlertCode} COULD NOT BE DELIVERED to {Endpoint} for settlement "
                + "{SettlementId}. The alert is in the log above; nobody has been paged.",
                alert.AlertCode,
                _endpoint,
                alert.SettlementId);
        }
    }

    /// <summary>The wire shape operators match rules on; every field of the alert, nothing derived.</summary>
    private static string SerializePayload(EconomicOperatorAlert alert)
        => JsonSerializer.Serialize(
            new
            {
                alertCode = alert.AlertCode,
                severity = alert.Severity.ToString(),
                outboxId = alert.OutboxId,
                settlementId = alert.SettlementId,
                settlementKind = alert.SettlementKind.ToString(),
                beneficiaryUserId = alert.BeneficiaryUserId,
                assetCode = alert.AssetCode,
                amountBaseUnits = alert.AmountBaseUnits,
                amountScale = alert.AmountScale,
                attemptCount = alert.AttemptCount,
                failureCode = alert.FailureCode,
                summary = alert.Summary,
                requiredAction = alert.RequiredAction,
                raisedAt = DateTime.UtcNow,
            });

    /// <summary>Absolute http/https only; every other value - blank included - means no channel.</summary>
    private static Uri? ParseEndpoint(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var endpoint))
            return null;

        return endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps
            ? endpoint
            : null;
    }

    /// <summary>A bounded, positive timeout; an unreadable or out-of-range value falls back to the default.</summary>
    private static TimeSpan ParseDeliveryTimeout(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured)
            || !double.TryParse(
                configured.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds))
        {
            return DefaultDeliveryTimeout;
        }

        var requested = TimeSpan.FromSeconds(seconds);
        return requested > TimeSpan.Zero && requested <= MaximumDeliveryTimeout
            ? requested
            : DefaultDeliveryTimeout;
    }

    /// <summary>Names the unusable shape without echoing a URL that may carry a secret path.</summary>
    private static string DescribeUnusableEndpoint(string? configured)
    {
        if (configured is null)
            return "absent";
        if (configured.Length == 0)
            return "empty";
        if (string.IsNullOrWhiteSpace(configured))
            return "whitespace";
        return "not an absolute http/https URL";
    }
}
