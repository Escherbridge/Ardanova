using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ICOContribution")]
public class ICOContribution
{
    [Key]
    public string id { get; set; }

    [Required]
    public string icoId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public decimal amount { get; set; }

    [Required]
    public decimal tokenAmount { get; set; }

    [Required]
    public string paymentAsset { get; set; }

    public string? txHash { get; set; }

    [Required]
    public ContributionStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("icoId")]
    public virtual ICO Ico { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
