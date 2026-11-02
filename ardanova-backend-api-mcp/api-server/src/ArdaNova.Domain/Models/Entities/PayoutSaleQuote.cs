// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in PayoutSaleQuote.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PayoutSaleQuote")]
public partial class PayoutSaleQuote
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string currencyCode { get; set; } = PayoutSaleDenomination.Code;

    [Required]
    public int currencyDecimals { get; set; } = PayoutSaleDenomination.Decimals;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string projectTokenConfigId { get; set; } = string.Empty;

    [Required]
    public TokenHolderClass holderClass { get; set; }

    [Required]
    public int sourceUnitAmount { get; set; }

    [Required]
    public long platformMinorUnits { get; set; }

    [Required]
    public long usdReferenceMinorUnits { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string commandJson { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime expiresAt { get; set; }

}
