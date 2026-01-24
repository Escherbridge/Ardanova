using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("GuildMember")]
public class GuildMember
{
    [Key]
    public string id { get; set; }

    [Required]
    public string guildId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string role { get; set; }

    [Required]
    public DateTime joinedAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild Guild { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
