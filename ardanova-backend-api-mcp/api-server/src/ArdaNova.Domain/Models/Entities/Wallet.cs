using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Wallet")]
public class Wallet
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string address { get; set; }

    [Required]
    public WalletProvider provider { get; set; }

    public string? label { get; set; }

    [Required]
    public bool isVerified { get; set; }

    [Required]
    public bool isPrimary { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
