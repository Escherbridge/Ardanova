using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ICO")]
public class ICO
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string tokenId { get; set; } = string.Empty;

    [Required]
    public decimal fundingGoal { get; set; }

    [Required]
    public decimal currentFunding { get; set; }

    public decimal? minContribution { get; set; }

    public decimal? maxContribution { get; set; }

    [Required]
    public decimal tokenPrice { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime endDate { get; set; }

    [Required]
    public ICOStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("tokenId")]
    public virtual ProjectToken? Token { get; set; }

    public virtual ICollection<ICOContribution> ICOContributions { get; set; } = new List<ICOContribution>();

}
