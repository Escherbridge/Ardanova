using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectEquity")]
public class ProjectEquity
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal sharePercent { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal investmentAmount { get; set; }

    [Required]
    public DateTime grantedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
