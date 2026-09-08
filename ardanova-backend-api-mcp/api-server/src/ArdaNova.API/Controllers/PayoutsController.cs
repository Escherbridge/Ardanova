namespace ArdaNova.API.Controllers;

using System.Security.Claims;
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
        => ToActionResult(await _payoutService.RequestPayoutAsync(ActorId, dto, ct));

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
            await CashOut.QuoteSaleAsync(ActorId, projectTokenConfigId, holderClass, units, ct));
    }

    /// <summary>Everything this person holds, with what is sellable now and what is still held.</summary>
    [HttpGet("sellable")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetSellableUnits(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.GetSellableUnitsAsync(ActorId, ct));
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

        return ToActionResult(await CashOut.GetAvailabilityAsync(ActorId, ct));
    }

    /// <summary>Where this person's money goes, and whether it is ready to receive any.</summary>
    [HttpGet("withdraw/destination")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetWithdrawDestination(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.GetDestinationAsync(ActorId, ct));
    }

    /// <summary>Starts or resumes setting up where this person's money goes.</summary>
    /// <remarks>First cash-out is the one moment paperwork is expected, so it is asked for here.</remarks>
    [HttpPost("withdraw/destination")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> BeginWithdrawDestinationSetup(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.BeginDestinationSetupAsync(ActorId, ct));
    }

    /// <summary>Sends money to this person's own bank. Idempotent on the request key.</summary>
    [HttpPost("withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDto dto, CancellationToken ct)
    {
        if (dto is null)
            return BadRequest(new { error = "Enter how much you want to send." });

        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(
            await CashOut.WithdrawAsync(ActorId, dto.RequestKey, dto.AmountMinorUnits, ct));
    }

    /// <summary>This person's own withdrawals, newest first, as the node records them.</summary>
    [HttpGet("withdraw")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetWithdrawals(CancellationToken ct)
    {
        if (CashOut is null)
            return ServiceUnavailable(CashOutUnavailableMessage);

        return ToActionResult(await CashOut.GetWithdrawalsAsync(ActorId, ct));
    }

    // -- Plumbing -------------------------------------------------------------

    /// <summary>The cash-out half of cashing out, or null where this deployment has none.</summary>
    private ICashOutService? CashOut => _cashOut;

    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IActionResult ServiceUnavailable(string message)
        => StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = message });

    /// <summary>
    /// Carries a service answer to a status code WITH its wording.
    /// </summary>
    /// <remarks>
    /// A refusal a person can act on - "these units are held until March" - has to reach them, so
    /// Forbidden becomes a 403 carrying its message rather than <c>Forbid()</c>, which returns an
    /// empty body and turns an explanation into a blank wall.
    /// </remarks>
    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
