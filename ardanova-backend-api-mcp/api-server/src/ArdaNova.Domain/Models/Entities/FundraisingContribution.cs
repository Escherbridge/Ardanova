using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("FundraisingContribution")]
public class FundraisingContribution
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string fundraisingId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal amount { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal shareAmount { get; set; }

    [Required]
    public string paymentAsset { get; set; } = string.Empty;

    public string? txHash { get; set; }

    [Required]
    public ContributionStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("fundraisingId")]
    public virtual Fundraising? Fundraising { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
