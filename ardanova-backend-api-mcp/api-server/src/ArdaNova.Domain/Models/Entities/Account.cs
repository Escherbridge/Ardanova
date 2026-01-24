using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Account")]
public class Account
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string type { get; set; }

    [Required]
    public string provider { get; set; }

    [Required]
    public string providerAccountId { get; set; }

    public string? refresh_token { get; set; }

    public string? access_token { get; set; }

    public int? expires_at { get; set; }

    public string? token_type { get; set; }

    public string? scope { get; set; }

    public string? id_token { get; set; }

    public string? session_state { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
