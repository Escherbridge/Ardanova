namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/swaps")]
public class TokenSwapsController : ControllerBase
{
    private readonly ITokenSwapService _swapService;

    public TokenSwapsController(ITokenSwapService swapService)
    {
        _swapService = swapService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _swapService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _swapService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}/paged")]
    public async Task<IActionResult> GetByUserIdPaged(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _swapService.GetByUserIdPagedAsync(userId, page, pageSize, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTokenSwapDto dto, CancellationToken ct)
    {
        var result = await _swapService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> StartProcessing(string id, CancellationToken ct)
    {
        var result = await _swapService.StartProcessingAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(string id, [FromBody] CompleteSwapDto dto, CancellationToken ct)
    {
        var result = await _swapService.CompleteAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/fail")]
    public async Task<IActionResult> Fail(string id, CancellationToken ct)
    {
        var result = await _swapService.FailAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var result = await _swapService.CancelAsync(id, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

[ApiController]
[Route("api/liquidity-pools")]
public class LiquidityPoolsController : ControllerBase
{
    private readonly ILiquidityPoolService _poolService;

    public LiquidityPoolsController(ILiquidityPoolService poolService)
    {
        _poolService = poolService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _poolService.GetAllAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActivePools(CancellationToken ct)
    {
        var result = await _poolService.GetActivePoolsAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _poolService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("pair/{token1Id}/{token2Id}")]
    public async Task<IActionResult> GetByTokenPair(string token1Id, string token2Id, CancellationToken ct)
    {
        var result = await _poolService.GetByTokenPairAsync(token1Id, token2Id, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLiquidityPoolDto dto, CancellationToken ct)
    {
        var result = await _poolService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPost("{id}/add-liquidity")]
    public async Task<IActionResult> AddLiquidity(string id, [FromBody] AddLiquidityDto dto, CancellationToken ct)
    {
        var result = await _poolService.AddLiquidityAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/remove-liquidity")]
    public async Task<IActionResult> RemoveLiquidity(string id, [FromBody] RemoveLiquidityDto dto, CancellationToken ct)
    {
        var result = await _poolService.RemoveLiquidityAsync(id, dto, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(string id, CancellationToken ct)
    {
        var result = await _poolService.ActivateAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        var result = await _poolService.DeactivateAsync(id, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}

[ApiController]
[Route("api/liquidity-providers")]
public class LiquidityProvidersController : ControllerBase
{
    private readonly ILiquidityProviderService _providerService;

    public LiquidityProvidersController(ILiquidityProviderService providerService)
    {
        _providerService = providerService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _providerService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("pool/{poolId}")]
    public async Task<IActionResult> GetByPoolId(string poolId, CancellationToken ct)
    {
        var result = await _providerService.GetByPoolIdAsync(poolId, ct);
        return ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _providerService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpGet("pool/{poolId}/user/{userId}")]
    public async Task<IActionResult> GetByPoolAndUser(string poolId, string userId, CancellationToken ct)
    {
        var result = await _providerService.GetByPoolAndUserAsync(poolId, userId, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLiquidityProviderDto dto, CancellationToken ct)
    {
        var result = await _providerService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _providerService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
