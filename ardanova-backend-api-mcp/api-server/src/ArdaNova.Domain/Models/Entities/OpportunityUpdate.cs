using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("OpportunityUpdate")]
public class OpportunityUpdate
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string opportunityId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string content { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? images { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("opportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
