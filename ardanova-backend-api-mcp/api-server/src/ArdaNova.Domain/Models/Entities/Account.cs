using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Account")]
public class Account
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string type { get; set; } = string.Empty;

    [Required]
    public string provider { get; set; } = string.Empty;

    [Required]
    public string providerAccountId { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? refresh_token { get; set; }

    [Column(TypeName = "text")]
    public string? access_token { get; set; }

    public int? expires_at { get; set; }

    public string? token_type { get; set; }

    public string? scope { get; set; }

    [Column(TypeName = "text")]
    public string? id_token { get; set; }

    public string? session_state { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
