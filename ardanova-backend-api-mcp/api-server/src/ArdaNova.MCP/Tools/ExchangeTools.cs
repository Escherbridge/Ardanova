namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class TokenSwapTools
{
    private readonly ITokenSwapService _swapService;

    public TokenSwapTools(ITokenSwapService swapService)
    {
        _swapService = swapService;
    }

    [McpServerTool(Name = "swap_get_by_id")]
    [Description("Retrieves a token swap by its unique identifier")]
    public async Task<TokenSwapDto?> GetSwapById(
        [Description("The swap ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _swapService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "swap_get_by_user_id")]
    [Description("Retrieves all swaps for a user")]
    public async Task<IReadOnlyList<TokenSwapDto>?> GetSwapsByUserId(
        [Description("The user ID")] string userId,
        CancellationToken ct = default)
    {
        var result = await _swapService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "swap_create")]
    [Description("Creates a new token swap request")]
    public async Task<TokenSwapDto?> CreateSwap(
        [Description("The user ID")] string userId,
        [Description("The source share ID")] string fromShareId,
        [Description("The destination share ID")] string toShareId,
        [Description("The source token ID")] string fromTokenId,
        [Description("The destination token ID")] string toTokenId,
        [Description("The amount to swap from")] decimal fromAmount,
        [Description("The amount to receive")] decimal toAmount,
        [Description("The exchange rate")] decimal exchangeRate,
        [Description("The swap fee")] decimal fee = 0,
        CancellationToken ct = default)
    {
        var dto = new CreateTokenSwapDto
        {
            UserId = userId,
            FromShareId = fromShareId,
            ToShareId = toShareId,
            FromTokenId = fromTokenId,
            ToTokenId = toTokenId,
            FromAmount = fromAmount,
            ToAmount = toAmount,
            ExchangeRate = exchangeRate,
            Fee = fee
        };
        var result = await _swapService.CreateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "swap_complete")]
    [Description("Completes a token swap")]
    public async Task<TokenSwapDto?> CompleteSwap(
        [Description("The swap ID")] string id,
        [Description("Optional transaction hash")] string? txHash = null,
        CancellationToken ct = default)
    {
        var result = await _swapService.CompleteAsync(id, new CompleteSwapDto { TxHash = txHash }, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "swap_cancel")]
    [Description("Cancels a pending token swap")]
    public async Task<TokenSwapDto?> CancelSwap(
        [Description("The swap ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _swapService.CancelAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }
}

[McpServerToolType]
public class LiquidityPoolTools
{
    private readonly ILiquidityPoolService _poolService;

    public LiquidityPoolTools(ILiquidityPoolService poolService)
    {
        _poolService = poolService;
    }

    [McpServerTool(Name = "pool_get_all")]
    [Description("Retrieves all liquidity pools")]
    public async Task<IReadOnlyList<LiquidityPoolDto>?> GetAllPools(
        CancellationToken ct = default)
    {
        var result = await _poolService.GetAllAsync(ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "pool_get_active")]
    [Description("Retrieves all active liquidity pools")]
    public async Task<IReadOnlyList<LiquidityPoolDto>?> GetActivePools(
        CancellationToken ct = default)
    {
        var result = await _poolService.GetActivePoolsAsync(ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "pool_get_by_id")]
    [Description("Retrieves a liquidity pool by its unique identifier")]
    public async Task<LiquidityPoolDto?> GetPoolById(
        [Description("The pool ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _poolService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "pool_get_by_token_pair")]
    [Description("Retrieves a liquidity pool by token pair")]
    public async Task<LiquidityPoolDto?> GetPoolByTokenPair(
        [Description("The first share ID")] string share1Id,
        [Description("The second share ID")] string share2Id,
        CancellationToken ct = default)
    {
        var result = await _poolService.GetByTokenPairAsync(share1Id, share2Id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "pool_create")]
    [Description("Creates a new liquidity pool")]
    public async Task<LiquidityPoolDto?> CreatePool(
        [Description("The first share ID")] string share1Id,
        [Description("The second share ID")] string share2Id,
        [Description("The first token ID")] string token1Id,
        [Description("The second token ID")] string token2Id,
        [Description("The fee percentage (default 0.3%)")] decimal feePercent = 0.003m,
        CancellationToken ct = default)
    {
        var dto = new CreateLiquidityPoolDto
        {
            Share1Id = share1Id,
            Share2Id = share2Id,
            Token1Id = token1Id,
            Token2Id = token2Id,
            FeePercent = feePercent
        };
        var result = await _poolService.CreateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
