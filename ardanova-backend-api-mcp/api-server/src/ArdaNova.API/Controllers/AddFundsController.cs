namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>How much a person wants to add, in whole cents.</summary>
public sealed class AddFundsRequestDto
{
    /// <summary>Cents, never a decimal number of dollars, so no rounding argument can reach money.</summary>
    public long AmountMinorUnits { get; init; }
}

/// <summary>One add-funds payment, as much of it as the payer's own page is allowed to know.</summary>
/// <param name="PaymentId">The same value the return address carries back as <c>?intent=</c>.</param>
/// <param name="Status">The provider-neutral payment word. Never rendered; the client maps it to copy.</param>
/// <param name="SettlementState">The provider-neutral settlement word. Outranks <paramref name="Status"/>.</param>
/// <param name="PaymentUrl">The hosted payment page, while this payment is still open.</param>
/// <param name="Resumed">True when this call resumed an open payment instead of starting a second one.</param>
/// <param name="AmountPaidMinorUnits">The amount that actually cleared. Null until it has.</param>
/// <param name="CurrencyCode">The currency the payment cleared in.</param>
/// <param name="ExpiresAt">When an unfinished payment stops being resumable.</param>
public sealed record AddFundsPaymentDto(
    string PaymentId,
    string Status,
    string SettlementState,
    string? PaymentUrl,
    bool Resumed,
    long? AmountPaidMinorUnits,
    string? CurrencyCode,
    DateTimeOffset? ExpiresAt);

/// <summary>
/// The only route by which a person can add funds, and the only route that says what happened to
/// one.
/// </summary>
/// <remarks>
/// <para><b>Whose money.</b> The account a payment is started for, and the account a payment is
/// read against, are both derived from the signed actor assertion. Neither is ever taken from a
/// body field or a query value, so no request can aim a payment at, or read a payment belonging
/// to, somebody else. <see cref="IFiatRailGateway.GetSessionAsync"/> is account-scoped, so a
/// guessed payment id read under the caller's own account resolves to nothing.</para>
///
/// <para><b>Never guess.</b> A browser redirect is not a receipt. The return address carries only
/// an opaque payment id; this controller re-reads that payment from the node and reports the
/// node's own words. When it cannot read one, it says so - it never promotes an unread payment
/// to a finished one.</para>
///
/// <para><b>Where a person is sent back to.</b> The return address is composed from this
/// deployment's own configuration, never from the request. A blank setting resolves to the
/// narrowest outcome explicitly - outside Development there is nowhere to come back to, so the
/// payment is refused rather than started against a guessed address.</para>
///
/// <para><b>What this controller does NOT do.</b> It never credits anything, and it holds no card
/// credential. A payment becomes spendable funds only when the node's own settlement applies it,
/// which is why a just-paid read still reports <c>payment_received</c> rather than success.</para>
/// </remarks>
[ApiController]
[Route("api/add-funds")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public sealed class AddFundsController : ControllerBase
{
    /// <summary>Names the action inside the readiness gate's own "before you can ..." sentence.</summary>
    private const string AddFundsAction = "add funds";

    private const string IdempotencyHeader = "X-Idempotency-Key";

    /// <summary>Where a payment sends the person back to. Absolute; the page path is appended here.</summary>
    public const string ClientBaseUrlConfigurationKey = "ArdaNova:ClientBaseUrl";

    /// <summary>The environment variable the same address may be supplied as.</summary>
    public const string ClientBaseUrlEnvironmentKey = "NEXTJS_URL";

    /// <summary>The one address a local developer may fall back to. Development only, by design.</summary>
    private const string DevelopmentClientBaseUrl = "http://localhost:3000";

    /// <summary>The page a payment returns to. The node appends the payment id to it.</summary>
    private const string ReturnPath = "/portfolio/add-funds";

    private const string SetUpFirstMessage =
        "Your account is still being set up, so there is nothing to add funds to yet.";

    private const string NotSwitchedOnMessage =
        "Adding funds isn't switched on for this site yet. Nothing was charged.";

    private const string CouldNotConfirmMessage =
        "We couldn't confirm this payment just now, so we won't say either way. Nothing has changed.";

    private const string AmountRequiredMessage = "Choose an amount to add.";

    private readonly IFiatRailGateway _fiatRail;
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly IAzoaCustodialAccountService _accounts;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AddFundsController> _logger;

    public AddFundsController(
        IFiatRailGateway fiatRail,
        IAzoaValueEligibilityService valueEligibility,
        IAzoaCustodialAccountService accounts,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AddFundsController> logger)
    {
        _fiatRail = fiatRail;
        _valueEligibility = valueEligibility;
        _accounts = accounts;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Starts, or resumes on the same request key, one payment that adds funds.</summary>
    [HttpPost]
    public async Task<IActionResult> Begin([FromBody] AddFundsRequestDto request, CancellationToken ct)
    {
        if (request is null || request.AmountMinorUnits <= 0)
            return BadRequest(new { error = AmountRequiredMessage });

        if (!Request.Headers.TryGetValue(IdempotencyHeader, out var suppliedKey) || suppliedKey.Count != 1)
            return BadRequest(new { error = IdempotencyHeader + " is required" });

        var requestKey = suppliedKey[0]?.Trim();
        if (string.IsNullOrEmpty(requestKey))
            return BadRequest(new { error = IdempotencyHeader + " is required" });

        // The gate has four outcomes, not two, and each one owns different words: ready, setup
        // genuinely unfinished, held for a human decision, and could-not-confirm. Propagating the
        // gate's own status code keeps a held person out of the "finish your setup" copy they
        // cannot act on.
        var readiness = await _valueEligibility.RequireReadyAsync(ActorId, AddFundsAction, ct);
        if (!readiness.IsSuccess)
        {
            return StatusCode(
                ValueEligibilityFailure.StatusCode(readiness),
                new { error = readiness.Error ?? SetUpFirstMessage });
        }

        if (!Guid.TryParse(readiness.Value?.AvatarId, out var accountId) || accountId == Guid.Empty)
            return Conflict(new { error = SetUpFirstMessage });

        if (!TryResolveReturnAddress(out var returnAddress))
        {
            _logger.LogError(
                "Adding funds was refused because no return address is configured. Set {ConfigurationKey} "
                + "or the {EnvironmentKey} environment variable to this site's absolute address.",
                ClientBaseUrlConfigurationKey,
                ClientBaseUrlEnvironmentKey);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = NotSwitchedOnMessage });
        }

        var started = await _fiatRail.BeginAddFundsAsync(
            new FiatCheckoutStart(
                accountId,
                requestKey,
                request.AmountMinorUnits,
                returnAddress,
                returnAddress),
            ct);

        return ToActionResult(started);
    }

    /// <summary>Reads one payment belonging to the caller. A payment it cannot read stays unresolved.</summary>
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken ct)
    {
        if (paymentId == Guid.Empty)
            return BadRequest(new { error = CouldNotConfirmMessage });

        // Deliberately NOT the full readiness gate. Somebody whose account went under review after
        // they paid still owns that payment and is still entitled to be told what happened to it;
        // refusing the read would leave them staring at money they cannot account for.
        var account = await _accounts.GetStatusAsync(ActorId, ct);
        if (!account.IsSuccess)
            return Conflict(new { error = CouldNotConfirmMessage });

        if (!Guid.TryParse(account.Value?.AvatarId, out var accountId) || accountId == Guid.Empty)
            return Conflict(new { error = SetUpFirstMessage });

        // The account is the caller's own, so a guessed payment id reads nothing rather than
        // somebody else's payment.
        var payment = await _fiatRail.GetSessionAsync(accountId, paymentId, ct);
        return ToActionResult(payment);
    }

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Composes the address a payment returns to from configuration alone. Blank resolves to the
    /// narrowest setting in code - a refusal - rather than to whatever a caller supplied.
    /// </summary>
    private bool TryResolveReturnAddress(out string returnAddress)
    {
        returnAddress = string.Empty;

        var configured = _configuration[ClientBaseUrlConfigurationKey];
        if (string.IsNullOrWhiteSpace(configured))
            configured = _configuration[ClientBaseUrlEnvironmentKey];

        if (string.IsNullOrWhiteSpace(configured))
        {
            if (!_environment.IsDevelopment())
                return false;

            configured = DevelopmentClientBaseUrl;
        }

        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var baseAddress)
            || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(baseAddress.UserInfo))
        {
            return false;
        }

        returnAddress = new Uri(baseAddress, ReturnPath).ToString();
        return true;
    }

    private IActionResult ToActionResult(Result<FiatCheckoutSession> result)
    {
        if (result.IsSuccess && result.Value is { } session)
        {
            return Ok(new AddFundsPaymentDto(
                session.IntentId.ToString("N"),
                session.Status,
                session.SettlementState,
                session.PaymentUrl,
                session.Resumed,
                session.AmountPaidMinorUnits,
                session.CurrencyCode,
                session.ExpiresAt));
        }

        var error = result.Error ?? CouldNotConfirmMessage;
        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error }),
            ResultType.Unauthorized => Unauthorized(new { error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error }),
            ResultType.Conflict => Conflict(new { error }),
            ResultType.ValidationError => BadRequest(new { error }),
            ResultType.BadRequest => BadRequest(new { error }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new { error }),
        };
    }
}
