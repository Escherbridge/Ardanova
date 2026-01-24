using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Referral")]
public class Referral
{
    [Key]
    public string id { get; set; }

    [Required]
    public string referrerId { get; set; }

    [Required]
    public string referredId { get; set; }

    public string? referralCode { get; set; }

    [Required]
    public ReferralStatus status { get; set; }

    [Required]
    public bool rewardClaimed { get; set; }

    public int? xpRewarded { get; set; }

    public decimal? tokenRewarded { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? completedAt { get; set; }

    [ForeignKey("referrerId")]
    public virtual User Referrer { get; set; }

    [ForeignKey("referredId")]
    public virtual User Referred { get; set; }

}
