using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(address), IsUnique = true)]
[Table("Wallet")]
public class Wallet
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string address { get; set; } = string.Empty;

    [Required]
    public WalletProvider provider { get; set; }

    public string? label { get; set; }

    [Required]
    public bool isVerified { get; set; }

    public DateTime? verifiedAt { get; set; }

    public string? verificationChain { get; set; }

    public string? verificationNetwork { get; set; }

    public string? verificationChallengeId { get; set; }

    [Required]
    public bool isPrimary { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    public virtual ICollection<WalletVerificationChallenge> WalletVerificationChallenges { get; set; } = new List<WalletVerificationChallenge>();

}
