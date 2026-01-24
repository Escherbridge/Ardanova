using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("LiquidityProvider")]
public class LiquidityProvider
{
    [Key]
    public string id { get; set; }

    [Required]
    public string poolId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public decimal shares { get; set; }

    [Required]
    public decimal token1In { get; set; }

    [Required]
    public decimal token2In { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("poolId")]
    public virtual LiquidityPool Pool { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
