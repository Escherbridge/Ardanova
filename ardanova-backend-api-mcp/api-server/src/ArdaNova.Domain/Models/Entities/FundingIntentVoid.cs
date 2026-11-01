using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>A sponsorship request key voided before anything was admitted under it: the tombstone a late create is refused by.</summary>
/// <remarks>See <c>src/ArdaNova.Application/Services/Implementations/AGENTS.md</c> section Sponsorship void.</remarks>
[Table("FundingIntentVoid")]
public class FundingIntentVoid
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string funderUserId { get; set; } = string.Empty;

    [Required]
    public string idempotencyKey { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("funderUserId")]
    public virtual User? FunderUser { get; set; }

}
