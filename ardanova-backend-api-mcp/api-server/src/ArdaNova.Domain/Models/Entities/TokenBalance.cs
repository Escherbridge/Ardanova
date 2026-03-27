using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TokenBalance")]
public class TokenBalance
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? projectTokenConfigId { get; set; }

    [Required]
    public bool isPlatformToken { get; set; }

    public TokenHolderClass? holderClass { get; set; }

    [Required]
    public bool isLiquid { get; set; }

    [Required]
    public int balance { get; set; }

    [Required]
    public int lockedBalance { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

}
