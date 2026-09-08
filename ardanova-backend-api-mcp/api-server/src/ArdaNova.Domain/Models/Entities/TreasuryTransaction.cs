using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>
/// RETIRED. A per-project transaction ledger that was never written and is never read.
/// </summary>
/// <remarks>
/// <para>
/// <b>Replaced, not revived.</b> A project's money view is DERIVED — see
/// <c>ProjectLedgerService</c> — from the rows that already record each event: the sponsorship
/// (<see cref="FundingIntent"/>), the award (<see cref="TokenAllocation"/>), the buy-back
/// (<see cref="PayoutRequest"/>) and the treasury receipt
/// (<see cref="PlatformTreasuryTransaction"/>). Writing those events a second time into this
/// table would create a ledger that can disagree with all four, and a per-project
/// <see cref="Treasury.balance"/> it accumulated into would be exactly the
/// database-number-as-money the platform rejected: the balance of record is the project's
/// custodial treasury account on the node.
/// </para>
/// <para>
/// <b>The retirement is a control, not a comment.</b> The constructor below is not public, so no
/// application code can create one of these rows; a future writer gets a compile error here
/// instead of quietly starting a second ledger. The columns are kept unchanged only so the
/// stored schema does not move — dropping the table is a migration, not an entity edit, and it
/// belongs to whichever item next opens <c>ArdaNovaDbContext</c>.
/// </para>
/// </remarks>
[Table("TreasuryTransaction")]
public class TreasuryTransaction
{
    /// <summary>Materialisation only. Not public: nothing may write a row to a retired ledger.</summary>
    protected TreasuryTransaction()
    {
    }

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string treasuryId { get; set; } = string.Empty;

    [Required]
    public TransactionType type { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal amount { get; set; }

    public string? description { get; set; }

    public string? txHash { get; set; }

    public string? proposalId { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("treasuryId")]
    public virtual Treasury? Treasury { get; set; }

}
