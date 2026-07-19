namespace ArdaNova.API.Controllers;

using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Retired local KYC surface; AZOA is the authoritative provider boundary.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public sealed class KycController : ControllerBase
{
    private const string Message =
        "ArdaNova-local KYC is retired. Use the actor-bound /api/azoa/custodial-account/kyc flow.";

    [HttpPost("submit")]
    public IActionResult Submit() => Gone();

    [HttpGet("status/{userId}")]
    public IActionResult GetStatus(string userId) => Gone();

    [HttpGet("{id}")]
    public IActionResult GetById(string id) => Gone();

    [HttpGet("pending")]
    public IActionResult GetPending() => Gone();

    [HttpPost("{id}/approve")]
    public IActionResult Approve(string id) => Gone();

    [HttpPost("{id}/reject")]
    public IActionResult Reject(string id) => Gone();

    private ObjectResult Gone()
        => StatusCode(StatusCodes.Status410Gone, new { error = Message });
}
