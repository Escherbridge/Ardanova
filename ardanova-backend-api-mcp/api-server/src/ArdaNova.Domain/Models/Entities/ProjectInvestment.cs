using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectInvestment")]
public class ProjectInvestment
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectTokenConfigId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public double usdAmount { get; set; }

    [Required]
    public int tokenAmount { get; set; }

    [Required]
    public DateTime investedAt { get; set; }

    [Required]
    public bool protectionEligible { get; set; }

    [Required]
    public bool protectionPaidOut { get; set; }

    public double? protectionAmount { get; set; }

    public DateTime? protectionPaidAt { get; set; }

    /// <summary>True only when the backer opted in ("show my name to other backers") to let
    /// project members see their display name against this pledge. Defaults to false, so a
    /// pledge stays anonymous unless the backer said otherwise.</summary>
    [Required]
    public bool showBackerName { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
