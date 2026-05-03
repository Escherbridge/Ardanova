using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(guildId), IsUnique = true)]
[Index(nameof(address), IsUnique = true)]
[Table("GuildWallet")]
public class GuildWallet
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    public string? address { get; set; }

    [Required]
    public WalletProvider provider { get; set; }

    public string? label { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal balance { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal reservedBalance { get; set; }

    [Required]
    public bool isVerified { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

}
