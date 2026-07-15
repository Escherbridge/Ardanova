using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("WalletVerificationChallenge")]
public class WalletVerificationChallenge
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string walletId { get; set; } = string.Empty;

    [Required]
    public string address { get; set; } = string.Empty;

    [Required]
    public string chain { get; set; } = string.Empty;

    [Required]
    public string network { get; set; } = string.Empty;

    [Required]
    public string nonceHash { get; set; } = string.Empty;

    [Required]
    public DateTime issuedAt { get; set; }

    [Required]
    public DateTime expiresAt { get; set; }

    public DateTime? consumedAt { get; set; }

    public bool? proofVerified { get; set; }

    public string? signatureHash { get; set; }

    public string? failureCode { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("walletId")]
    public virtual Wallet? Wallet { get; set; }

}
