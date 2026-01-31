namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class TaskEscrowTools
{
    private readonly ITaskEscrowService _escrowService;

    public TaskEscrowTools(ITaskEscrowService escrowService)
    {
        _escrowService = escrowService;
    }

    [McpServerTool(Name = "escrow_get_by_id")]
    [Description("Retrieves an escrow by its unique identifier")]
    public async Task<TaskEscrowDto?> GetEscrowById(
        [Description("The escrow ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _escrowService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "escrow_get_by_task_id")]
    [Description("Retrieves the escrow for a specific task")]
    public async Task<TaskEscrowDto?> GetEscrowByTaskId(
        [Description("The task ID")] string taskId,
        CancellationToken ct = default)
    {
        var result = await _escrowService.GetByTaskIdAsync(taskId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "escrow_get_by_funder")]
    [Description("Retrieves all escrows funded by a user")]
    public async Task<IReadOnlyList<TaskEscrowDto>?> GetEscrowsByFunder(
        [Description("The funder user ID")] string funderId,
        CancellationToken ct = default)
    {
        var result = await _escrowService.GetByFunderIdAsync(funderId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "escrow_create")]
    [Description("Creates a new escrow for a task")]
    public async Task<TaskEscrowDto?> CreateEscrow(
        [Description("The task ID")] string taskId,
        [Description("The funder user ID")] string funderId,
        [Description("The share ID for payment")] string shareId,
        [Description("The token ID for payment")] string tokenId,
        [Description("The amount to escrow")] decimal amount,
        [Description("Optional funding transaction hash")] string? txHash = null,
        CancellationToken ct = default)
    {
        var dto = new CreateTaskEscrowDto
        {
            TaskId = taskId,
            FunderId = funderId,
            ShareId = shareId,
            TokenId = tokenId,
            Amount = amount,
            TxHashFund = txHash
        };
        var result = await _escrowService.CreateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "escrow_release")]
    [Description("Releases the escrowed funds to the task assignee")]
    public async Task<TaskEscrowDto?> ReleaseEscrow(
        [Description("The escrow ID")] string id,
        [Description("Optional release transaction hash")] string? txHash = null,
        CancellationToken ct = default)
    {
        var result = await _escrowService.ReleaseAsync(id, new ReleaseEscrowDto { TxHash = txHash }, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "escrow_dispute")]
    [Description("Marks an escrow as disputed")]
    public async Task<TaskEscrowDto?> DisputeEscrow(
        [Description("The escrow ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _escrowService.DisputeAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "escrow_refund")]
    [Description("Refunds the escrowed funds to the funder")]
    public async Task<TaskEscrowDto?> RefundEscrow(
        [Description("The escrow ID")] string id,
        [Description("Optional refund transaction hash")] string? txHash = null,
        CancellationToken ct = default)
    {
        var result = await _escrowService.RefundAsync(id, new RefundEscrowDto { TxHash = txHash }, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
