using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("GuildMember")]
public class GuildMember
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public GuildMemberRole role { get; set; }

    [Required]
    public DateTime joinedAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
