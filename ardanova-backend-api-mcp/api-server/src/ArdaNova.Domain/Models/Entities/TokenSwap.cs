using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TokenSwap")]
public class TokenSwap
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string fromTokenId { get; set; } = string.Empty;

    [Required]
    public string toTokenId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal fromAmount { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal toAmount { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal exchangeRate { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal fee { get; set; }

    public string? txHash { get; set; }

    [Required]
    public SwapStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? completedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("fromTokenId")]
    [InverseProperty("TokenSwapsAsFromToken")]
    public virtual ProjectToken? FromToken { get; set; }

    [ForeignKey("toTokenId")]
    [InverseProperty("TokenSwapsAsToToken")]
    public virtual ProjectToken? ToToken { get; set; }

}
