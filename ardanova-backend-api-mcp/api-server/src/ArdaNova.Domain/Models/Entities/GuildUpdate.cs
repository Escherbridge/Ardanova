using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("GuildUpdate")]
public class GuildUpdate
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    [Required]
    public string createdById { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string content { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? images { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("createdById")]
    public virtual User? CreatedBy { get; set; }

}
