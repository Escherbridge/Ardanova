using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("EconomicSettlementLeg")]
public class EconomicSettlementLeg
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string economicSettlementId { get; set; } = string.Empty;

    [Required]
    public int position { get; set; }

    [Required]
    public EconomicSettlementLegKind kind { get; set; }

    [Required]
    public string assetDefinitionId { get; set; } = string.Empty;

    [Required]
    public string amountAtoms { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("economicSettlementId")]
    public virtual EconomicSettlement? EconomicSettlement { get; set; }

    [ForeignKey("assetDefinitionId")]
    public virtual AssetDefinition? AssetDefinition { get; set; }

}
