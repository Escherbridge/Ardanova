using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("MarketingCampaign")]
public class MarketingCampaign
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string shopId { get; set; } = string.Empty;

    [Required]
    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    [Required]
    public string platform { get; set; } = string.Empty;

    [Required]
    public string content { get; set; } = string.Empty;

    public string? mediaUrls { get; set; }

    public DateTime? scheduledAt { get; set; }

    [Required]
    public CampaignStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string userId { get; set; } = string.Empty;

    [ForeignKey("shopId")]
    public virtual Shop? Shop { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
