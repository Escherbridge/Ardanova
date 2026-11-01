using System.ComponentModel.DataAnnotations.Schema;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>
/// One "sell my units" request, and the read-model of the node exchange that settled it.
/// </summary>
/// <remarks>A receipt, never the balance of record; see <c>Models/Entities/AGENTS.md</c> section payout request.</remarks>
public partial class PayoutRequest
{
    /// <summary>Prefix of the exchange idempotency key every unit sale is submitted under.</summary>
    /// <remarks>
    /// The key is DERIVED from <see cref="id"/> rather than stored, so the request that submits a
    /// sale and the reconciliation that chases it can never address two different node
    /// operations. See <see cref="ExchangeIdempotencyKey"/>.
    /// </remarks>
    public const string ExchangeIdempotencyKeyPrefix = "ardanova:unit-sale:";

    /// <summary>The node's receipt reference for this sale, under its honest name.</summary>
    [NotMapped]
    public string? SellReceiptReference
    {
        get => conversionTxHash;
        set => conversionTxHash = value;
    }

    /// <summary>The node's exchange operation id for this sale, under its honest name.</summary>
    [NotMapped]
    public string? SellOperationId
    {
        get => payoutTxHash;
        set => payoutTxHash = value;
    }

    /// <summary>The one key this sale is submitted and reconciled under, derived from <see cref="id"/>.</summary>
    /// <remarks>
    /// Derived rather than stored on purpose: a stored key can be missing on a row whose
    /// submission already happened, and a reconciliation that cannot name the operation it is
    /// chasing has to guess. A GUID row id makes the derived key globally unique on its own.
    /// </remarks>
    [NotMapped]
    public string ExchangeIdempotencyKey => ExchangeIdempotencyKeyPrefix + id;

    /// <summary>True only once the node has returned a receipt for this sale.</summary>
    [NotMapped]
    public bool IsNodeSettled => !string.IsNullOrWhiteSpace(conversionTxHash);
}
