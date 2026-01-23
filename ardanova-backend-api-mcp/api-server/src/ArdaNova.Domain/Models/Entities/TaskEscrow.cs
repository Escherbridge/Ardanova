namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class TaskEscrow
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid FunderId { get; private set; }
    public Guid TokenId { get; private set; }
    public decimal Amount { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EscrowStatus Status { get; private set; }

    public string? TxHashFund { get; private set; }
    public string? TxHashRelease { get; private set; }
    public string? TxHashRefund { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? FundedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    // Navigation
    public ProjectTask Task { get; private set; } = null!;
    public User Funder { get; private set; } = null!;

    private TaskEscrow() { }

    public static TaskEscrow Create(
        Guid taskId,
        Guid funderId,
        Guid tokenId,
        decimal amount)
    {
        return new TaskEscrow
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            FunderId = funderId,
            TokenId = tokenId,
            Amount = amount,
            Status = EscrowStatus.FUNDED,
            CreatedAt = DateTime.UtcNow,
            FundedAt = DateTime.UtcNow
        };
    }

    public void SetFundTxHash(string txHash)
    {
        TxHashFund = txHash;
    }

    public void Release(string? txHash = null)
    {
        Status = EscrowStatus.RELEASED;
        TxHashRelease = txHash;
        ReleasedAt = DateTime.UtcNow;
    }

    public void Dispute()
    {
        Status = EscrowStatus.DISPUTED;
    }

    public void Refund(string? txHash = null)
    {
        Status = EscrowStatus.REFUNDED;
        TxHashRefund = txHash;
        RefundedAt = DateTime.UtcNow;
    }
}
