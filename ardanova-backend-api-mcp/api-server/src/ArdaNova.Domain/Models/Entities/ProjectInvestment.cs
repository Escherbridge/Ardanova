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

    public string? stripePaymentIntentId { get; set; }

    [Required]
    public DateTime investedAt { get; set; }

    [Required]
    public bool protectionEligible { get; set; }

    [Required]
    public bool protectionPaidOut { get; set; }

    public double? protectionAmount { get; set; }

    public DateTime? protectionPaidAt { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
