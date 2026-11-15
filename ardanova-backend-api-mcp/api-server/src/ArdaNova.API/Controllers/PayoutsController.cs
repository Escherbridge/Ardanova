namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using System.Globalization;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>What a person asks for when they want money sent to their bank.</summary>
public sealed class WithdrawRequestDto
{
    /// <summary>
    /// Caller-stable key. The same key returns the same withdrawal instead of starting a second
    /// one, so a double-tapped button cannot send the money twice.
    /// </summary>
    public string RequestKey { get; set; } = string.Empty;

    /// <summary>How much to send, in cents. Money, never a unit count.</summary>
    public long AmountMinorUnits { get; set; }
}

/// <summary>
/// Cashing out, in the two steps a person actually takes: sell units for funds, then send those
/// funds to their own bank.
/// </summary>
/// <remarks>
/// <para><b>Whose account.</b> Every route acts for the AUTHENTICATED actor and nothing else. No
/// body field names a subject, and the bank destination is resolved by the node from that
/// person's own setup record, never from anything a caller sends.</para>
/// <para><b>Where the decision lives.</b> Nothing is gated here. The service runs one assessment -
/// account readiness, project provisioning, liquidity, the worker's hold window, the amount, the
/// exact price - for the quote and for the sale alike, so this controller only carries its answer
/// to a status code.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class PayoutsController : ControllerBase
{
    /// <summary>Said when this deployment has no route to the node's off-ramp at all.</summary>
    private const string CashOutUnavailableMessage =
        "Sending money to a bank account is not switched on for this deployment.";

    private readonly IPayoutService _payoutService;
    private readonly ICashOutService? _cashOut;

    /// <param name="cashOut">
    /// The money half of cashing out. Optional so the type still constructs without a container;
    /// a null is a deployment whose payout service does not offer it, which is a stated 503.
    /// </param>
    public PayoutsController(IPayoutService payoutService, ICashOutService? cashOut = null)
    {
        _payoutService = payoutService;
        _cashOut = cashOut;
    }

    // -- Step one: sell units for funds --------------------------------------

    /// <summary>Sells units for platform funds. The node moves them; this records what it did.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> RequestPayout([FromBody] CreatePayoutRequestDto dto, CancellationToken ct)
        => BadRequest(new { error = "Use a server-priced quote and a request key to start a sale." });

    [HttpPost("sale/quotes")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CreateSaleQuote([FromBody] CreateUnitSaleQuote dto, CancellationToken ct)
        => _payoutService is IQuotedSaleService sales
            ? QuoteAction(await sales.CreateSaleQuoteAsync(ActorId, dto, ct))
            : ServiceUnavailable(CashOutUnavailableMessage);

    [HttpPost("sale")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> AcceptSaleQuote([FromBody] AcceptUnitSaleQuote dto, CancellationToken ct)
        => _payoutService is IQuotedSaleService sales
            ? SaleAction(await sales.AcceptSaleQuoteAsync(ActorId, dto, ct))
            : ServiceUnavailable(CashOutUnavailableMessage);

    [HttpGet("sale/{id}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> ReadSale(string id, CancellationToken ct)
        => _payoutService is IQuotedSaleService sales
            ? SaleAction(await sales.ReadSaleAsync(ActorId, id, ct))
            : ServiceUnavailable(CashOutUnavailableMessage);

    [HttpPost("sale/requests/{requestKey}/void")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> VoidSaleRequest(string requestKey, [FromBody] SaleVoidRequestDto dto, CancellationToken ct)
    {
        if (_payoutService is not IQuotedSaleService sales) return ServiceUnavailable(CashOutUnavailableMessage);
        var result = await sales.VoidSaleRequestAsync(ActorId, new(requestKey, dto.QuoteId), ct);
        if (result.IsFailure || result.Value is not { } resolution)
            return ToActionResult(result, MoneyRefusalContext.CashOutGeneral);
        if (resolution.Outcome == "voided") return Ok(new { outcome = "voided" });
        if (resolution.Sale is not { ArdaMinorUnits: not null } sale)
            return Conflict(new { error = "The exact sale record needs reconciliation." });
        return Ok(new { outcome = "revealed", sale = SdkSaleBody(sale) });
    }

    private IActionResult QuoteAction(Result<UnitSaleQuote> result)
    {
        if (result.IsFailure || result.Value is not { } quote)
            return ToActionResult(result, MoneyRefusalContext.CashOutGeneral);
        return Ok(new
        {
            quote.QuoteId, quote.ProjectTokenConfigId, quote.UnitName, quote.SourceUnitAmount,
            quantityBaseUnits = quote.SourceUnitAmount.ToString(CultureInfo.InvariantCulture),
            platformMinorUnits = quote.PlatformMinorUnits.ToString(CultureInfo.InvariantCulture),
            usdReferenceMinorUnits = quote.UsdReferenceMinorUnits.ToString(CultureInfo.InvariantCulture),
            sellableUnitCount = quote.SellableUnitCount.ToString(CultureInfo.InvariantCulture),
            quote.RestrictedUntil, quote.CanSell, quote.StatusMessage, quote.ExpiresAt, quote.CurrencyCode, quote.CurrencyDecimals,
        });
    }

    private IActionResult SaleAction(Result<ArdaNova.Application.DTOs.PayoutRequestDto> result)
    {
        if (result.IsFailure || result.Value is not { } sale)
            return ToActionResult(result, MoneyRefusalContext.CashOutGeneral);
        if (sale.ArdaMinorUnits is null)
            return Conflict(new { error = "The exact sale amount needs reconciliation." });
        return Ok(SdkSaleBody(sale));
    }

    private static object SdkSaleBody(PayoutRequestDto sale)
        => new
        {
            sale.Id, sale.Status, sale.SourceTokenAmount,
            quantityBaseUnits = sale.SourceTokenAmount.ToString(CultureInfo.InvariantCulture),
            ardaMinorUnits = sale.ArdaMinorUnits!.Value.ToString(CultureInfo.InvariantCulture),
            usdReferenceMinorUnits = sale.UsdReferenceMinorUnits?.ToString(CultureInfo.InvariantCulture),
            sale.CurrencyCode, sale.CurrencyDecimals, sale.RequestedAt, sale.ProcessedAt, sale.CompletedAt, sale.FailureReason,
            statusMessage = sale.Status switch
            {
                PayoutStatus.COMPLETED => "The sale is complete. Funds are in your balance.",
                PayoutStatus.FAILED or PayoutStatus.CANCELLED => sale.FailureReason ?? "No units were sold.",
                _ => "The sale is still being confirmed. Your units stay set aside.",
            },
        };

    /// <summary>What a sale would pay, checked against the same gates the sale itself faces.</summary>
    [HttpGet("quote")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> QuoteSale(
        [FromQuery] string projectTokenConfigId,
        [FromQuery] TokenHolderClass holderClass,
        [FromQuery] int units,
        CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(
            await CashOut.QuoteSaleAsync(ActorId, projectTokenConfigId, holderClass, units, ct),
            MoneyRefusalContext.CashOutGeneral);
    }

    /// <summary>Everything this person holds, with what is sellable now and what is still held.</summary>
    [HttpGet("sellable")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetSellableUnits(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.GetSellableUnitsAsync(ActorId, ct), MoneyRefusalContext.CashOutGeneral);
    }

    /// <summary>
    /// Re-reads a sale the node left ambiguous and settles the row on what it finds. Creates no
    /// economic effect: the receipt route is a read under the key the sale was submitted with.
    /// </summary>
    [HttpPost("{payoutRequestId}/process")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> ProcessPayout(string payoutRequestId, CancellationToken ct)
        => ToActionResult(await _payoutService.ProcessPayoutAsync(payoutRequestId, ct));

    /// <summary>Calls off a sale that has not gone through, and returns the units.</summary>
    [HttpPost("{payoutRequestId}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> CancelPayout(string payoutRequestId, CancellationToken ct)
    {
        var mine = await _payoutService.GetPayoutsByUserAsync(ActorId, ct);
        if (!mine.IsSuccess)
            return ToActionResult(mine);
        if (!mine.Value!.Any(payout => string.Equals(payout.Id, payoutRequestId, StringComparison.Ordinal)))
            return Forbid();

        return ToActionResult(await _payoutService.CancelPayoutAsync(payoutRequestId, ct));
    }

    /// <summary>This person's own sales, whatever state they are in.</summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => ToActionResult(await _payoutService.GetPayoutsByUserAsync(ActorId, ct));

    /// <summary>Sales an operator still has to account for: waiting, or parked mid-flight.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = AuthorizationPolicies.AdminApiKey)]
    public async Task<IActionResult> GetPendingPayouts(CancellationToken ct)
        => ToActionResult(await _payoutService.GetPendingPayoutsAsync(ct));

    // -- Step two: send the funds to a bank ----------------------------------

    /// <summary>What this person can send to their bank today, and what is still held.</summary>
    [HttpGet("withdraw/availability")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetWithdrawAvailability(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.GetAvailabilityAsync(ActorId, ct), MoneyRefusalContext.CashOutGeneral);
    }

    /// <summary>Where this person's money goes, and whether it is ready to receive any.</summary>
    [HttpGet("withdraw/destination")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetWithdrawDestination(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        // A READ: F-H8 guards only the setup POST below, so a 403 here is the generic cash-out gate.
        return ToActionResult(await CashOut.GetDestinationAsync(ActorId, ct), MoneyRefusalContext.CashOutGeneral);
    }

    /// <summary>Starts or resumes setting up where this person's money goes.</summary>
    /// <remarks>First cash-out is the one moment paperwork is expected, so it is asked for here.</remarks>
    [HttpPost("withdraw/destination")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> BeginWithdrawDestinationSetup(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return StatusCode(StatusCodes.Status403Forbidden, MoneyRefusal.ToErrorBody(MoneyRefusal.For(MoneyRefusalCodes.AccountHolderRequired,
            "Continue through the secure account handoff before setting up your bank.")));
    }

    [HttpPost("withdraw/requests/{requestKey}/void")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> VoidWithdrawal(string requestKey, CancellationToken ct)
    {
        if (CashOut is null) return ServiceUnavailable(CashOutUnavailableMessage);
        var result = await CashOut.VoidWithdrawalAsync(ActorId, requestKey, ct);
        if (result.IsSuccess && result.Value is { Outcome: "voided", CashOut: null })
            return Ok(new { outcome = "voided" });
        if (result.IsSuccess && result.Value is { Outcome: "revealed", CashOut: { } cashOut })
            return Ok(new { outcome = "revealed", cashOut });
        return Conflict(MoneyRefusal.ToErrorBody(MoneyRefusal.ForFailedVoid(result,
            MoneyRefusalContext.CashOutGeneral, "The request could not be resolved. Keep its saved key.")));
    }

    /// <summary>Sends money to this person's own bank. Idempotent on the request key.</summary>
    [HttpPost("withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDto dto, CancellationToken ct)
    {
        if (dto is null)
        {
            return BadRequest(MoneyRefusal.ToErrorBody(
                MoneyRefusal.For(MoneyRefusalCodes.Definite, "Enter how much you want to send.")));
        }

        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(
            await CashOut.WithdrawAsync(ActorId, dto.RequestKey, dto.AmountMinorUnits, ct),
            MoneyRefusalContext.CashOutGeneral);
    }

    /// <summary>This person's own withdrawals, newest first, as the node records them.</summary>
    [HttpGet("withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetWithdrawals(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.GetWithdrawalsAsync(ActorId, ct), MoneyRefusalContext.CashOutGeneral);
    }

    // -- Plumbing -------------------------------------------------------------

    /// <summary>The cash-out half of cashing out, or null where this deployment has none.</summary>
    private ICashOutService? CashOut => _cashOut;

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IActionResult ServiceUnavailable(string message)
        => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            MoneyRefusal.ToErrorBody(MoneyRefusal.For(MoneyRefusalCodes.PaymentsNotConfigured, message)));

    /// <summary>
    /// Carries a service answer to a status code WITH its wording.
    /// </summary>
    /// <remarks>
    /// A refusal a person can act on - "these units are held until March" - has to reach them, so
    /// Forbidden becomes a 403 carrying its message rather than <c>Forbid()</c>, which returns an
    /// empty body and turns an explanation into a blank wall. <paramref name="context"/> defaults
    /// to <see cref="MoneyRefusalContext.General"/> for the <c>IPayoutService</c>-routed actions
    /// (sale-side refusals have no single dominant node cause the way the cash-out routes do).
    /// </remarks>
    private IActionResult ToActionResult<T>(Result<T> result, MoneyRefusalContext context = MoneyRefusalContext.General)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        var body = MoneyRefusal.ToErrorBody(MoneyRefusal.From(result, context));
        return result.Type switch
        {
            ResultType.NotFound => NotFound(body),
            ResultType.ValidationError or ResultType.BadRequest => BadRequest(body),
            ResultType.Unauthorized => Unauthorized(body),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            ResultType.Conflict => Conflict(body),
            // A transport failure is the platform's, never the caller's request: 503, not 400.
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, body),
        };
    }
}

public sealed record SaleVoidRequestDto(string QuoteId);
