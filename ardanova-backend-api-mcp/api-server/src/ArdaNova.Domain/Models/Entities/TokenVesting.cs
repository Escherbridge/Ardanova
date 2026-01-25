using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TokenVesting")]
public class TokenVesting
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string holderId { get; set; } = string.Empty;

    [Required]
    public decimal totalAmount { get; set; }

    [Required]
    public decimal releasedAmount { get; set; }

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime cliffEnd { get; set; }

    [Required]
    public DateTime vestingEnd { get; set; }

    [Required]
    public VestingFrequency releaseFrequency { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("holderId")]
    public virtual TokenHolder? Holder { get; set; }

}
