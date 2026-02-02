using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(shareId), IsUnique = true)]
[Table("Fundraising")]
public class Fundraising
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string shareId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal fundingGoal { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal currentFunding { get; set; }

    [Precision(18, 8)]
    public decimal? minContribution { get; set; }

    [Precision(18, 8)]
    public decimal? maxContribution { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal sharePrice { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime endDate { get; set; }

    [Required]
    public FundraisingStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("shareId")]
    public virtual ProjectShare? Share { get; set; }

    public virtual ICollection<FundraisingContribution> FundraisingContributions { get; set; } = new List<FundraisingContribution>();

}
