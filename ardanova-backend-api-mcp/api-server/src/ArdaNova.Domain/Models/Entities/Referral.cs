using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(referredId), IsUnique = true)]
[Table("Referral")]
public class Referral
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string referrerId { get; set; } = string.Empty;

    [Required]
    public string referredId { get; set; } = string.Empty;

    public string? referralCode { get; set; }

    [Required]
    public ReferralStatus status { get; set; }

    [Required]
    public bool rewardClaimed { get; set; }

    public int? xpRewarded { get; set; }

    [Precision(18, 8)]
    public decimal? tokenRewarded { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? completedAt { get; set; }

    [ForeignKey("referrerId")]
    [InverseProperty("ReferralsAsReferrer")]
    public virtual User? Referrer { get; set; }

    [ForeignKey("referredId")]
    [InverseProperty("ReferralsAsReferred")]
    public virtual User? Referred { get; set; }

}
