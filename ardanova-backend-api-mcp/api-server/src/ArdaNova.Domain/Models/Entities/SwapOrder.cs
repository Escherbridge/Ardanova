using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(quoteId), IsUnique = true)]
[Index(nameof(economicSettlementId), IsUnique = true)]
[Table("SwapOrder")]
public class SwapOrder
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string quoteId { get; set; } = string.Empty;

    [Required]
    public string actorUserId { get; set; } = string.Empty;

    [Required]
    public SwapOrderStatus status { get; set; }

    public string? economicSettlementId { get; set; }

    public DateTime? acceptedAt { get; set; }

    public DateTime? confirmedAt { get; set; }

    public string? failureCode { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("quoteId")]
    public virtual SwapQuote? Quote { get; set; }

    [ForeignKey("actorUserId")]
    public virtual User? ActorUser { get; set; }

    [ForeignKey("economicSettlementId")]
    public virtual EconomicSettlement? EconomicSettlement { get; set; }

}
