using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("GuildReview")]
public class GuildReview
{
    [Key]
    public string id { get; set; }

    [Required]
    public string guildId { get; set; }

    public string? projectId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public int rating { get; set; }

    public string? comment { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild Guild { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
