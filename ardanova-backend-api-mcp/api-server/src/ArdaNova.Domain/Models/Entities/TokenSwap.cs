namespace ArdaNova.Domain.Models.Entities;

using System.Text.Json.Serialization;
using ArdaNova.Domain.Models.Enums;

public class TokenSwap
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FromTokenId { get; private set; }
    public Guid ToTokenId { get; private set; }
    public decimal FromAmount { get; private set; }
    public decimal ToAmount { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public decimal Fee { get; private set; }
    public string? TxHash { get; private set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SwapStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private TokenSwap() { }

    public static TokenSwap Create(
        Guid userId,
        Guid fromTokenId,
        Guid toTokenId,
        decimal fromAmount,
        decimal toAmount,
        decimal exchangeRate,
        decimal fee = 0)
    {
        return new TokenSwap
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FromTokenId = fromTokenId,
            ToTokenId = toTokenId,
            FromAmount = fromAmount,
            ToAmount = toAmount,
            ExchangeRate = exchangeRate,
            Fee = fee,
            Status = SwapStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void StartProcessing()
    {
        Status = SwapStatus.PROCESSING;
    }

    public void Complete(string? txHash = null)
    {
        Status = SwapStatus.COMPLETED;
        TxHash = txHash;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        Status = SwapStatus.FAILED;
    }

    public void Cancel()
    {
        Status = SwapStatus.CANCELLED;
    }
}
