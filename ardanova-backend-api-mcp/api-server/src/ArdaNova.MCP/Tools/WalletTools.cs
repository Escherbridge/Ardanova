namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class WalletTools
{
    private readonly IWalletService _walletService;

    public WalletTools(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [McpServerTool(Name = "wallet_get_by_id")]
    [Description("Retrieves a wallet by its unique identifier")]
    public async Task<WalletDto?> GetWalletById(
        [Description("The unique identifier of the wallet")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _walletService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "wallet_get_by_user_id")]
    [Description("Retrieves all wallets for a user")]
    public async Task<IReadOnlyList<WalletDto>?> GetWalletsByUserId(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _walletService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "wallet_get_by_address")]
    [Description("Retrieves a wallet by its blockchain address")]
    public async Task<WalletDto?> GetWalletByAddress(
        [Description("The blockchain wallet address")] string address,
        CancellationToken ct = default)
    {
        var result = await _walletService.GetByAddressAsync(address, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "wallet_get_primary")]
    [Description("Retrieves the primary wallet for a user")]
    public async Task<WalletDto?> GetPrimaryWallet(
        [Description("The user ID")] Guid userId,
        CancellationToken ct = default)
    {
        var result = await _walletService.GetPrimaryWalletAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "wallet_verify")]
    [Description("Marks a wallet as verified")]
    public async Task<WalletDto?> VerifyWallet(
        [Description("The wallet ID to verify")] Guid id,
        CancellationToken ct = default)
    {
        var result = await _walletService.VerifyAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
